using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using The_Hirelo.Data;
using The_Hirelo.Repositories;
using The_Hirelo.Repositories.Interfaces;
using The_Hirelo.Services;
using The_Hirelo.Services.Interfaces;
using The_Hirelo.Storage;

namespace The_Hirelo;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Load AWS credentials from environment variables
        var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")
            ?? Environment.GetEnvironmentVariable("AWS__AccessKey");
        var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")
            ?? Environment.GetEnvironmentVariable("AWS__SecretKey");

        if (!string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey))
        {
            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", awsAccessKey);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", awsSecretKey);
        }

        services.AddAWSService<IAmazonS3>();

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
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

        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        string finalConnectionString;
        if (!string.IsNullOrEmpty(dbHost) && !string.IsNullOrEmpty(dbUser))
        {
            finalConnectionString =
                $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;";
        }
        else
        {
            finalConnectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        services.AddDbContext<HireloDbContext>(options =>
            options.UseNpgsql(finalConnectionString)
        );

        var awsRegion = Environment.GetEnvironmentVariable("AWS__Cognito__Region")
            ?? Environment.GetEnvironmentVariable("AWS_REGION")
            ?? _configuration["AWS:Cognito:Region"]
            ?? "ap-southeast-1";
        var userPoolId = Environment.GetEnvironmentVariable("AWS__Cognito__UserPoolId")
            ?? Environment.GetEnvironmentVariable("AWS_USER_POOL_ID")
            ?? _configuration["AWS:Cognito:UserPoolId"];
        var clientId = Environment.GetEnvironmentVariable("AWS__Cognito__ClientId")
            ?? Environment.GetEnvironmentVariable("AWS_CLIENT_ID")
            ?? _configuration["AWS:Cognito:ClientId"];
        var clientSecret = Environment.GetEnvironmentVariable("AWS__Cognito__ClientSecret")
            ?? _configuration["AWS:Cognito:ClientSecret"];
        var cognitoAuthority = $"https://cognito-idp.{awsRegion}.amazonaws.com/{userPoolId}";
        var validateAudience = !string.IsNullOrEmpty(clientId);

        services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
        {
            var endpointRegion = awsRegion switch
            {
                "us-east-1" => Amazon.RegionEndpoint.USEast1,
                "us-west-2" => Amazon.RegionEndpoint.USWest2,
                "eu-west-1" => Amazon.RegionEndpoint.EUWest1,
                "ap-southeast-1" => Amazon.RegionEndpoint.APSoutheast1,
                "ap-southeast-2" => Amazon.RegionEndpoint.APSoutheast2,
                _ => Amazon.RegionEndpoint.APSoutheast1
            };
            return new AmazonCognitoIdentityProviderClient(endpointRegion);
        });

        services.AddAuthentication(options =>
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
                ValidateAudience = validateAudience,
                ValidAudience = validateAudience ? clientId : null,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RoleClaimType = "cognito:groups"
            };

            options.MapInboundClaims = true;
        });

        services.AddAuthorization();

        var allowedCorsOrigin = Environment.GetEnvironmentVariable("ALLOWED_CORS_ORIGIN");
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCors", policy =>
            {
                if (!string.IsNullOrEmpty(allowedCorsOrigin))
                {
                    policy.WithOrigins(allowedCorsOrigin)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        // Register repositories and services
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<ICandidateRepository, CandidateRepository>();
        services.AddScoped<ICandidateService, CandidateService>();
        services.AddScoped<IComparisonService, ComparisonService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IInterviewRepository, InterviewRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();

        services.AddScoped<IRecruiterVerificationRepository, RecruiterVerificationRepository>();
        services.AddScoped<IRecruiterVerificationService, RecruiterVerificationService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IFileStorage, S3FileStorage>();
    }

    public void Configure(IApplicationBuilder app)
    {
        var enableSwagger = _env.IsDevelopment() ||
                            string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase);
        if (enableSwagger)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("DefaultCors");

        app.UseAuthentication();
        app.UseMiddleware<The_Hirelo.Middleware.EnsureUserExistsMiddleware>();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        var shouldSeedAdmin = _env.IsDevelopment() ||
                              string.Equals(Environment.GetEnvironmentVariable("SEED_ADMIN"), "true", StringComparison.OrdinalIgnoreCase);
        if (shouldSeedAdmin)
        {
            try
            {
                using var scope = app.ApplicationServices.CreateScope();
                SeedData.SeedAdminAsync(scope.ServiceProvider).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin seeding encountered an error: {ex.Message}");
            }
        }
    }
}
