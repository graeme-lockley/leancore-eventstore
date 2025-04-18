using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;

namespace EventStore.Api.Configuration;

/// <summary>
/// Configuration for Swagger/OpenAPI
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Configures Swagger/OpenAPI generation
    /// </summary>
    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EventStore API",
                Version = "v1",
                Description = """
                    A generic event sourcing framework API that provides interfaces and implementations for event sourcing.

                    ## Health Check Endpoints

                    The API provides health check endpoints to monitor the system's health:

                    ### GET /api/v1/health
                    Returns the overall health status of the system and all its components.
                    - 200 OK: System is healthy or degraded
                    - 503 Service Unavailable: System is unhealthy

                    ### GET /api/v1/health/{componentName}
                    Returns the health status of a specific component.
                    - 200 OK: Component is healthy or degraded
                    - 404 Not Found: Component not found
                    - 503 Service Unavailable: Component is unhealthy

                    Available components:
                    - BlobStorage: Azure Blob Storage health status
                    - System: Overall system health including memory and thread pool metrics

                    Response caching is enabled for 10 seconds on all health endpoints.
                    """,
                Contact = new OpenApiContact
                {
                    Name = "EventStore Team",
                    Email = "team@eventstore.com",
                    Url = new Uri("https://github.com/yourusername/eventstore")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            // Add security definition if needed
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Organize endpoints by controller
            options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
            options.DocInclusionPredicate((docName, api) => true);

            // Add response examples
            options.ExampleFilters();
        });

        // Add response examples
        services.AddSwaggerExamplesFromAssemblyOf<Program>();
    }

    /// <summary>
    /// Configures Swagger/OpenAPI middleware
    /// </summary>
    public static void UseSwaggerConfiguration(this IApplicationBuilder app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "api-docs/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/api-docs/v1/swagger.json", "EventStore API v1");
            options.RoutePrefix = "api-docs";
            options.DocumentTitle = "EventStore API Documentation";
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
        });
    }
} 