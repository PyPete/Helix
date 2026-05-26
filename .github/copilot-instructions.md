# Copilot Instructions

## Project Guidelines
- Repository branch flow includes dev, stage, and main; CI workflows should account for all three branches where appropriate.

## Device Management
- Use device instance identity based on unique COM port mapping plus USB VID:PID (and related hardware identifiers) for distinguishing physically identical devices in Helix orchestration.