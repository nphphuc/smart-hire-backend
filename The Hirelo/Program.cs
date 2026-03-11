using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using The_Hirelo.Data;
using The_Hirelo.Repositories;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.Storage;

namespace The_Hirelo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
            var builder = WebApplication.CreateBuilder(args);
            
            // Load AWS credentials from environment variables
            var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")
                ?? Environment.GetEnvironmentVariable("AWS__AccessKey");
            var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")
                ?? Environment.GetEnvironmentVariable("AWS__SecretKey");

            if (!string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey))
            {
                // Set AWS credentials from environment
                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", awsAccessKey);
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", awsSecretKey);
            }

            builder.Services.AddAWSService<IAmazonS3>();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter your Cognito JWT token"
                });
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? builder.Configuration.GetConnectionString("DefaultConnection");
            // tam thoi comment dong phia tren de nham muc dich push docker
            //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");               

            Console.WriteLine("=== CONNECTION STRING DEBUG ===");
            //Console.WriteLine(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "ENV NULL");
            Console.WriteLine(connectionString);
            Console.WriteLine("================================");

            builder.Services.AddDbContext<HireloDbContext>(options =>
                options.UseNpgsql(connectionString)
            );

            var awsRegion = Environment.GetEnvironmentVariable("AWS__Cognito__Region")
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? builder.Configuration["AWS:Cognito:Region"];
            var userPoolId = Environment.GetEnvironmentVariable("AWS__Cognito__UserPoolId")
                ?? Environment.GetEnvironmentVariable("AWS_USER_POOL_ID")
                ?? builder.Configuration["AWS:Cognito:UserPoolId"];
            var clientId = Environment.GetEnvironmentVariable("AWS__Cognito__ClientId")
                ?? Environment.GetEnvironmentVariable("AWS_CLIENT_ID")
                ?? builder.Configuration["AWS:Cognito:ClientId"];
            var clientSecret = Environment.GetEnvironmentVariable("AWS__Cognito__ClientSecret")
                ?? builder.Configuration["AWS:Cognito:ClientSecret"];
            var cognitoAuthority = $"https://cognito-idp.{awsRegion}.amazonaws.com/{userPoolId}";
            var s3BucketName = builder.Configuration["AWS:S3:BucketName"] ?? "hirelo-media";
            
            Console.WriteLine($"AWS_REGION = {awsRegion}");
            Console.WriteLine($"USER_POOL_ID = {userPoolId}");
            Console.WriteLine($"CLIENT_ID = {clientId}");
            Console.WriteLine($"CLIENT_SECRET = {(string.IsNullOrEmpty(clientSecret) ? "NOT SET" : "SET")}\n");
            Console.WriteLine($"COGNITO_AUTHORITY = {cognitoAuthority}");
            Console.WriteLine($"S3_BUCKET_NAME = {s3BucketName}");
            Console.WriteLine($"AWS_ACCESS_KEY_ID = {(string.IsNullOrEmpty(awsAccessKey) ? "NOT SET" : "SET")}");

            // Register AWS Cognito Identity Provider Client
            builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(sp =>
            {
                return new AmazonCognitoIdentityProviderClient(RegionEndpoint.GetBySystemName(awsRegion));
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = cognitoAuthority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = cognitoAuthority,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Map Cognito group claim to role so [Authorize(Roles = "Admin")] works
                    RoleClaimType = "cognito:groups"
                };

                // Ensure inbound claim mapping doesn't drop custom claims
                options.MapInboundClaims = true;
            });

            builder.Services.AddAuthorization();

            // Register repositories and services
            builder.Services.AddScoped<IJobRepository, JobRepository>();
            builder.Services.AddScoped<IJobService, JobService>();
            builder.Services.AddScoped<ICandidateRepository, CandidateRepository>();
            builder.Services.AddScoped<ICandidateService, CandidateService>();
            builder.Services.AddScoped<IComparisonService, ComparisonService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
            builder.Services.AddScoped<ICompanyService, CompanyService>();
            builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
            builder.Services.AddScoped<IReportRepository, ReportRepository>();

            builder.Services.AddScoped<IRecruiterVerificationRepository, RecruiterVerificationRepository>();
            builder.Services.AddScoped<IRecruiterVerificationService, RecruiterVerificationService>();

            // Auth Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IPasswordService, PasswordService>();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddScoped<IFileStorage, S3FileStorage>();
            //builder.WebHost.UseUrls("http://0.0.0.0:8080");

            var app = builder.Build();

            //using (var scope = app.Services.CreateScope())
            //{
            //    var db = scope.ServiceProvider.GetRequiredService<HireloDbContext>();
            //    db.Database.Migrate();
            //}

            // Call centralized seed class to seed admin
            try
            {
                SeedData.SeedAdminAsync(app.Services).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin seeding encountered an error: {ex.Message}");
            }


            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

                app.UseSwagger();
                app.UseSwaggerUI();
            
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
