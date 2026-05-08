using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using VetCare.Application.Abstractions.Auditing;
using VetCare.Application.Abstractions.Identity;
using VetCare.Application.Abstractions.Messaging;
using VetCare.Application.Abstractions.MultiTenancy;
using VetCare.Application.Abstractions.Persistence;
using VetCare.Application.Abstractions.Storage;
using VetCare.Domain.Appointments;
using VetCare.Domain.Owners;
using VetCare.Domain.Pets;
using VetCare.Domain.Vaccinations;
using VetCare.Infrastructure.Auditing;
using VetCare.Infrastructure.Identity;
using VetCare.Infrastructure.Messaging;
using VetCare.Infrastructure.MultiTenancy;
using VetCare.Infrastructure.Persistence;
using VetCare.Infrastructure.Persistence.Repositories;
using VetCare.Infrastructure.Storage;

namespace VetCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<VetCareDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VetCareDbContext.Schema)));

        services.AddScoped<IVetCareDbContext>(sp => sp.GetRequiredService<VetCareDbContext>());

        services.AddScoped<IRepository<Owner>, OwnerRepository>();
        services.AddScoped<IRepository<Pet>, PetRepository>();
        services.AddScoped<IRepository<Appointment>, AppointmentRepository>();
        services.AddScoped<IRepository<Vaccination>, VaccinationRepository>();

        services.AddScoped<ITenantProvider, CurrentTenantProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "Jwt:Secret is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .Validate(o => o.ExpiryMinutes > 0, "Jwt:ExpiryMinutes must be > 0.")
            .ValidateOnStart();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddOptions<AwsOptions>()
            .Bind(configuration.GetSection(AwsOptions.SectionName));

        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var aws = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
            var sqsConfig = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(aws.Region),
            };

            if (!string.IsNullOrWhiteSpace(aws.ServiceUrl))
            {
                sqsConfig.ServiceURL = aws.ServiceUrl;
                sqsConfig.AuthenticationRegion = aws.Region;
            }

            if (!string.IsNullOrWhiteSpace(aws.AccessKey) && !string.IsNullOrWhiteSpace(aws.SecretKey))
            {
                return new AmazonSQSClient(new BasicAWSCredentials(aws.AccessKey, aws.SecretKey), sqsConfig);
            }

            return new AmazonSQSClient(sqsConfig);
        });

        services.AddSingleton<ISqsPublisher, SqsPublisher>();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var aws = sp.GetRequiredService<IOptions<AwsOptions>>().Value;
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(aws.Region),
                ForcePathStyle = !string.IsNullOrWhiteSpace(aws.ServiceUrl),
            };

            if (!string.IsNullOrWhiteSpace(aws.ServiceUrl))
            {
                s3Config.ServiceURL = aws.ServiceUrl;
                s3Config.AuthenticationRegion = aws.Region;
            }

            if (!string.IsNullOrWhiteSpace(aws.AccessKey) && !string.IsNullOrWhiteSpace(aws.SecretKey))
            {
                return new AmazonS3Client(new BasicAWSCredentials(aws.AccessKey, aws.SecretKey), s3Config);
            }

            return new AmazonS3Client(s3Config);
        });

        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName));

        services.AddSingleton<IStorageService, S3StorageService>();

        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(options.DatabaseName);
        });

        services.AddSingleton<IAuditRepository, MongoAuditRepository>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
