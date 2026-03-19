using System.Text;
using System.Text.Json;
using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.EntityFrameworkCore;
using The_Hirelo.Data;

namespace The_Hirelo.Services
{
    public class CVParseService : ICVParseService
    {
        private readonly IAmazonTextract _textract;
        private readonly IAmazonBedrockRuntime _bedrock;
        private readonly HireloDbContext _context;
        private readonly ILogger<CVParseService> _logger;

        private const string ModelId = "anthropic.claude-3-5-sonnet-20240620-v1:0";

        public CVParseService(
            IAmazonTextract textract,
            IAmazonBedrockRuntime bedrock,
            HireloDbContext context,
            ILogger<CVParseService> logger)
        {
            _textract = textract;
            _bedrock = bedrock;
            _context = context;
            _logger = logger;
        }

        public async Task<string> ExtractRawTextAsync(string bucketName, string fileKey)
        {
            var response = await _textract.DetectDocumentTextAsync(new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    S3Object = new S3Object { Bucket = bucketName, Name = fileKey }
                }
            });

            var rawText = string.Join(" ", response.Blocks
                .Where(b => b.BlockType == BlockType.LINE)
                .Select(b => b.Text));

            _logger.LogInformation("Textract extracted {Chars} characters", rawText.Length);
            return rawText;
        }

        public async Task<CVStructuredResult> ExtractStructuredAsync(string rawText)
        {
            var systemPrompt = "You are an expert technical recruiter AI evaluating a candidate's resume. "
                + "Your task is to extract the candidate's skills and experience. "
                + "You MUST output the result strictly as a valid JSON object. "
                + "Do NOT include any conversational text or markdown formatting. Output ONLY the raw JSON. "
                + "Strict JSON Schema to follow:\n"
                + "{\n"
                + "  \"frontend_skills\": [\"array of strings\"],\n"
                + "  \"backend_skills\": [\"array of strings\"],\n"
                + "  \"devops_skills\": [\"array of strings\"],\n"
                + "  \"soft_skills\": [\"array of strings\"],\n"
                + "  \"years_experience\": 0,\n"
                + "  \"seniority_estimate\": \"Junior/Mid/Senior\"\n"
                + "}";

            var userMessage = "Please analyze this resume text:\n\n<resume>\n" + rawText + "\n</resume>";

            var responseText = await InvokeBedrockAsync(systemPrompt, userMessage);

            var result = JsonSerializer.Deserialize<CVStructuredResult>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new CVStructuredResult();

            return result;
        }

        public async Task<CVMatchResult> MatchCVWithJDAsync(CVStructuredResult cvData, Guid jobId)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found, skipping match", jobId);
                return new CVMatchResult { MatchingScore = 0, Strengths = "N/A", Gaps = "Job not found" };
            }

            var jdContent = "Title: " + job.Title + "\n\n" + job.Description;
            var allSkills = string.Join(", ",
                (cvData.FrontendSkills ?? [])
                .Concat(cvData.BackendSkills ?? [])
                .Concat(cvData.DevopsSkills ?? []));

            var systemPrompt = "You are an expert technical recruiter. Evaluate how well this candidate matches the job. "
                + "Respond ONLY with a valid JSON object (no markdown) with this exact schema:\n"
                + "{\n"
                + "  \"matchingScore\": 0,\n"
                + "  \"strengths\": \"what candidate does well for this role\",\n"
                + "  \"gaps\": \"what candidate is missing for this role\"\n"
                + "}";

            var userMessage = "CANDIDATE:\n"
                + "Seniority: " + cvData.SeniorityEstimate + "\n"
                + "Years Experience: " + cvData.YearsExperience + "\n"
                + "Skills: " + allSkills + "\n\n"
                + "JOB DESCRIPTION:\n" + jdContent;

            var responseText = await InvokeBedrockAsync(systemPrompt, userMessage);

            var result = JsonSerializer.Deserialize<CVMatchResult>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new CVMatchResult();

            _logger.LogInformation("Match score for Job {JobId}: {Score}", jobId, result.MatchingScore);
            return result;
        }

        private async Task<string> InvokeBedrockAsync(string systemPrompt, string userMessage)
        {
            var body = JsonSerializer.Serialize(new
            {
                anthropic_version = "bedrock-2023-05-31",
                max_tokens = 1000,
                temperature = 0.0,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userMessage } }
            });

            var response = await _bedrock.InvokeModelAsync(new InvokeModelRequest
            {
                ModelId = ModelId,
                ContentType = "application/json",
                Accept = "application/json",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(body))
            });

            var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "{}";
        }
    }
}