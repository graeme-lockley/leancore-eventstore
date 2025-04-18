using Azure.Storage.Blobs;
using EventStore.Api.Configuration;
using EventStore.Domain.Health;
using EventStore.Infrastructure.Health;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Filters;
using System.Threading.RateLimiting;

namespace EventStore.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.ConfigureSwagger();
        builder.Services.AddResponseCaching();

        // Add rate limiting
        builder.Services.AddRateLimiter(options =>
        {
            // Global rate limit
            options.AddFixedWindowLimiter("GlobalRateLimit", options =>
            {
                options.PermitLimit = 100;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 2;
            });

            // Health check specific rate limit
            options.AddFixedWindowLimiter("HealthCheck", options =>
            {
                options.PermitLimit = 30;
                options.Window = TimeSpan.FromSeconds(10);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 2;
            });
        });

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("HealthCheckPolicy", policy =>
            {
                policy.WithOrigins("*")
                    .WithMethods("GET")
                    .WithHeaders("Content-Type");
            });
        });

        // Configure Azure Storage
        var connectionString = builder.Configuration["AzureStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
        builder.Services.AddSingleton(new BlobServiceClient(connectionString));
        builder.Services.AddSingleton<IBlobServiceClient>(sp => 
            new BlobServiceClientWrapper(sp.GetRequiredService<BlobServiceClient>()));

        // Configure health checks
        builder.Services.AddHealthCheckService(options =>
        {
            options.EnableCaching = true;
            options.CacheDuration = TimeSpan.FromSeconds(30);
            options.EnableParallelExecution = true;
            options.HealthCheckTimeout = TimeSpan.FromSeconds(5);
        });

        // Add System Health Check
        builder.Services.AddHealthCheck<SystemHealthCheck>();
        builder.Services.Configure<SystemHealthCheckOptions>(options =>
        {
            options.MemoryThresholds = new MemoryThresholds
            {
                DegradedBytes = 512L * 1024L * 1024L,    // 512 MB
                UnhealthyBytes = 1024L * 1024L * 1024L   // 1 GB
            };
            options.ThreadPoolThresholds = new ThreadPoolThresholds
            {
                DegradedUtilization = 0.7,   // 70%
                UnhealthyUtilization = 0.85  // 85%
            };
        });

        // Add Blob Storage Health Check
        builder.Services.AddHealthCheck<BlobStorageHealthCheck>();
        builder.Services.Configure<BlobStorageHealthCheckOptions>(options =>
        {
            options.TimeoutMs = 5000;        // 5 seconds
            options.IncludeDetailedInfo = true;
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerConfiguration();
        }

        app.UseResponseCaching();
        app.UseRateLimiter();
        app.UseCors("HealthCheckPolicy");
        app.UseAuthorization();
        app.MapControllers()
            .RequireRateLimiting("GlobalRateLimit");

        // Register health checks with the service
        var healthCheckService = app.Services.GetRequiredService<IHealthCheckService>();
        var serviceScope = app.Services.CreateScope();
        var systemHealthCheck = serviceScope.ServiceProvider.GetRequiredService<SystemHealthCheck>();
        var blobStorageHealthCheck = serviceScope.ServiceProvider.GetRequiredService<BlobStorageHealthCheck>();
        healthCheckService.RegisterHealthCheck(systemHealthCheck);
        healthCheckService.RegisterHealthCheck(blobStorageHealthCheck);

        app.Run();
    }
} 