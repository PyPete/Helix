# Stage 1 — build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (preserving directory structure)
COPY Helix.Orchestration.Contracts/Helix.Orchestration.Contracts.csproj Helix.Orchestration.Contracts/
COPY Helix.Orchestration.Core/Helix.Orchestration.Core.csproj Helix.Orchestration.Core/
COPY Helix.Orchestration.Devices/Helix.Orchestration.Devices.csproj Helix.Orchestration.Devices/
COPY Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj Helix.Orchestration.Infrastructure/
COPY Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj Helix.Orchestration.Agent/

RUN dotnet restore Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj

# Copy all source
COPY . .

RUN dotnet publish Helix.Orchestration.Agent/Helix.Orchestration.Agent.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2 — runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5080

ENTRYPOINT ["dotnet", "Helix.Orchestration.Agent.dll"]
