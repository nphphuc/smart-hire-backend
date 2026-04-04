# =============================================================================
# jd_parser.py — SmartHire AI | Job Description Parsing Pipeline
#
# Upgrade summary vs original:
#
#   NEW — JD Vector Caching:
#     After Claude parses the JD, this lambda immediately generates a Cohere
#     embedding of the cleaned JD text and stores it as `jdVector` in DynamoDB.
#
#     Why: cv_parser.py processes every applicant independently. Without this
#     cache, 100 candidates applying to the same job = 100 identical Cohere API
#     calls for the same JD text. With the cache, cv_parser checks DynamoDB
#     first and re-uses the pre-computed vector. One JD → one embedding call,
#     regardless of applicant volume.
#
#     DynamoDB field added: `jdVector` (List<Decimal>)
#     — Stored as Decimal because DynamoDB rejects Python float.
#     — cv_parser casts back to float on retrieval.
# =============================================================================

import base64
import json
import os
import urllib.parse
from datetime import datetime, timezone
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

s3_client = boto3.client("s3", region_name="ap-southeast-1")
bedrock = boto3.client("bedrock-runtime", config=retry_config)
dynamodb = boto3.resource("dynamodb", region_name="ap-southeast-1")


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

CLAUDE_MODEL_ID = os.environ.get(
    "CLAUDE_MODEL_ID", "apac.anthropic.claude-3-5-sonnet-20241022-v2:0"
)
DYNAMODB_JOBS_TABLE = os.environ.get("DYNAMODB_JOBS_TABLE", "SmartHire_Jobs")

# Cohere model used for JD vector embedding (must match the model used in
# cv_parser.py so vectors live in the same embedding space for cosine comparison).
COHERE_MODEL_ID = os.environ.get("COHERE_MODEL_ID", "cohere.embed-english-v3")

# How many characters of the JD to embed. The full cleaned_jd_text can be very
# long; Cohere embed-v3 enforces a 2048-char limit per text. We take the opening
# section which contains the highest-signal content (requirements, skills lists).
JD_EMBED_CHARS = int(os.environ.get("JD_EMBED_CHARS", "2048"))


# ---------------------------------------------------------------------------
# Utility Helpers
# ---------------------------------------------------------------------------

def now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def get_metadata_from_s3(bucket: str, key: str) -> dict:
    """Read S3 object metadata headers (set by the .NET upload service)."""
    try:
        response = s3_client.head_object(Bucket=bucket, Key=key)
        return response.get("Metadata", {})
    except Exception as e:
        print(f"WARNING: Could not fetch metadata for {key}: {str(e)}")
        return {}


# ---------------------------------------------------------------------------
# JD Vector Embedding (NEW)
# ---------------------------------------------------------------------------

def generate_jd_embedding(jd_text: str) -> list[float] | None:
    """
    Embed the cleaned JD text using Cohere via Bedrock.

    Returns a list of floats (the embedding vector), or None on failure.
    Failures are non-fatal: cv_parser will fall back to computing the
    embedding itself if `jdVector` is absent from DynamoDB.

    The `input_type` is set to "search_document" to match the setting used
    for CV embeddings in cv_parser, keeping both vectors in the same space.
    Using mismatched input_types (e.g., "search_query" for JD vs
    "search_document" for CV) would corrupt cosine similarity scores.
    """
    try:
        normalized = (jd_text or "").strip()
        safe_text = normalized[:JD_EMBED_CHARS] if normalized else "No content"

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
        vector = response_body["embeddings"][0]
        print(f"INFO: JD embedding generated, dimension={len(vector)}")
        return vector

    except Exception as exc:
        print(f"WARNING: JD embedding failed (non-fatal): {exc}")
        return None


def vector_to_decimal(vector: list[float]) -> list[Decimal]:
    """
    Convert a float embedding vector to a list of Decimal for DynamoDB storage.

    DynamoDB's Python SDK raises TypeError on Python float. Decimal is the
    required type for numeric attributes. We round to 8 significant digits
    to stay within DynamoDB's 38-digit Decimal precision limit while keeping
    enough fidelity for cosine similarity calculations.
    """
    return [Decimal(str(round(v, 8))) for v in vector]


# ---------------------------------------------------------------------------
# JD Parsing: Claude Vision/PDF (unchanged logic, cleaned up)
# ---------------------------------------------------------------------------

def extract_and_structure_jd_with_claude_pdf(bucket: str, key: str) -> dict:
    """
    Send the raw JD file (PDF or image) to Claude via Bedrock's document API.

    Claude reads the visual layout directly, preserving heading hierarchy,
    bold text, and bullet structure in the returned `cleaned_jd_text` field.
    This is superior to Textract for JDs because layout context (e.g., a
    bold "Required Skills" heading) improves Claude's extraction accuracy.
    """
    response = s3_client.get_object(Bucket=bucket, Key=key)
    file_bytes = response["Body"].read()
    b64_data = base64.b64encode(file_bytes).decode("utf-8")

    # Detect MIME type from file extension
    media_type = "application/pdf"
    lower_key = key.lower()
    if lower_key.endswith(".png"):
        media_type = "image/png"
    elif lower_key.endswith(".jpg") or lower_key.endswith(".jpeg"):
        media_type = "image/jpeg"

    system_prompt = """You are an elite HR Technical AI.
You are directly viewing a Job Description document.
Extract the structural content and output it as valid JSON strictly adhering to the following schema:
{
  "job_title": "String",
  "seniority": "String (e.g. Junior, Mid-Level, Senior, Lead)",
  "required_skills": ["array of key skills extracted"],
  "cleaned_jd_text": "A comprehensive, beautifully formatted Markdown text representing the entire role. You MUST preserve the exact visual structure you see in the document. Ensure Headers (`#`, `##`) are accurately sized, Paragraphs are explicitly separated by `\\n\\n`, Bullet points uses `-` instead of raw spaces, and crucially: preserve bold text using `**bold**` whenever you see bold/heavy typography in the source document."
}"""

    content_block = []
    if media_type == "application/pdf":
        content_block.append({
            "type": "document",
            "source": {"type": "base64", "media_type": "application/pdf", "data": b64_data},
        })
    else:
        content_block.append({
            "type": "image",
            "source": {"type": "base64", "media_type": media_type, "data": b64_data},
        })
    content_block.append({
        "type": "text",
        "text": "Analyze this document and output exactly the JSON structure.",
    })

    payload = {
        "anthropic_version": "bedrock-2023-05-31",
        "max_tokens": 4096,
        "temperature": 0.0,
        "system": system_prompt,
        "messages": [{"role": "user", "content": content_block}],
    }

    response = bedrock.invoke_model(
        modelId=CLAUDE_MODEL_ID,
        contentType="application/json",
        accept="application/json",
        body=json.dumps(payload),
    )
    response_data = json.loads(response.get("body").read())
    model_text = response_data.get("content", [{}])[0].get("text", "{}")

    try:
        return json.loads(model_text)
    except Exception:
        return {
            "job_title": "Unknown",
            "seniority": "Unknown",
            "required_skills": [],
            "cleaned_jd_text": "Failed parsing document.",
        }


# ---------------------------------------------------------------------------
# Lambda Entry Point
# ---------------------------------------------------------------------------

def lambda_handler(event, context):
    jobs_to_process = []

    # Scenario A: S3 Event Trigger (direct notification from S3 bucket)
    if "Records" in event and event["Records"][0].get("eventSource") == "aws:s3":
        for record in event["Records"]:
            bucket = record["s3"]["bucket"]["name"]
            raw_key = record["s3"]["object"]["key"]
            file_key = urllib.parse.unquote_plus(raw_key)
            metadata = get_metadata_from_s3(bucket, file_key)
            job_id = metadata.get(
                "jobid", file_key.split("/")[-1].rsplit(".", 1)[0]
            )
            jobs_to_process.append({
                "jobId": job_id,
                "bucketName": bucket,
                "fileKey": file_key,
                "metadata": metadata,
            })

    # Scenario B: Direct Lambda Invoke from .NET backend (synchronous API call)
    elif "jobId" in event and "fileKey" in event:
        jobs_to_process.append({
            "jobId": event["jobId"],
            "bucketName": event.get("bucketName", os.environ.get("CV_BUCKET_NAME")),
            "fileKey": event["fileKey"],
            "metadata": {},
        })
    else:
        return {
            "statusCode": 400,
            "body": "Unrecognized event format. Need S3 event or direct JSON payload.",
        }

    for job in jobs_to_process:
        job_id = job["jobId"]
        bucket = job["bucketName"]
        file_key = job["fileKey"]
        metadata = job.get("metadata", {})

        print(f"INFO: Parsing JD for JobId={job_id} from s3://{bucket}/{file_key}")

        try:
            # ── Step 1: Claude parses the JD PDF/image ──────────────────────
            structured_jd = extract_and_structure_jd_with_claude_pdf(bucket, file_key)

            # ── Step 2: Generate and cache the JD embedding (NEW) ───────────
            # This pre-computes the JD vector so cv_parser can reuse it for
            # every applicant without redundant Bedrock API calls.
            cleaned_jd_text = structured_jd.get("cleaned_jd_text", "")
            jd_vector = generate_jd_embedding(cleaned_jd_text)

            # ── Step 3: Resolve final field values (metadata takes priority) ─
            raw_job_title = metadata.get("jobtitle")
            final_job_title = (
                urllib.parse.unquote(raw_job_title)
                if raw_job_title and raw_job_title != "Unknown"
                else structured_jd.get("job_title", "Unknown")
            )

            raw_seniority = metadata.get("seniority")
            final_seniority = (
                urllib.parse.unquote(raw_seniority)
                if raw_seniority and raw_seniority != "Unknown"
                else structured_jd.get("seniority", "Unknown")
            )

            raw_company_name = metadata.get("companyname")
            final_company_name = (
                urllib.parse.unquote(raw_company_name)
                if raw_company_name and raw_company_name != "Unknown"
                else "SmartHire Network"
            )

            # ── Step 4: Persist to DynamoDB ──────────────────────────────────
            table = dynamodb.Table(DYNAMODB_JOBS_TABLE)

            item = {
                "jobId": str(job_id),
                "originalFileKey": str(file_key),
                "jobTitle": final_job_title,
                "companyName": final_company_name,
                "seniority": final_seniority,
                "requiredSkills": structured_jd.get("required_skills", []),
                "jdText": cleaned_jd_text or "Failed resolving text.",
                "parseStatus": "SUCCEEDED",
                "updatedAt": now_iso(),
            }

            # Attach the cached JD vector if embedding succeeded.
            # cv_parser will try to fetch `jdVector` from this item first
            # before computing its own embedding, saving one Bedrock API call
            # per candidate application.
            if jd_vector is not None:
                item["jdVector"] = vector_to_decimal(jd_vector)
                print(f"INFO: JD vector cached in DynamoDB for JobId={job_id}")
            else:
                print(
                    f"WARNING: JD vector could not be generated for JobId={job_id}. "
                    f"cv_parser will compute the embedding per-candidate as fallback."
                )

            table.put_item(Item=item)
            print(f"SUCCESS: JD {job_id} saved to DynamoDB.")

        except Exception as exc:
            print(f"ERROR: Failed processing JD {job_id}: {str(exc)}")
            raise

    return {"statusCode": 200, "body": "Successfully processed JD upload."}
