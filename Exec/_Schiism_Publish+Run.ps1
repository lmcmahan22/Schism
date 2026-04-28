# ============================================
# Schiism Service Dev Deployment Script
# Written by ChatGPT, reviewed by LJM
# ============================================

# These parameters let you optionally change behavior when running the script:
# Example usage:
# .\dev.ps1              → normal publish + restart
# .\dev.ps1 -Install     → reinstall service
# .\dev.ps1 -Clean       → wipe publish folder first

# Define optional in line parameters
param(
[switch]$Install,
[switch]$Clean
)

# Define publish directory
$publishDir = "C:\Users\lmcmahan\Github\Schism\Exec"

# Path to .csproj file (used for dotnet publish command)
$projectPath = "C:\Users\lmcmahan\Github\Schism\Schiism\Schiism.Service\Schiism.Service.csproj"

# Print to console
Write-Host ""
Write-Host "=== SCHIISM DEPLOY SCRIPT ==="
Write-Host ""

# Stop service (safe even if already stopped)

Write-Host "Verifying that Service is Stopped..."
sc.exe stop SchiismModbusClientService
Start-Sleep -Seconds 1

# If -Clean is passed, delete everything in the publish folder
# This ensures no stale files remain from previous builds
if ($Clean) {
Write-Host "Cleaning publish directory..."
Remove-Item "$publishDir*" -Recurse -Force -ErrorAction SilentlyContinue
}

# Publish the application with a console message, indicating that we're doing so
Write-Host "Publishing application..."
dotnet publish $projectPath -c Release -r win-x64 -o $publishDir

# If -Install is passed, install the service. The Service project is written such that calling this will remove and re-install, if already present
# NOTE: If both -Install and -Clean are passed, Clean happens first (by nature of this top to bottom script)
if ($Install) {
Write-Host "Installing (or reinstalling) service..."

# Stop service if it's running (ignore errors if it isn't)
# Delete existing service registration
# Create the service pointing to your published EXE
.\Schiism.Service.exe -install

Write-Host "Service installed at: $publishDir\Schiism.Service.exe"
}

# Start service again (this loads your newly published EXE)

Write-Host "Starting service..."
sc.exe start SchiismModbusClientService
Start-Sleep -Seconds 1

# Show current state (RUNNING / STOPPED / etc.)
Write-Host ""
Write-Host "Service status:"
sc.exe query SchiismModbusClientService

Write-Host ""
Write-Host "=== DONE ==="
