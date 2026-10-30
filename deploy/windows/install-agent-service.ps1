# Helix Agent service installation is intentionally unavailable in the Phase 01
# same-OS-user profile.  A LocalSystem service cannot prove that it is the
# scientist who completed desktop enrollment, so it is unsafe to install one.
[CmdletBinding()]
param(
    [string]$InstallPath = "C:\Helix\Agent"
)

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
Write-Error @"
HelixAgent is not installed as a Windows service for the same-user profile.
Launch the published Agent interactively as $currentUser from $InstallPath using
the enrolled scientist account. Do not use LocalSystem, a shared service
account, NSSM, or a scheduled task. Inventory any existing HelixAgent service
before changing it; this installer never replaces or removes installed state.
"@
exit 1
