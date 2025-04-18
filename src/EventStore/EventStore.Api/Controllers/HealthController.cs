using EventStore.Application.Health.Responses;
using EventStore.Domain.Health;
using EventStore.Api.Examples;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

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
    [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(HealthyResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status503ServiceUnavailable, typeof(UnhealthyResponseExample))]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing health check request");
            
            var results = await _healthCheckService.CheckAllAsync(cancellationToken);
            var response = CreateResponse(results);
            
            _logger.LogInformation(
                "Health check completed. Status: {Status}, Components: {ComponentCount}",
                response.Status,
                response.Components.Count);

            return response.Status == "Unhealthy" 
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, response)
                : Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing health check request");
            
            var errorResponse = new HealthCheckResponse(
                "Unhealthy",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new ComponentHealthResponse(
                        "System",
                        "Unhealthy",
                        null,
                        "Error processing health check request")
                });

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
    [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetComponentHealth(string componentName, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing health check request for component: {ComponentName}", componentName);
            
            var result = await _healthCheckService.CheckComponentAsync(componentName, cancellationToken);
            var response = CreateComponentResponse(result);

            _logger.LogInformation(
                "Component health check completed. Component: {ComponentName}, Status: {Status}",
                componentName,
                response.Status);

            if (response.Status == "Unhealthy")
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);

            return Ok(response);
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
        var status = DetermineOverallStatus(results);
        var components = results.Select(CreateComponentResponse).ToList();

        return new HealthCheckResponse(
            status.ToString(),
            DateTimeOffset.UtcNow,
            components);
    }

    private static ComponentHealthResponse CreateComponentResponse(HealthCheckResult result)
    {
        return new ComponentHealthResponse(
            result.ComponentName,
            result.Status.ToString(),
            result.Status != HealthStatus.Unhealthy ? result.Description : null,
            result.Status == HealthStatus.Unhealthy ? result.Description : null);
    }

    private static string DetermineOverallStatus(IReadOnlyCollection<HealthCheckResult> results)
    {
        if (results.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy.ToString();

        if (results.Any(r => r.Status == HealthStatus.Degraded))
            return HealthStatus.Degraded.ToString();

        return HealthStatus.Healthy.ToString();
    }
} 