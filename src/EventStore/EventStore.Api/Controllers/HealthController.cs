using EventStore.Application.Health.Responses;
using EventStore.Domain.Health;
using EventStore.Api.Examples;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using System.Diagnostics;

namespace EventStore.Api.Controllers;

/// <summary>
/// Controller for health check endpoints
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the HealthController
    /// </summary>
    /// <param name="healthCheckService">The health check service</param>
    /// <param name="logger">The logger</param>
    public HealthController(
        IHealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the health status of all components
    /// </summary>
    /// <remarks>
    /// This endpoint returns the health status of all registered components in the system.
    /// The overall status will be:
    /// - Healthy: All components are healthy
    /// - Degraded: At least one component is degraded and no components are unhealthy
    /// - Unhealthy: At least one component is unhealthy
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of all components</returns>
    /// <response code="200">System is healthy or degraded</response>
    /// <response code="503">System is unhealthy</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable)]
    [ResponseCache(Duration = 10, VaryByQueryKeys = ["*"])]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(HealthyResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status503ServiceUnavailable, typeof(UnhealthyResponseExample))]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var requestId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["Operation"] = "GetHealth"
        });

        try
        {
            _logger.LogDebug("Processing health check request. RequestId: {RequestId}", requestId);

            var results = await _healthCheckService.CheckAllAsync(cancellationToken);
            var response = CreateResponse(results);

            _logger.LogInformation(
                "Health check completed. Status: {Status}, Components: {ComponentCount}, RequestId: {RequestId}",
                response.Status,
                response.Components.Count,
                requestId);

            return response.Status == "Unhealthy"
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, response)
                : Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing health check request. RequestId: {RequestId}", requestId);

            var errorResponse = new HealthCheckResponse(
                "Unhealthy",
                DateTimeOffset.UtcNow,
                [
                    new ComponentHealthResponse(
                        "System",
                        "Unhealthy",
                        null,
                        "Error processing health check request")
                ]);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }

    /// <summary>
    /// Gets the health status of a specific component
    /// </summary>
    /// <remarks>
    /// This endpoint returns the health status of a specific component.
    /// The status will be one of:
    /// - Healthy: The component is functioning normally
    /// - Degraded: The component is experiencing issues but is still operational
    /// - Unhealthy: The component is not functioning correctly
    /// 
    /// Available components:
    /// - BlobStorage: Azure Blob Storage health status
    /// - System: Overall system health including memory and thread pool metrics
    /// </remarks>
    /// <param name="componentName">Name of the component to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of the specified component</returns>
    /// <response code="200">Component is healthy or degraded</response>
    /// <response code="404">Component not found</response>
    /// <response code="503">Component is unhealthy</response>
    [HttpGet("{componentName}")]
    [ProducesResponseType(typeof(ComponentHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ComponentHealthResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ComponentHealthResponse), StatusCodes.Status503ServiceUnavailable)]
    [ResponseCache(Duration = 10, VaryByQueryKeys = ["*"])]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(HealthyComponentResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status503ServiceUnavailable, typeof(UnhealthyComponentResponseExample))]
    public async Task<IActionResult> GetComponentHealth(string componentName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(componentName))
        {
            _logger.LogWarning("Component name is null or empty");
            return BadRequest(new ComponentHealthResponse(
                "Unknown",
                "Unhealthy",
                null,
                "Component name cannot be null or empty"));
        }

        try
        {
            _logger.LogDebug("Processing health check request for component: {ComponentName}", componentName);

            var result = await _healthCheckService.CheckComponentAsync(componentName, cancellationToken);

            // Handle component not found
            if (result.Status == HealthStatus.Unhealthy &&
                result.Description.Contains("No health check registered"))
            {
                return NotFound(new ComponentHealthResponse(
                    componentName,
                    "Unhealthy",
                    null,
                    $"Component '{componentName}' not found"));
            }

            var response = CreateComponentResponse(result);

            _logger.LogInformation(
                "Component health check completed. Component: {ComponentName}, Status: {Status}",
                componentName,
                response.Status);

            if (response.Status == "Unhealthy")
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);

            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check cancelled for component: {ComponentName}", componentName);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ComponentHealthResponse(
                componentName,
                "Unhealthy",
                null,
                "Health check operation cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for component: {ComponentName}", componentName);

            var errorResponse = new ComponentHealthResponse(
                componentName,
                "Unhealthy",
                null,
                $"Error checking component health: {ex.Message}");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse);
        }
    }

    private static HealthCheckResponse CreateResponse(IReadOnlyCollection<HealthCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var status = DetermineOverallStatus(results);
        var components = results.Select(CreateComponentResponse).ToList();

        // Validate response format
        ValidateResponse(status, components);

        return new HealthCheckResponse(
            status,
            DateTimeOffset.UtcNow,
            components);
    }

    private static ComponentHealthResponse CreateComponentResponse(HealthCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(result.ComponentName))
            throw new ArgumentException("Component name cannot be null or empty", nameof(result));

        // Ensure status is a valid value
        if (!Enum.IsDefined(typeof(HealthStatus), result.Status))
            throw new ArgumentException($"Invalid health status: {result.Status}", nameof(result));

        return new ComponentHealthResponse(
            result.ComponentName,
            result.Status.ToString(),
            result.Status != HealthStatus.Unhealthy ? result.Description : null,
            result.Status == HealthStatus.Unhealthy ? result.Description : null,
            result.Data);
    }

    private static void ValidateResponse(string status, IReadOnlyCollection<ComponentHealthResponse> components)
    {
        if (!Enum.TryParse<HealthStatus>(status, out var healthStatus))
            throw new InvalidOperationException($"Invalid health status: {status}");

        // Validate overall status matches component statuses
        var hasUnhealthy = components.Any(c => c.Status == nameof(HealthStatus.Unhealthy));
        var hasDegraded = components.Any(c => c.Status == nameof(HealthStatus.Degraded));

        if (healthStatus == HealthStatus.Unhealthy && !hasUnhealthy)
            throw new InvalidOperationException("Overall status is Unhealthy but no components are unhealthy");

        if (healthStatus == HealthStatus.Degraded && !hasDegraded && !hasUnhealthy)
            throw new InvalidOperationException("Overall status is Degraded but no components are degraded");

        if (healthStatus == HealthStatus.Healthy && (hasDegraded || hasUnhealthy))
            throw new InvalidOperationException("Overall status is Healthy but components are degraded or unhealthy");

        // Validate component response format
        foreach (var component in components)
        {
            if (string.IsNullOrWhiteSpace(component.Name))
                throw new InvalidOperationException("Component name cannot be null or empty");

            if (!Enum.TryParse<HealthStatus>(component.Status, out _))
                throw new InvalidOperationException($"Invalid component status: {component.Status}");

            if (component.Status == nameof(HealthStatus.Unhealthy) && string.IsNullOrWhiteSpace(component.Error))
                throw new InvalidOperationException("Unhealthy component must have an error message");

            if (component.Status != nameof(HealthStatus.Unhealthy) && !string.IsNullOrWhiteSpace(component.Error))
                throw new InvalidOperationException("Non-unhealthy component should not have an error message");
        }
    }

    private static string DetermineOverallStatus(IReadOnlyCollection<HealthCheckResult> results)
    {
        if (results.Any(r => r.Status == HealthStatus.Unhealthy))
            return nameof(HealthStatus.Unhealthy);

        if (results.Any(r => r.Status == HealthStatus.Degraded))
            return nameof(HealthStatus.Degraded);

        return nameof(HealthStatus.Healthy);
    }
}