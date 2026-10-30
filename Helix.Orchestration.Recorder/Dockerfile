# Stage 1 — build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (preserving directory structure)
COPY Helix.Orchestration.Infrastructure/Helix.Orchestration.Infrastructure.csproj Helix.Orchestration.Infrastructure/
COPY Helix.Orchestration.Recorder/Helix.Orchestration.Recorder.csproj Helix.Orchestration.Recorder/

RUN dotnet restore Helix.Orchestration.Recorder/Helix.Orchestration.Recorder.csproj

# Copy all source
COPY . .

RUN dotnet publish Helix.Orchestration.Recorder/Helix.Orchestration.Recorder.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2 — runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Helix.Orchestration.Recorder.dll"]
