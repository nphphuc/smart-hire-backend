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

        private const string ModelId = "anthropic.claude-3-sonnet-20240229-v1:0";

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

        public async Task<string> ExtractRawTextAsync(byte[] fileBytes)
        {
            var response = await _textract.DetectDocumentTextAsync(new DetectDocumentTextRequest
            {
                Document = new Document { Bytes = new MemoryStream(fileBytes) }
            });

            var sb = new StringBuilder();
            foreach (var block in response.Blocks.Where(b => b.BlockType == BlockType.LINE))
                sb.AppendLine(block.Text);

            var rawText = sb.ToString();
            _logger.LogInformation("Textract extracted {Chars} characters", rawText.Length);
            return rawText;
        }

        public async Task<CVStructuredResult> ExtractStructuredAsync(string rawText)
        {
            var prompt = "You are an expert HR assistant. Analyze the following CV text and extract structured information.\n\n"
                + "CV TEXT:\n" + rawText + "\n\n"
                + "Respond ONLY with a valid JSON object (no markdown) with this exact schema:\n"
                + "{\n"
                + "  \"seniority\": \"Junior|Mid|Senior|Lead|Principal\",\n"
                + "  \"skills\": [\"skill1\", \"skill2\"],\n"
                + "  \"summary\": \"brief professional summary\"\n"
                + "}";

            var responseText = await InvokeBedrockAsync(prompt);

            var result = JsonSerializer.Deserialize<CVStructuredResult>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new CVStructuredResult();

            _logger.LogInformation("Structured: seniority={Seniority}, skills={Count}",
                result.Seniority, result.Skills.Count);

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
            var cvSkills = string.Join(", ", cvData.Skills);

            var prompt = "You are an expert technical recruiter. Evaluate how well this candidate matches the job.\n\n"
                + "CANDIDATE:\n"
                + "Seniority: " + cvData.Seniority + "\n"
                + "Skills: " + cvSkills + "\n"
                + "Summary: " + cvData.Summary + "\n\n"
                + "JOB DESCRIPTION:\n" + jdContent + "\n\n"
                + "Respond ONLY with a valid JSON object (no markdown) with this exact schema:\n"
                + "{\n"
                + "  \"matchingScore\": 0,\n"
                + "  \"strengths\": \"what candidate does well for this role\",\n"
                + "  \"gaps\": \"what candidate is missing for this role\"\n"
                + "}";

            var responseText = await InvokeBedrockAsync(prompt);

            var result = JsonSerializer.Deserialize<CVMatchResult>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new CVMatchResult();

            _logger.LogInformation("Match score for Job {JobId}: {Score}", jobId, result.MatchingScore);

            return result;
        }

        private async Task<string> InvokeBedrockAsync(string prompt)
        {
            var body = JsonSerializer.Serialize(new
            {
                anthropic_version = "bedrock-2023-05-31",
                max_tokens = 1024,
                messages = new[] { new { role = "user", content = prompt } }
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

            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "{}";
        }
    }
}