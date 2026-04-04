# =============================================================================
# cv_parser.py — SmartHire AI | Candidate CV Scoring Pipeline
#
# ┌─────────────────────────────────────────────────────────────────────────┐
# │  PIPELINE OVERVIEW (execution order)                                     │
# │                                                                          │
# │  [1] Textract      Extract raw text from CV (PDF/DOCX on S3)            │
# │  [2] Blind Screen  spaCy NER masks PII before any text touches AI       │
# │  [3] Bi-Encoder    Cohere embeds CV (+ cached JD vector from DynamoDB)  │
# │  [4] Cross-Encoder ms-marco-MiniLM re-ranks with full attention         │
# │  [5] Hybrid Score  0.35 × bi + 0.65 × cross → final_score              │
# │  [6] Claude Agent  Extracts skills, strengths, gaps (structured JSON)   │
# │  [7] Interview AI  Second Claude call → 3 probing interview questions   │
# │  [8] DynamoDB      Persists full result + scoring_details               │
# │  [9] OpenSearch    Indexes CV vector for talent pool search             │
# └─────────────────────────────────────────────────────────────────────────┘
#
# ─── UPGRADE 1: Ethical AI — Blind Screening (spaCy NER) ──────────────────
#
#   Problem: LLMs can unconsciously bias match scores if they see candidate
#   names, nationalities, or university affiliations. A model may pattern-
#   match "Harvard" as a prestige signal or adjust tone based on a name.
#
#   Solution: Before the raw CV text touches Cohere or Claude, run it through
#   spaCy's named entity recognizer. Detected entities are replaced with
#   neutral tokens: "John Smith" → [CANDIDATE_NAME], "Vietnam" → [LOCATION].
#   The AI evaluates only technical merit from that point forward.
#
#   Entity labels masked:
#     PERSON → [CANDIDATE_NAME]
#     ORG    → [ORGANIZATION]   (companies and universities)
#     GPE    → [LOCATION]       (countries, cities, states)
#     LOC    → [LOCATION]       (non-GPE locations)
#     NORP   → [DEMOGRAPHIC]    (nationalities, ethnic/religious groups)
#
#   Entities deliberately NOT masked: DATE, CARDINAL, SKILL, PRODUCT.
#   Years of experience ("5 years"), product names ("React", "AWS Lambda"),
#   and version numbers are legitimate technical signal, not PII.
#
#   Deployment: include `en_core_web_sm` in the container image:
#     RUN pip install spacy && python -m spacy download en_core_web_sm
#   Falls back silently to unmasked text if spaCy is unavailable.
#
# ─── UPGRADE 2: Cross-Encoder Reranking ───────────────────────────────────
#
#   Problem: Bi-encoders embed documents independently. "Architected highly
#   available clusters" and "requires AWS EC2 experience" land in different
#   vector regions despite being semantically equivalent in a hiring context.
#
#   Solution: cross-encoder/ms-marco-MiniLM-L-6-v2 receives (JD_query,
#   CV_chunk) as a single input. The transformer's attention heads compare
#   tokens across both documents simultaneously, bridging the vocabulary gap.
#
#   Chunking strategy: CV is split into overlapping 800-char windows with
#   150-char overlap. Top-3 chunk scores are weighted-averaged (0.5/0.3/0.2)
#   to reward candidates with multiple relevant sections, not just one keyword
#   paragraph. Final scores: sigmoid(logit) → [0,1] probability space.
#
#   Deployment: Lambda Container Image with:
#     RUN pip install sentence-transformers torch --index-url \
#             https://download.pytorch.org/whl/cpu
#     # Optional — bake model at build time to avoid cold-start download:
#     RUN python -c "from sentence_transformers import CrossEncoder; \
#         CrossEncoder('cross-encoder/ms-marco-MiniLM-L-6-v2')"
#
# ─── UPGRADE 3: Multi-Agent Interview Guide ───────────────────────────────
#
#   After Claude's skill extraction call produces the structured gaps JSON,
#   a second Claude call receives those gaps and generates 3 highly specific,
#   challenging technical questions calibrated to probe exactly what the
#   candidate claimed but didn't demonstrate.
#
#   The prompts are role-aware: a gap in "AWS Lambda" for a Backend role
#   gets a different question depth than the same gap for a DevOps role.
#
#   Output stored in DynamoDB as `interviewGuide: [{question, rationale}]`.
#   Recruiters see exactly WHY each question was generated, building trust
#   in the AI recommendation.
#
# ─── UPGRADE 4: Talent Pool RAG — OpenSearch Vector Indexing ──────────────
#
#   After DynamoDB write, the candidate's CV vector + structured profile is
#   indexed into AWS OpenSearch Serverless (kNN index, cosine space).
#
#   This enables natural-language talent pool search via a separate endpoint:
#     Query: "mid-level frontend dev experienced in migrating legacy to React"
#     → Cohere embeds the query → kNN search → returns top-5 candidates
#
#   The search endpoint itself lives in a separate Lambda (search_handler.py).
#   A utility function `search_talent_pool()` is included in this file for
#   reference and testing, but is not called during normal CV processing.
#
#   Index structure: candidateId, jobId, cv_vector (1024-dim), skills arrays,
#   seniority, matching_score, updatedAt.
#
#   Deployment requirements:
#     pip install opensearch-py requests requests-aws4auth
#     Env var: OPENSEARCH_ENDPOINT=<your-aoss-endpoint>
#     IAM policy: aoss:APIAccessAll on the collection ARN
#
# =============================================================================

import json
import math
import os
import time
import urllib.parse
from datetime import datetime, timedelta, timezone
from decimal import Decimal

import boto3
from botocore.config import Config


# ---------------------------------------------------------------------------
# AWS Clients
# ---------------------------------------------------------------------------

retry_config = Config(
    region_name="ap-southeast-1",
    retries={"max_attempts": 10, "mode": "adaptive"},
)

bedrock = boto3.client("bedrock-runtime", config=retry_config)
textract = boto3.client("textract", region_name="ap-southeast-1")
dynamodb = boto3.resource("dynamodb", region_name="ap-southeast-1")


# ---------------------------------------------------------------------------
# Configuration — Environment Variables
# ---------------------------------------------------------------------------

DYNAMODB_JOBS_TABLE = os.environ.get("DYNAMODB_JOBS_TABLE", "SmartHire_Jobs")
CLAUDE_MODEL_ID = os.environ.get("CLAUDE_MODEL_ID") or os.environ.get(
    "BEDROCK_MODEL_ID", "apac.anthropic.claude-3-5-sonnet-20241022-v2:0"
)
COHERE_MODEL_ID = os.environ.get("COHERE_MODEL_ID", "cohere.embed-english-v3")
CV_BUCKET_NAME = os.environ.get("CV_BUCKET_NAME", "")
DYNAMODB_TABLE = (
    os.environ.get("DYNAMODB_TABLE_NAME")
    or os.environ.get("DYNAMODB_TABLE")
    or "SmartHire_Profiles"
)
PARSE_STATUS_TTL_DAYS = int(os.environ.get("PARSE_STATUS_TTL_DAYS", "30"))

# Cross-Encoder
CROSS_ENCODER_MODEL_ID = os.environ.get(
    "CROSS_ENCODER_MODEL_ID", "cross-encoder/ms-marco-MiniLM-L-6-v2"
)
CROSS_ENCODER_BAKED_PATH = os.environ.get(
    "CROSS_ENCODER_MODEL_PATH", "/opt/ml/cross_encoder"
)
CROSS_ENCODER_CHUNK_CHARS = int(os.environ.get("CE_CHUNK_CHARS", "800"))
CROSS_ENCODER_OVERLAP_CHARS = int(os.environ.get("CE_OVERLAP_CHARS", "150"))
CROSS_ENCODER_MAX_CHUNKS = int(os.environ.get("CE_MAX_CHUNKS", "12"))
CROSS_ENCODER_JD_QUERY_CHARS = int(os.environ.get("CE_JD_QUERY_CHARS", "1500"))
CROSS_ENCODER_TOP_K = int(os.environ.get("CE_TOP_K", "3"))

# Hybrid blending weights (must sum to 1.0)
HYBRID_WEIGHT_CROSS = float(os.environ.get("HYBRID_WEIGHT_CROSS", "0.65"))
HYBRID_WEIGHT_BI = float(os.environ.get("HYBRID_WEIGHT_BI", "0.35"))

# OpenSearch Talent Pool
# Set to your AWS OpenSearch Serverless collection endpoint (without https://).
# Example: "abc123xyz.ap-southeast-1.aoss.amazonaws.com"
OPENSEARCH_ENDPOINT = os.environ.get("OPENSEARCH_ENDPOINT", "")
OPENSEARCH_INDEX = os.environ.get("OPENSEARCH_INDEX", "smarthire-candidates")
OPENSEARCH_REGION = os.environ.get("OPENSEARCH_REGION", "ap-southeast-1")

# Feature flags — set to "false" to disable individual upgrades without
# code changes, useful for A/B testing or staged rollouts.
ENABLE_BLIND_SCREENING = os.environ.get("ENABLE_BLIND_SCREENING", "true").lower() == "true"
ENABLE_CROSS_ENCODER = os.environ.get("ENABLE_CROSS_ENCODER", "true").lower() == "true"
ENABLE_INTERVIEW_GUIDE = os.environ.get("ENABLE_INTERVIEW_GUIDE", "true").lower() == "true"
ENABLE_OPENSEARCH = os.environ.get("ENABLE_OPENSEARCH", "true").lower() == "true"


# ---------------------------------------------------------------------------
# UPGRADE 1: Blind Screening — spaCy NER PII Masker
# ---------------------------------------------------------------------------

# Module-level spaCy model singleton. Loaded once per container lifetime.
_spacy_nlp = None

# Mapping from spaCy entity label to the replacement token inserted in the text.
# Only labels that carry genuine demographic/identity bias risk are included.
# Technical entities (PRODUCT, DATE, CARDINAL, ORDINAL) are intentionally
# excluded — they carry legitimate signal for skills assessment.
_PII_LABEL_TOKENS = {
    "PERSON": "[CANDIDATE_NAME]",
    "ORG":    "[ORGANIZATION]",
    "GPE":    "[LOCATION]",
    "LOC":    "[LOCATION]",
    "NORP":   "[DEMOGRAPHIC]",
}


def _get_spacy_model():
    """
    Load and cache the spaCy NER model.

    Returns the loaded nlp object, or None if spaCy is not installed.
    Failure is non-fatal — the pipeline falls back to using the original
    CV text and logs a warning so the ops team can fix the deployment.
    """
    global _spacy_nlp

    if _spacy_nlp is not None:
        return _spacy_nlp

    try:
        import spacy  # type: ignore

        # Try to load from the baked model path first (container image deployment).
        # If absent, load from the default spaCy model store (requires the model
        # to have been installed via: python -m spacy download en_core_web_sm).
        baked_model_path = os.environ.get("SPACY_MODEL_PATH", "/opt/ml/spacy_model")
        if os.path.isdir(baked_model_path):
            _spacy_nlp = spacy.load(baked_model_path)
            print(f"INFO: spaCy model loaded from container image: {baked_model_path}")
        else:
            _spacy_nlp = spacy.load("en_core_web_sm")
            print("INFO: spaCy model loaded from package install.")

        return _spacy_nlp

    except ImportError:
        print(
            "WARNING: spaCy is not installed. Blind screening is disabled. "
            "Install spaCy and en_core_web_sm in the container image to enable PII masking."
        )
        return None
    except OSError as exc:
        print(
            f"WARNING: spaCy model could not be loaded: {exc}. "
            f"Blind screening is disabled for this invocation."
        )
        return None


def mask_pii_entities(raw_text: str) -> tuple[str, dict]:
    """
    Replace personally identifiable entities in CV text with neutral tokens.

    Returns:
        masked_text:    CV text with PII replaced. This is what flows into
                        Cohere, the Cross-Encoder, and Claude. The original
                        text is never logged or stored after this function runs.
        masking_report: A summary dict stored in DynamoDB for audit purposes,
                        showing counts per entity type. We do NOT store the
                        actual masked values to avoid re-introducing PII into
                        logs.

    Algorithm:
        spaCy processes the text and returns a list of detected entities with
        their character offsets. We rebuild the text by iterating through
        character positions and substituting entity spans with their tokens.
        Processing is done right-to-left so replacements don't shift offsets
        for subsequent substitutions.
    """
    if not ENABLE_BLIND_SCREENING:
        return raw_text, {"blind_screening": "disabled"}

    nlp = _get_spacy_model()
    if nlp is None:
        return raw_text, {"blind_screening": "unavailable", "note": "spaCy not installed"}

    doc = nlp(raw_text)

    # Collect entities that need masking, in reverse order (last → first)
    # so character offset replacements don't invalidate earlier positions.
    entities_to_mask = [
        ent for ent in doc.ents if ent.label_ in _PII_LABEL_TOKENS
    ]
    entities_to_mask.sort(key=lambda e: e.start_char, reverse=True)

    masked_text = raw_text
    masking_counts: dict[str, int] = {}

    for ent in entities_to_mask:
        label = ent.label_
        token = _PII_LABEL_TOKENS[label]
        masked_text = masked_text[: ent.start_char] + token + masked_text[ent.end_char :]
        masking_counts[label] = masking_counts.get(label, 0) + 1

    total_masked = sum(masking_counts.values())
    masking_report = {
        "blind_screening": "applied",
        "total_entities_masked": total_masked,
        "by_label": masking_counts,
    }
    print(
        f"INFO: Blind screening complete. "
        f"Masked {total_masked} entities: {masking_counts}"
    )
    return masked_text, masking_report


# ---------------------------------------------------------------------------
# UPGRADE 2: Cross-Encoder Reranking
# ---------------------------------------------------------------------------

_cross_encoder_instance = None


def _sigmoid(x: float) -> float:
    """
    Map a raw ms-marco logit to a [0, 1] probability via sigmoid.

    ms-marco outputs unbounded logits, not probabilities. A logit of +5 does
    not mean "50% relevant". Sigmoid normalizes this so cross-encoder scores
    are directly comparable with the [0, 1] cosine similarity from Cohere.
    """
    x = max(-500.0, min(500.0, x))
    return 1.0 / (1.0 + math.exp(-x))


def _get_cross_encoder():
    """
    Load the CrossEncoder model, using a /tmp cache for warm invocations.

    Load priority:
      1. CROSS_ENCODER_BAKED_PATH   — baked into container at build time (best)
      2. /tmp/cross_encoder_model   — downloaded by a previous cold start
      3. HuggingFace Hub download   — first cold start only (~10-15s, one-time)

    Returns None on any failure; callers fall back to bi-encoder score only.
    """
    global _cross_encoder_instance

    if _cross_encoder_instance is not None:
        return _cross_encoder_instance

    if not ENABLE_CROSS_ENCODER:
        return None

    try:
        from sentence_transformers import CrossEncoder  # type: ignore

        tmp_cache_path = "/tmp/cross_encoder_model"

        if os.path.isdir(CROSS_ENCODER_BAKED_PATH):
            load_path = CROSS_ENCODER_BAKED_PATH
            print(f"INFO: Cross-encoder loaded from container image: {load_path}")
        elif os.path.isdir(tmp_cache_path):
            load_path = tmp_cache_path
            print(f"INFO: Cross-encoder loaded from /tmp cache.")
        else:
            print(
                f"INFO: Downloading cross-encoder '{CROSS_ENCODER_MODEL_ID}' "
                f"to /tmp (one-time cold-start cost)."
            )
            os.environ["HF_HOME"] = "/tmp/hf_home"
            temp_model = CrossEncoder(CROSS_ENCODER_MODEL_ID, max_length=512)
            temp_model.save(tmp_cache_path)
            del temp_model
            load_path = tmp_cache_path
            print("INFO: Cross-encoder downloaded and cached.")

        _cross_encoder_instance = CrossEncoder(load_path, max_length=512)
        print("INFO: Cross-encoder ready.")
        return _cross_encoder_instance

    except ImportError:
        print(
            "WARNING: sentence-transformers not installed. "
            "Cross-encoder disabled. Deploy as container image."
        )
        return None
    except Exception as exc:
        print(f"WARNING: Cross-encoder failed to load: {exc}")
        return None


def _create_cv_chunks(cv_text: str) -> list[str]:
    """
    Split a long CV into overlapping fixed-size windows.

    Overlap prevents key sentences straddling a chunk boundary from being
    split across two chunks and weakened in both. The list is capped at
    CROSS_ENCODER_MAX_CHUNKS to bound Lambda execution time.
    """
    chunks = []
    step = CROSS_ENCODER_CHUNK_CHARS - CROSS_ENCODER_OVERLAP_CHARS
    start = 0
    while start < len(cv_text):
        end = start + CROSS_ENCODER_CHUNK_CHARS
        chunk = cv_text[start:end].strip()
        if chunk:
            chunks.append(chunk)
        if end >= len(cv_text):
            break
        start += step
    return chunks[:CROSS_ENCODER_MAX_CHUNKS]


def _aggregate_chunk_scores(normalized_scores: list[float]) -> float:
    """
    Weighted average of the top-K chunk scores.

    Weights [0.5, 0.3, 0.2] reward candidates with multiple strong sections.
    A candidate whose Experience section AND Skills section both match the JD
    scores higher than one with a single keyword-stuffed paragraph.

    Weights are renormalized if fewer than 3 chunks exist.
    """
    if not normalized_scores:
        return 0.0
    top_k = sorted(normalized_scores, reverse=True)[:CROSS_ENCODER_TOP_K]
    raw_weights = [0.5, 0.3, 0.2][: len(top_k)]
    weight_sum = sum(raw_weights)
    weights = [w / weight_sum for w in raw_weights]
    return sum(score * weight for score, weight in zip(top_k, weights))


def compute_cross_encoder_score(cv_text: str, jd_text: str) -> float | None:
    """
    Score the CV against the JD using the Cross-Encoder.

    Returns a float in [0, 1], or None if the model is unavailable.
    JD text is truncated to its opening section (highest requirement density).
    CV is chunked and each chunk is scored against the JD as the query.
    """
    ce = _get_cross_encoder()
    if ce is None:
        return None

    jd_query = jd_text[:CROSS_ENCODER_JD_QUERY_CHARS].strip()
    if not jd_query:
        return None

    cv_chunks = _create_cv_chunks(cv_text)
    if not cv_chunks:
        return None

    print(f"INFO: Cross-encoder scoring {len(cv_chunks)} chunk(s).")
    pairs = [(jd_query, chunk) for chunk in cv_chunks]
    raw_logits = ce.predict(pairs)
    normalized = [_sigmoid(float(logit)) for logit in raw_logits]
    final_score = _aggregate_chunk_scores(normalized)

    print(f"INFO: Cross-encoder chunk scores: {[round(s, 4) for s in normalized]}")
    print(f"INFO: Cross-encoder aggregated score: {round(final_score, 4)}")
    return final_score


# ---------------------------------------------------------------------------
# Hybrid Score Computation
# ---------------------------------------------------------------------------

def compute_hybrid_score(
    bi_score_pct: float, cross_score: float | None
) -> tuple[float, dict]:
    """
    Blend bi-encoder and cross-encoder scores into a single final score.

    Args:
        bi_score_pct:   Cosine similarity × 100 (0–100 scale).
        cross_score:    Sigmoid-normalized cross-encoder score [0, 1], or None.

    Returns:
        (final_score_pct, scoring_details)
        scoring_details is stored in DynamoDB for calibration analysis.
    """
    bi_normalized = bi_score_pct / 100.0

    if cross_score is None:
        final_pct = bi_score_pct
        details = {
            "bi_encoder_score": round(bi_score_pct, 2),
            "cross_encoder_score": None,
            "hybrid_score": round(final_pct, 2),
            "method": "bi_encoder_only",
            "note": "Cross-encoder unavailable; using bi-encoder score only.",
        }
    else:
        hybrid = HYBRID_WEIGHT_BI * bi_normalized + HYBRID_WEIGHT_CROSS * cross_score
        final_pct = round(hybrid * 100, 2)
        details = {
            "bi_encoder_score": round(bi_score_pct, 2),
            "cross_encoder_score": round(cross_score * 100, 2),
            "hybrid_score": final_pct,
            "bi_encoder_weight": HYBRID_WEIGHT_BI,
            "cross_encoder_weight": HYBRID_WEIGHT_CROSS,
            "method": "hybrid_bi_cross_encoder",
        }

    print(f"INFO: Hybrid scoring: {details}")
    return final_pct, details


# ---------------------------------------------------------------------------
# UPGRADE 3: Multi-Agent Interview Guide
# ---------------------------------------------------------------------------

def generate_interview_guide(
    parsed_result: dict,
    job_title: str,
    required_skills: list[str],
) -> list[dict]:
    """
    Second Claude agent: generate 3 targeted technical interview questions.

    This call is intentionally a separate LLM invocation from the scoring call.
    Separation of concerns: the scoring agent extracts facts objectively
    (temperature=0), while the interview guide agent synthesizes and creates
    (slightly higher temperature for question variety).

    Input: the gaps, claimed skills, seniority, and years_experience fields
    from the scoring agent's JSON output. The model never sees the raw CV text
    again — only the structured extraction — which keeps this prompt tight and
    focused.

    Output schema for each question:
      {
        "question": "Specific, challenging technical question",
        "skill_targeted": "The exact gap or skill being probed",
        "rationale": "One sentence: why this question exposes real vs. claimed knowledge"
      }

    Returns an empty list on any failure (non-fatal — DynamoDB record is still
    saved with the scoring data intact).
    """
    if not ENABLE_INTERVIEW_GUIDE:
        return []

    gaps = parsed_result.get("gaps", "")
    seniority = parsed_result.get("seniority_estimate", "Unknown")
    years_exp = parsed_result.get("years_experience", 0)
    claimed_backend = parsed_result.get("backend_skills", [])
    claimed_frontend = parsed_result.get("frontend_skills", [])
    claimed_devops = parsed_result.get("devops_skills", [])

    all_claimed = claimed_backend + claimed_frontend + claimed_devops

    system_prompt = f"""You are a Senior Technical Interviewer AI specializing in {job_title} roles.

A candidate has been evaluated for a {seniority} {job_title} position with {years_exp} year(s) of experience.

Your task is to generate exactly 3 highly specific, technical interview questions designed to probe the weaknesses and unverified claims in their CV.

These are NOT generic questions. Each question must:
1. Target a specific identified gap or claimed skill that needs validation.
2. Be appropriately difficult for a {seniority} candidate.
3. Have a clear, verifiable correct answer — not a vague "tell me about a time" question.
4. Reveal the difference between someone who truly knows the technology versus someone who has only used it superficially.

You MUST output a valid JSON array of exactly 3 objects. No markdown, no preamble. Output ONLY raw JSON.

Schema:
[
  {{
    "question": "The specific technical question to ask",
    "skill_targeted": "Name of the exact skill or gap this tests",
    "rationale": "One sentence explaining what a wrong answer reveals about their knowledge"
  }}
]"""

    user_message = f"""Candidate skill profile:
- Role: {job_title}
- Seniority: {seniority}
- Years of experience: {years_exp}
- Claimed technical skills: {", ".join(all_claimed) if all_claimed else "Not specified"}
- Required skills for role: {", ".join(required_skills) if required_skills else "See gaps below"}
- Identified knowledge gaps: {gaps if gaps else "No major gaps identified"}

Generate 3 targeted interview questions to validate this candidate."""

    payload = {
        "anthropic_version": "bedrock-2023-05-31",
        "max_tokens": 1200,
        "temperature": 0.3,
        "system": system_prompt,
        "messages": [{"role": "user", "content": user_message}],
    }

    try:
        response = bedrock.invoke_model(
            modelId=CLAUDE_MODEL_ID,
            contentType="application/json",
            accept="application/json",
            body=json.dumps(payload),
        )
        response_data = json.loads(response.get("body").read())
        model_text = response_data.get("content", [{}])[0].get("text", "[]")

        # Strip potential markdown code fences before parsing JSON
        cleaned = model_text.strip()
        if cleaned.startswith("```"):
            cleaned = cleaned.split("```")[1]
            if cleaned.startswith("json"):
                cleaned = cleaned[4:]
        cleaned = cleaned.strip()

        questions = json.loads(cleaned)

        if isinstance(questions, list) and all(
            isinstance(q, dict) and "question" in q for q in questions
        ):
            print(f"INFO: Interview guide generated: {len(questions)} question(s).")
            return questions[:3]  # Enforce exactly 3
        else:
            print("WARNING: Interview guide returned unexpected shape. Skipping.")
            return []

    except Exception as exc:
        print(f"WARNING: Interview guide generation failed (non-fatal): {exc}")
        return []


# ---------------------------------------------------------------------------
# UPGRADE 4: OpenSearch Talent Pool Indexing
# ---------------------------------------------------------------------------

_opensearch_client = None


def _get_opensearch_client():
    """
    Build and cache the OpenSearch client with AWS Signature V4 auth.

    Uses `aoss` (OpenSearch Serverless) as the service name. If you are using
    a standard OpenSearch Service domain (not Serverless), change the service
    to `es`.

    Returns None if OPENSEARCH_ENDPOINT is not configured or on any error.
    OpenSearch indexing is non-blocking: a failure here does NOT prevent the
    DynamoDB write or the response back to the caller.
    """
    global _opensearch_client

    if _opensearch_client is not None:
        return _opensearch_client

    if not OPENSEARCH_ENDPOINT or not ENABLE_OPENSEARCH:
        return None

    try:
        from opensearchpy import OpenSearch, RequestsHttpConnection, AWSV4SignerAuth  # type: ignore
        import boto3 as _boto3

        credentials = _boto3.Session().get_credentials()
        auth = AWSV4SignerAuth(credentials, OPENSEARCH_REGION, "aoss")

        _opensearch_client = OpenSearch(
            hosts=[{"host": OPENSEARCH_ENDPOINT, "port": 443}],
            http_auth=auth,
            use_ssl=True,
            verify_certs=True,
            connection_class=RequestsHttpConnection,
            timeout=10,
        )
        print(f"INFO: OpenSearch client initialized: {OPENSEARCH_ENDPOINT}")
        return _opensearch_client

    except ImportError:
        print(
            "WARNING: opensearch-py not installed. Talent pool indexing disabled. "
            "Add 'opensearch-py requests requests-aws4auth' to requirements.txt."
        )
        return None
    except Exception as exc:
        print(f"WARNING: OpenSearch client failed to initialize: {exc}")
        return None


def index_candidate_to_talent_pool(
    candidate_id: str,
    job_id: str,
    cv_vector: list[float],
    parsed_result: dict,
    final_score: float,
) -> bool:
    """
    Index the candidate's CV vector and structured profile into OpenSearch.

    The document stored in OpenSearch is intentionally minimal — only the
    fields needed for search ranking and result display. The full parsed_result
    stays in DynamoDB. The cv_vector is the original float vector (before
    Decimal conversion), stored natively as a JSON array for kNN queries.

    Index mapping required (create once during infrastructure setup):
    {
      "settings": {"index": {"knn": true, "knn.space_type": "cosinesimil"}},
      "mappings": {
        "properties": {
          "cv_vector":       {"type": "knn_vector", "dimension": 1024},
          "candidateId":     {"type": "keyword"},
          "jobId":           {"type": "keyword"},
          "seniority":       {"type": "keyword"},
          "frontend_skills": {"type": "keyword"},
          "backend_skills":  {"type": "keyword"},
          "devops_skills":   {"type": "keyword"},
          "soft_skills":     {"type": "keyword"},
          "years_experience":{"type": "integer"},
          "matching_score":  {"type": "float"},
          "updatedAt":       {"type": "date"}
        }
      }
    }

    Returns True on success, False on failure (non-fatal).
    """
    os_client = _get_opensearch_client()
    if os_client is None:
        return False

    doc = {
        "candidateId": candidate_id,
        "jobId": job_id,
        "cv_vector": cv_vector,
        "seniority": parsed_result.get("seniority_estimate", "Unknown"),
        "frontend_skills": parsed_result.get("frontend_skills", []),
        "backend_skills": parsed_result.get("backend_skills", []),
        "devops_skills": parsed_result.get("devops_skills", []),
        "soft_skills": parsed_result.get("soft_skills", []),
        "years_experience": parsed_result.get("years_experience", 0),
        "matching_score": final_score,
        "updatedAt": datetime.now(timezone.utc).isoformat(),
    }

    # Document ID is composite so re-processing the same CV upserts correctly.
    doc_id = f"{candidate_id}_{job_id}"

    try:
        os_client.index(index=OPENSEARCH_INDEX, id=doc_id, body=doc)
        print(f"INFO: Candidate {candidate_id} indexed in talent pool (doc_id={doc_id}).")
        return True
    except Exception as exc:
        print(f"WARNING: OpenSearch indexing failed for {candidate_id}: {exc}")
        return False


def search_talent_pool(natural_language_query: str, top_k: int = 5) -> list[dict]:
    """
    Search the talent pool using a natural language query.

    This function is designed to be called by a separate search Lambda, not
    during CV processing. It is included here as a utility and reference
    implementation for the search endpoint.

    Example:
        results = search_talent_pool(
            "mid-level frontend developer with React and TypeScript, "
            "experienced migrating legacy jQuery codebases"
        )

    Process:
        1. Embed the query using Cohere (input_type="search_query" for asymmetric search)
        2. kNN query against the cv_vector field in OpenSearch
        3. Return top_k hits with candidate metadata

    Note: Uses input_type="search_query" (different from "search_document" used
    for CV/JD indexing). This asymmetric setup is intentional: Cohere's
    embed-v3 model is trained to align query vectors with document vectors
    when different input_type values are used, improving recall.
    """
    os_client = _get_opensearch_client()
    if os_client is None:
        return []

    # Embed the search query
    try:
        query_payload = {
            "texts": [natural_language_query[:2048]],
            "input_type": "search_query",  # Asymmetric: query vs. search_document
            "truncate": "END",
        }
        response = bedrock.invoke_model(
            modelId=COHERE_MODEL_ID,
            contentType="application/json",
            accept="application/json",
            body=json.dumps(query_payload),
        )
        query_vector = json.loads(response.get("body").read())["embeddings"][0]
    except Exception as exc:
        print(f"ERROR: Failed to embed search query: {exc}")
        return []

    # kNN search against indexed CV vectors
    search_body = {
        "size": top_k,
        "query": {
            "knn": {
                "cv_vector": {
                    "vector": query_vector,
                    "k": top_k,
                }
            }
        },
        "_source": {
            "excludes": ["cv_vector"]  # Don't return the raw vector in results
        },
    }

    try:
        response = os_client.search(index=OPENSEARCH_INDEX, body=search_body)
        hits = response.get("hits", {}).get("hits", [])
        results = [
            {
                "candidateId": hit["_source"].get("candidateId"),
                "jobId": hit["_source"].get("jobId"),
                "seniority": hit["_source"].get("seniority"),
                "matching_score": hit["_source"].get("matching_score"),
                "frontend_skills": hit["_source"].get("frontend_skills", []),
                "backend_skills": hit["_source"].get("backend_skills", []),
                "devops_skills": hit["_source"].get("devops_skills", []),
                "years_experience": hit["_source"].get("years_experience"),
                "search_score": hit.get("_score"),
            }
            for hit in hits
        ]
        print(f"INFO: Talent pool search returned {len(results)} result(s).")
        return results
    except Exception as exc:
        print(f"ERROR: OpenSearch kNN search failed: {exc}")
        return []


# ---------------------------------------------------------------------------
# Utility Helpers (unchanged)
# ---------------------------------------------------------------------------

def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def expires_at_epoch() -> int:
    return int(
        (datetime.now(timezone.utc) + timedelta(days=PARSE_STATUS_TTL_DAYS)).timestamp()
    )


def parse_json_if_string(value):
    if isinstance(value, str):
        return json.loads(value)
    return value


def decode_s3_key(raw_key: str) -> str:
    return urllib.parse.unquote_plus(raw_key)


def extract_job_id_and_candidate_from_key(object_key: str) -> tuple[str, str]:
    parts = object_key.split("/")
    try:
        idx = parts.index("candidates")
        candidate_id = parts[idx + 1] if len(parts) > idx + 1 else "unknown"
        job_id = parts[idx + 2] if len(parts) > idx + 2 else "unknown"
        return candidate_id, job_id
    except ValueError:
        return "unknown", "unknown"


def safe_json_parse(text: str) -> dict:
    try:
        return json.loads(text)
    except Exception:
        return {
            "summary": "Model output was not valid JSON",
            "rawModelOutput": text,
            "frontend_skills": [],
            "backend_skills": [],
            "devops_skills": [],
            "soft_skills": [],
            "years_experience": 0,
            "matching_score": 0,
            "strengths": "",
            "gaps": "",
        }


# ---------------------------------------------------------------------------
# DynamoDB: Job Data Fetch with Cached JD Vector
# ---------------------------------------------------------------------------

def get_jd_data_from_db(job_id: str) -> dict:
    """
    Fetch JD text, title, required skills, AND cached embedding from DynamoDB.

    The `jdVector` field is populated by jd_parser.py when the JD is first
    parsed. If present, cv_parser uses it directly and skips the Cohere API
    call for the JD. If absent (e.g., JD was parsed before this upgrade was
    deployed), the field is None and cv_parser falls back to embedding on demand.
    """
    default_data = {
        "jdText": "Evaluate general technical skills.",
        "jobTitle": "Unknown",
        "requiredSkills": [],
        "jdVector": None,
    }
    try:
        table = dynamodb.Table(DYNAMODB_JOBS_TABLE)
        response = table.get_item(Key={"jobId": str(job_id)})
        if "Item" in response:
            item = response["Item"]
            raw_vector = item.get("jdVector")
            # Convert DynamoDB Decimal back to float for math operations
            jd_vector = [float(v) for v in raw_vector] if raw_vector else None
            return {
                "jdText": item.get("jdText", default_data["jdText"]),
                "jobTitle": item.get("jobTitle", default_data["jobTitle"]),
                "requiredSkills": item.get("requiredSkills", default_data["requiredSkills"]),
                "jdVector": jd_vector,
            }
    except Exception as e:
        print(f"WARNING: Failed to fetch JD from DB for jobId {job_id}: {str(e)}")
    return default_data


# ---------------------------------------------------------------------------
# SQS Payload Extraction (updated to return jdVector from get_jd_data_from_db)
# ---------------------------------------------------------------------------

def extract_job_payload(record: dict) -> dict:
    body = parse_json_if_string(record.get("body", "{}"))

    # Shape A: custom queue payload from Candidate service
    if isinstance(body, dict) and body.get("profileId") and body.get("fileKey"):
        jd_data = get_jd_data_from_db(body.get("jobId", "unknown"))
        if body.get("jdText"):
            jd_data["jdText"] = body.get("jdText")
        return {
            "profile_id": str(body["profileId"]),
            "job_id": str(body.get("jobId", "unknown")),
            "file_key": str(body["fileKey"]),
            "jd_text": jd_data["jdText"],
            "job_title": jd_data["jobTitle"],
            "required_skills": jd_data["requiredSkills"],
            "jd_vector": jd_data["jdVector"],
            "bucket": body.get("bucketName") or CV_BUCKET_NAME,
        }

    # Shape B: direct S3 event in SQS
    if isinstance(body, dict):
        records = body.get("Records", [])
        if records and isinstance(records[0], dict):
            s3 = records[0].get("s3", {})
            bucket = s3.get("bucket", {}).get("name")
            raw_key = s3.get("object", {}).get("key")
            if bucket and raw_key:
                file_key = decode_s3_key(raw_key)
                candidate_id, job_id = extract_job_id_and_candidate_from_key(file_key)
                jd_data = get_jd_data_from_db(job_id)
                return {
                    "profile_id": candidate_id,
                    "job_id": job_id,
                    "file_key": file_key,
                    "jd_text": jd_data["jdText"],
                    "job_title": jd_data["jobTitle"],
                    "required_skills": jd_data["requiredSkills"],
                    "jd_vector": jd_data["jdVector"],
                    "bucket": bucket,
                }

    # Shape C: SNS wrapped S3 event in SQS
    if isinstance(body, dict) and body.get("Message"):
        sns_message = parse_json_if_string(body.get("Message"))
        if isinstance(sns_message, dict):
            records = sns_message.get("Records", [])
            if records and isinstance(records[0], dict):
                s3 = records[0].get("s3", {})
                bucket = s3.get("bucket", {}).get("name")
                raw_key = s3.get("object", {}).get("key")
                if bucket and raw_key:
                    file_key = decode_s3_key(raw_key)
                    candidate_id, job_id = extract_job_id_and_candidate_from_key(file_key)
                    jd_data = get_jd_data_from_db(job_id)
                    return {
                        "profile_id": candidate_id,
                        "job_id": job_id,
                        "file_key": file_key,
                        "jd_text": jd_data["jdText"],
                        "job_title": jd_data["jobTitle"],
                        "required_skills": jd_data["requiredSkills"],
                        "jd_vector": jd_data["jdVector"],
                        "bucket": bucket,
                    }

    raise ValueError("SQS message is not recognized. Need custom payload or valid S3 event.")


# ---------------------------------------------------------------------------
# Bi-Encoder: Cohere Embeddings + Cosine Similarity
# ---------------------------------------------------------------------------

def get_cohere_embedding(text: str) -> list[float]:
    """Embed text using Cohere embed-v3 via Bedrock. Max 2048 chars."""
    normalized = (text or "").strip()
    safe_text = normalized[:2048] if normalized else "No content"
    payload = {
        "texts": [safe_text],
        "input_type": "search_document",
        "truncate": "END",
    }
    response = bedrock.invoke_model(
        modelId=COHERE_MODEL_ID,
        contentType="application/json",
        accept="application/json",
        body=json.dumps(payload),
    )
    response_body = json.loads(response.get("body").read())
    return response_body["embeddings"][0]


def calculate_cosine_similarity(vec1: list[float], vec2: list[float]) -> float:
    dot_product = sum(a * b for a, b in zip(vec1, vec2))
    magnitude1 = math.sqrt(sum(a * a for a in vec1))
    magnitude2 = math.sqrt(sum(b * b for b in vec2))
    if magnitude1 == 0 or magnitude2 == 0:
        return 0.0
    return dot_product / (magnitude1 * magnitude2)


# ---------------------------------------------------------------------------
# Claude Agent 1: CV Skill Extraction & Scoring
# ---------------------------------------------------------------------------

def parse_and_evaluate_cv(
    masked_cv_text: str,
    jd_text: str,
    job_title: str,
    required_skills: list[str],
    final_match_score: float,
) -> dict:
    """
    First Claude agent: extract structured skills and write strengths/gaps.

    Receives the MASKED CV text (PII already removed by spaCy). The final
    hybrid score is passed in as ground truth — Claude cannot override it.
    """
    system_prompt = f"""You are an elite Senior Technical Recruiter AI.

CRITICAL INSTRUCTION: A deterministic Machine Learning engine (Bi-Encoder + Cross-Encoder hybrid) has already scored this candidate against the Job Description.
The official Match Score is exactly {final_match_score}%.
You MUST output this exact score in the "matching_score" field. Do not invent your own score.

You are evaluating a candidate for the following specific role: "{job_title}".
The primary required skills for this role are: {", ".join(required_skills) if required_skills else "Not specifically provided, derive from JD text"}.

Note: Candidate names, organization names, and locations have been anonymized with neutral tokens (e.g., [CANDIDATE_NAME], [ORGANIZATION], [LOCATION]) to ensure unbiased evaluation. Evaluate based on technical skills and experience only.

Using the ML score as your baseline truth, extract their skills and write a professional summary of their Strengths and Gaps specifically related to the "{job_title}" role requirements.

You MUST output the result strictly as a valid JSON object.
Do NOT include any conversational text or markdown. Output ONLY raw JSON.

Strict JSON Schema:
{{
  "seniority_estimate": "Junior/Mid/Senior",
  "frontend_skills": ["array of strings"],
  "backend_skills": ["array of strings"],
  "devops_skills": ["array of strings"],
  "soft_skills": ["array of strings"],
  "years_experience": 0,
  "matching_score": {final_match_score},
  "strengths": "Short paragraph explaining why they are a fit for {job_title}.",
  "gaps": "Short paragraph explaining what required skills they are missing for {job_title}."
}}"""

    user_message = (
        f"<job_description>\n{jd_text}\n</job_description>\n"
        f"<resume>\n{masked_cv_text[:120000]}\n</resume>"
    )
    payload = {
        "anthropic_version": "bedrock-2023-05-31",
        "max_tokens": 1500,
        "temperature": 0.0,
        "system": system_prompt,
        "messages": [{"role": "user", "content": user_message}],
    }
    response = bedrock.invoke_model(
        modelId=CLAUDE_MODEL_ID,
        contentType="application/json",
        accept="application/json",
        body=json.dumps(payload),
    )
    response_data = json.loads(response.get("body").read())
    model_text = response_data.get("content", [{}])[0].get("text", "{}")
    return safe_json_parse(model_text)


# ---------------------------------------------------------------------------
# Textract: CV Text Extraction (unchanged)
# ---------------------------------------------------------------------------

def extract_text_from_s3(bucket: str, key: str) -> str:
    response = textract.start_document_text_detection(
        DocumentLocation={"S3Object": {"Bucket": bucket, "Name": key}}
    )
    job_id = response["JobId"]

    while True:
        result = textract.get_document_text_detection(JobId=job_id, MaxResults=1000)
        status = result.get("JobStatus")
        if status == "SUCCEEDED":
            break
        if status == "FAILED":
            raise RuntimeError("Textract job failed")
        time.sleep(2)

    lines = []
    next_token = None
    while True:
        kwargs = {"JobId": job_id, "MaxResults": 1000}
        if next_token:
            kwargs["NextToken"] = next_token
        page = textract.get_document_text_detection(**kwargs)
        for block in page.get("Blocks", []):
            if block.get("BlockType") == "LINE" and block.get("Text"):
                lines.append(block["Text"])
        next_token = page.get("NextToken")
        if not next_token:
            break

    return " ".join(lines)


# ---------------------------------------------------------------------------
# DynamoDB: Result Persistence (updated schema)
# ---------------------------------------------------------------------------

def save_to_dynamodb(
    profile_id: str,
    file_key: str,
    parsed_data: dict,
    success: bool,
    error_message: str = "",
    scoring_details: dict | None = None,
    interview_guide: list | None = None,
    masking_report: dict | None = None,
) -> None:
    """
    Persist full CV processing result to DynamoDB.

    New fields vs. original:
      scoringDetails   — bi/cross/hybrid scores + method used
      interviewGuide   — [{question, skill_targeted, rationale}] × 3
      maskingReport    — PII entity counts (no actual PII values stored)
    """
    table = dynamodb.Table(DYNAMODB_TABLE)
    parsed_data = json.loads(json.dumps(parsed_data), parse_float=Decimal)

    def to_decimal(obj):
        return json.loads(json.dumps(obj or {}), parse_float=Decimal)

    status = "SUCCEEDED" if success else "FAILED"
    item = {
        "candidateId": str(profile_id),
        "objectKey": str(file_key),
        "parseStatus": status,
        "updatedAt": now_iso(),
        "expiresAt": expires_at_epoch(),
        "parsedResult": parsed_data,
        "errorMessage": error_message,
        "scoringDetails": to_decimal(scoring_details),
        "interviewGuide": interview_guide or [],
        "maskingReport": masking_report or {},
        # Legacy fields for backward compatibility with downstream readers
        "ProfileId": str(profile_id),
        "ProcessSuccess": success,
        "ProcessedAt": int(time.time()),
    }

    table.put_item(Item=item)


# ---------------------------------------------------------------------------
# Core Processing Logic
# ---------------------------------------------------------------------------

def process_record(record: dict) -> None:
    """
    Full pipeline for a single SQS record.

    Execution order:
      [1] Extract payload (S3 key, JD data, candidate ID)
      [2] Textract: extract raw CV text from S3
      [3] Blind Screen: spaCy NER masks PII → masked_cv_text
      [4] Bi-Encoder: Cohere embeds CV; reuse cached JD vector if available
      [5] Cross-Encoder: chunked CV × JD → cross_score
      [6] Hybrid Blend: 0.35 × bi + 0.65 × cross → final_score
      [7] Claude Agent 1: extract skills/strengths/gaps from masked CV
      [8] Claude Agent 2: generate 3 targeted interview questions from gaps
      [9] DynamoDB: persist full result including all new fields
     [10] OpenSearch: index CV vector + profile for talent pool search
    """
    payload = extract_job_payload(record)
    profile_id = payload["profile_id"]
    job_id = payload["job_id"]
    file_key = payload["file_key"]
    jd_text = payload["jd_text"]
    job_title = payload["job_title"]
    required_skills = payload["required_skills"]
    cached_jd_vector = payload["jd_vector"]
    bucket = payload["bucket"]

    if not bucket:
        raise ValueError(
            "Missing bucket name. Provide bucketName in SQS body or CV_BUCKET_NAME env var."
        )
    if profile_id == "unknown":
        raise ValueError("Cannot derive candidate profile id from message/file key.")

    print(f"INFO: Processing CV for ProfileId={profile_id}, JobId={job_id}, key={file_key}")

    # ── [1] Textract: extract raw CV text ────────────────────────────────────
    raw_cv_text = extract_text_from_s3(bucket, file_key)

    # ── [2] Blind Screening: mask PII before any AI processing ───────────────
    # The original raw_cv_text is discarded after this point. All downstream
    # AI calls use masked_cv_text only.
    print("INFO: Running blind screening (PII masking).")
    masked_cv_text, masking_report = mask_pii_entities(raw_cv_text)

    # ── [3] Bi-Encoder: embed masked CV; reuse cached JD vector ─────────────
    print("INFO: Running Stage 1 — Bi-Encoder (Cohere).")
    cv_vector = get_cohere_embedding(masked_cv_text)

    if cached_jd_vector is not None:
        print("INFO: Using cached JD vector from DynamoDB (saved by jd_parser).")
        jd_vector = cached_jd_vector
    else:
        print("INFO: JD vector not cached; computing embedding now.")
        jd_vector = get_cohere_embedding(jd_text)

    bi_raw = calculate_cosine_similarity(cv_vector, jd_vector)
    bi_score_pct = round(bi_raw * 100, 2)
    print(f"INFO: Bi-Encoder score: {bi_score_pct}%")

    # ── [4] Cross-Encoder: attention-based reranking ──────────────────────────
    print("INFO: Running Stage 2 — Cross-Encoder (ms-marco reranking).")
    cross_score = compute_cross_encoder_score(masked_cv_text, jd_text)
    if cross_score is not None:
        print(f"INFO: Cross-Encoder score: {round(cross_score * 100, 2)}%")

    # ── [5] Hybrid blend ──────────────────────────────────────────────────────
    final_score, scoring_details = compute_hybrid_score(bi_score_pct, cross_score)
    print(f"INFO: Final hybrid score: {final_score}%")

    # ── [6] Claude Agent 1: skill extraction ──────────────────────────────────
    print("INFO: Running Claude Agent 1 (skill extraction + scoring).")
    parsed_data = parse_and_evaluate_cv(
        masked_cv_text, jd_text, job_title, required_skills, final_score
    )
    if "matching_score" not in parsed_data:
        parsed_data["matching_score"] = final_score

    # ── [7] Claude Agent 2: interview guide ───────────────────────────────────
    print("INFO: Running Claude Agent 2 (interview question generation).")
    interview_guide = generate_interview_guide(parsed_data, job_title, required_skills)

    # ── [8] DynamoDB persistence ───────────────────────────────────────────────
    save_to_dynamodb(
        profile_id=profile_id,
        file_key=file_key,
        parsed_data=parsed_data,
        success=True,
        scoring_details=scoring_details,
        interview_guide=interview_guide,
        masking_report=masking_report,
    )
    print(f"INFO: DynamoDB record saved for ProfileId={profile_id}.")

    # ── [9] OpenSearch talent pool indexing (non-blocking) ────────────────────
    # cv_vector is the pre-masking float list. PII masking affects only text
    # passed to LLMs — the vector itself is derived from masked text so it
    # contains no raw PII either (embeddings are not reversible to original text).
    indexed = index_candidate_to_talent_pool(
        candidate_id=profile_id,
        job_id=job_id,
        cv_vector=cv_vector,
        parsed_result=parsed_data,
        final_score=final_score,
    )
    if indexed:
        print(f"INFO: Candidate {profile_id} added to talent pool index.")


# ---------------------------------------------------------------------------
# Lambda Entry Point
# ---------------------------------------------------------------------------

def lambda_handler(event, context):
    records = event.get("Records", [])
    if not records:
        return {"statusCode": 200, "body": "No records to process"}

    for record in records:
        profile_id = "unknown"
        file_key = "unknown"
        try:
            payload = extract_job_payload(record)
            profile_id = payload["profile_id"]
            file_key = payload["file_key"]
            process_record(record)
        except Exception as exc:
            print(
                json.dumps({
                    "level": "error",
                    "message": "Candidate CV processing failed",
                    "profileId": profile_id,
                    "fileKey": file_key,
                    "error": str(exc),
                    "bodyPreview": str(record.get("body", ""))[:500],
                })
            )
            if profile_id != "unknown" and file_key != "unknown":
                save_to_dynamodb(
                    profile_id, file_key, {}, success=False, error_message=str(exc)
                )
            # Re-raise so SQS can retry and route to DLQ after max attempts
            raise

    return {"statusCode": 200, "body": "Success"}
