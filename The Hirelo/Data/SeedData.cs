using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using The_Hirelo.Models;

namespace The_Hirelo.Data
{
    public static class SeedData
    {
        /// <summary>
        /// Seed admin user into Cognito (if configured) and into the database.
        /// Call this from a startup script or manually when needed.
        /// </summary>
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var db = provider.GetRequiredService<HireloDbContext>();
            var configuration = provider.GetRequiredService<IConfiguration>();

            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

            // If admin already exists in DB, skip
            var existing = db.Users.FirstOrDefault(u => u.Email == adminEmail);
            if (existing != null)
            {
                Console.WriteLine("SeedData: Admin already exists in database. Skipping.");
                return;
            }

            string? cognitoSub = null;

            try
            {
                var poolId = configuration["AWS:Cognito:UserPoolId"]
                             ?? Environment.GetEnvironmentVariable("AWS__Cognito__UserPoolId");

                if (!string.IsNullOrWhiteSpace(poolId))
                {
                    try
                    {
                        var cognitoClient = provider.GetRequiredService<IAmazonCognitoIdentityProvider>();

                        var listReq = new ListUsersRequest
                        {
                            UserPoolId = poolId,
                            Filter = $"email = \"{adminEmail}\"",
                            Limit = 1
                        };

                        var listResp = await cognitoClient.ListUsersAsync(listReq);
                        var found = listResp.Users?.FirstOrDefault();
                        if (found != null)
                        {
                            cognitoSub = found.Attributes?.FirstOrDefault(a => a.Name == "sub")?.Value;
                        }

                        if (cognitoSub == null)
                        {
                            var createReq = new AdminCreateUserRequest
                            {
                                UserPoolId = poolId,
                                Username = adminEmail,
                                MessageAction = MessageActionType.SUPPRESS,
                                UserAttributes = new System.Collections.Generic.List<AttributeType>
                                {
                                    new AttributeType { Name = "email", Value = adminEmail },
                                    new AttributeType { Name = "email_verified", Value = "true" }
                                }
                            };

                            var createResp = await cognitoClient.AdminCreateUserAsync(createReq);
                            cognitoSub = createResp.User?.Attributes?.FirstOrDefault(a => a.Name == "sub")?.Value ?? createResp.User.Username;
                        }

                        if (!string.IsNullOrWhiteSpace(cognitoSub))
                        {
                            try
                            {
                                var setPwdReq = new AdminSetUserPasswordRequest
                                {
                                    UserPoolId = poolId,
                                    Username = adminEmail,
                                    Password = adminPassword,
                                    Permanent = true
                                };
                                await cognitoClient.AdminSetUserPasswordAsync(setPwdReq);
                            }
                            catch (Exception exPwd)
                            {
                                Console.WriteLine($"SeedData: AdminSetUserPassword failed: {exPwd.Message}");
                            }
                        }
                    }
                    catch (Exception exC)
                    {
                        Console.WriteLine($"SeedData: Cognito seeding skipped/failed: {exC.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("SeedData: Cognito UserPoolId not configured. Only seeding DB.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SeedData: Unexpected error during Cognito seed: {ex.Message}");
            }

            // Seed into database
            var user = new User
            {
                Id = Guid.NewGuid(),
                CognitoSub = cognitoSub ?? "seed-admin",
                Email = adminEmail,
                Role = The_Hirelo.Enums.UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
            Console.WriteLine("SeedData: Admin user seeded to database.");
        }
    }
}
