FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["EventStore.Api/EventStore.Api.csproj", "EventStore.Api/"]
COPY ["EventStore.Application/EventStore.Application.csproj", "EventStore.Application/"]
COPY ["EventStore.Domain/EventStore.Domain.csproj", "EventStore.Domain/"]
COPY ["EventStore.Infrastructure/EventStore.Infrastructure.csproj", "EventStore.Infrastructure/"]
RUN dotnet restore "EventStore.Api/EventStore.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "EventStore.Api/EventStore.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "EventStore.Api/EventStore.Api.csproj" -c Release -o /app/publish

# Create the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EventStore.Api.dll"] 