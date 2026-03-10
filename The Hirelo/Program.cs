using Amazon;
using Amazon.BedrockRuntime;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Amazon.SimpleEmail;
using Amazon.SQS;
using Amazon.Textract;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using The_Hirelo.Common;
using The_Hirelo.Data;
using The_Hirelo.Repositories;
using The_Hirelo.Services;
using The_Hirelo.Workers;

namespace The_Hirelo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Database ──────────────────────────────────────────────────────
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<HireloDbContext>(options =>
                options.UseNpgsql(connectionString));

            // ── AWS config ────────────────────────────────────────────────────
            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION")
                ?? builder.Configuration["AWS:Cognito:Region"]
                ?? "ap-southeast-1";

            var userPoolId = Environment.GetEnvironmentVariable("AWS_USER_POOL_ID")
                ?? builder.Configuration["AWS:Cognito:UserPoolId"];

            var region = RegionEndpoint.GetBySystemName(awsRegion);

            // ── AWS SDK clients ───────────────────────────────────────────────
            builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(
                _ => new AmazonCognitoIdentityProviderClient(region));

            builder.Services.AddSingleton<IAmazonS3>(
                _ => new AmazonS3Client(region));

            builder.Services.AddSingleton<IAmazonSQS>(
                _ => new AmazonSQSClient(region));

            builder.Services.AddSingleton<IAmazonTextract>(
                _ => new AmazonTextractClient(region));

            builder.Services.AddSingleton<IAmazonBedrockRuntime>(
                _ => new AmazonBedrockRuntimeClient(region));

            builder.Services.AddSingleton<IAmazonSimpleEmailService>(
                _ => new AmazonSimpleEmailServiceClient(region));

            // ── Auth (Cognito JWT) ─────────────────────────────────────────────
            var cognitoAuthority = $"https://cognito-idp.{awsRegion}.amazonaws.com/{userPoolId}";

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
                    ValidateIssuerSigningKey = true
                };
            });

            builder.Services.AddAuthorization();

            // ── Repositories ───────────────────────────────────────────────────
            builder.Services.AddScoped<ICandidateProfileRepository, CandidateProfileRepository>();

            // ── Services ───────────────────────────────────────────────────────
            builder.Services.AddScoped<ICVService, CVService>();
            builder.Services.AddScoped<ICVParseService, CVParseService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();

            // ── WebSocket Manager ──────────────────────────────────────────────
            builder.Services.AddSingleton<IWebSocketManager, The_Hirelo.Common.WebSocketManager>();

            // ── Background Workers ─────────────────────────────────────────────
            builder.Services.AddHostedService<NotificationWorker>();

            // ── Controllers & Swagger ──────────────────────────────────────────
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "The Hirelo API", Version = "v1" });
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

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            // WebSocket phải đặt trước UseAuthentication
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}