using Azure.Storage.Blobs;
using EventStore.Domain.Health;
using EventStore.Infrastructure.Health;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCaching();

// Configure Azure Storage
var connectionString = builder.Configuration["AzureStorage:ConnectionString"];
builder.Services.AddSingleton(new BlobServiceClient(connectionString));

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCaching();
app.UseAuthorization();
app.MapControllers();

// Register health checks with the service
var healthCheckService = app.Services.GetRequiredService<IHealthCheckService>();
var serviceScope = app.Services.CreateScope();
var systemHealthCheck = serviceScope.ServiceProvider.GetRequiredService<SystemHealthCheck>();
var blobStorageHealthCheck = serviceScope.ServiceProvider.GetRequiredService<BlobStorageHealthCheck>();
healthCheckService.RegisterHealthCheck(systemHealthCheck);
healthCheckService.RegisterHealthCheck(blobStorageHealthCheck);

app.Run(); 