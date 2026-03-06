using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Amazon;
using Amazon.CognitoIdentityProvider;
using The_Hirelo.Data;
using The_Hirelo.Repositories;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services;
using The_Hirelo.Services.Interfaces;
using DotNetEnv;

namespace The_Hirelo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
            var builder = WebApplication.CreateBuilder(args);
            
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

            Console.WriteLine("=== CONNECTION STRING DEBUG ===");            
            Console.WriteLine(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "ENV NULL");
            Console.WriteLine("================================");

            builder.Services.AddDbContext<HireloDbContext>(options =>
                options.UseNpgsql(connectionString)
            );

            var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION")
                ?? builder.Configuration["AWS:Cognito:Region"];
            var userPoolId = Environment.GetEnvironmentVariable("AWS_USER_POOL_ID")
                ?? builder.Configuration["AWS:Cognito:UserPoolId"];
            var clientId = Environment.GetEnvironmentVariable("AWS_CLIENT_ID")
                ?? builder.Configuration["AWS:Cognito:ClientId"];
            var cognitoAuthority = $"https://cognito-idp.{awsRegion}.amazonaws.com/{userPoolId}";

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
                    ValidateIssuerSigningKey = true
                };
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
