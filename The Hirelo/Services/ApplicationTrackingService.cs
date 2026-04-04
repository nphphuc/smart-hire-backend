using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using The_Hirelo.Enums;
using The_Hirelo.Services.Interfaces;

namespace The_Hirelo.Services
{
    public class ApplicationTrackingService : IApplicationTrackingService
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private const string TableName = "ApplicationTracking";

        public ApplicationTrackingService(IAmazonDynamoDB dynamoDbClient)
        {
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task<bool> CreateApplicationTrackingAsync(Guid applicationId, Guid jobId, Guid candidateId)
        {
            try
            {
                var table = Table.LoadTable(_dynamoDbClient, TableName);

                var document = new Document
                {
                    ["applicationId"] = applicationId.ToString(),
                    ["jobId"] = jobId.ToString(),
                    ["candidateId"] = candidateId.ToString(),
                    ["status"] = ApplicationStatus.Applied.ToString(),
                    ["createdAt"] = DateTime.UtcNow.ToString("o"),
                    ["updatedAt"] = DateTime.UtcNow.ToString("o")
                };

                await table.PutItemAsync(document);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating application tracking in DynamoDB: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateApplicationTrackingAsync(Guid applicationId, ApplicationStatus status)
        {
            try
            {
                var table = Table.LoadTable(_dynamoDbClient, TableName);

                var document = new Document
                {
                    ["applicationId"] = applicationId.ToString(),
                    ["status"] = status.ToString(),
                    ["updatedAt"] = DateTime.UtcNow.ToString("o")
                };

                await table.UpdateItemAsync(document);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating application tracking in DynamoDB: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteApplicationTrackingAsync(Guid applicationId)
        {
            try
            {
                var table = Table.LoadTable(_dynamoDbClient, TableName);
                await table.DeleteItemAsync(applicationId.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting application tracking in DynamoDB: {ex.Message}");
                return false;
            }
        }
    }
}
