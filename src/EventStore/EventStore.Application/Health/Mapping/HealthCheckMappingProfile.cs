using AutoMapper;
using EventStore.Application.Health.Responses;
using EventStore.Domain.Health;

namespace EventStore.Application.Health.Mapping;

/// <summary>
/// AutoMapper profile for health check response mappings
/// </summary>
public class HealthCheckMappingProfile : Profile
{
    public HealthCheckMappingProfile()
    {
        CreateMap<HealthCheckResult, ComponentHealthResponse>()
            .ConstructUsing(src => new ComponentHealthResponse(
                src.ComponentName,
                src.Status.ToString(),
                src.Status != HealthStatus.Unhealthy ? src.Description : null,
                src.Status == HealthStatus.Unhealthy ? src.Description : null,
                src.Data));

        CreateMap<IReadOnlyCollection<HealthCheckResult>, SystemHealthResponse>()
            .ConstructUsing((src, context) => new SystemHealthResponse(
                DetermineOverallStatus(src),
                src.Select(x => context.Mapper.Map<ComponentHealthResponse>(x)).ToList()));
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