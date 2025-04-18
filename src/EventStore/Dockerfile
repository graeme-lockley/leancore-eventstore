FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EventStore.Api/EventStore.Api.csproj", "EventStore.Api/"]
COPY ["EventStore.Application/EventStore.Application.csproj", "EventStore.Application/"]
COPY ["EventStore.Domain/EventStore.Domain.csproj", "EventStore.Domain/"]
COPY ["EventStore.Infrastructure/EventStore.Infrastructure.csproj", "EventStore.Infrastructure/"]
RUN dotnet restore "EventStore.Api/EventStore.Api.csproj"
COPY . .
RUN dotnet build "EventStore.Api/EventStore.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventStore.Api/EventStore.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
EXPOSE 443

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/api/v1/health || exit 1

ENTRYPOINT ["dotnet", "EventStore.Api.dll"] 