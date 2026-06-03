# Stage 1 — build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (preserving directory structure)
COPY Helix.Orchestration.Contracts/Helix.Orchestration.Contracts.csproj Helix.Orchestration.Contracts/
COPY Helix.Orchestration.Core/Helix.Orchestration.Core.csproj Helix.Orchestration.Core/
COPY Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj Helix.Orchestration.Infrastructure/
COPY Helix.Orchestration.Simulation/Helix.Orchestration.Simulation.csproj Helix.Orchestration.Simulation/
COPY Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj Helix.Orchestration.SiteServer/

RUN dotnet restore Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj

# Copy all source
COPY . .

RUN dotnet publish Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2 — runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5090

EXPOSE 5090

ENTRYPOINT ["dotnet", "Helix.Orchestration.SiteServer.dll"]
