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
                src.Description,
                src.Timestamp,
                src.Data));

        CreateMap<IReadOnlyCollection<HealthCheckResult>, HealthCheckResponse>()
            .ConstructUsing((src, context) => new HealthCheckResponse(
                DetermineOverallStatus(src),
                src.Max(x => x.Timestamp),
                src.Select(x => context.Mapper.Map<ComponentHealthResponse>(x)).ToList()));
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