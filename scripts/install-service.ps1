param(
    [string]$ServiceName = "PrinterHttpService",
    [string]$InstallPath = "C:\PrinterHttpService"
)

Write-Host "Installing Printer HTTP Service..."

if (-Not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptDir "..\build\win-x64"

if (-Not (Test-Path $sourcePath)) {
    Write-Error "Build folder not found at $sourcePath. Please build the project first."
    exit 1
}

Copy-Item "$sourcePath\*" $InstallPath -Recurse -Force

$exePath = Join-Path $InstallPath "PrinterHttpService.exe"

Write-Host "Creating Windows Service $ServiceName with binPath $exePath"

sc.exe create $ServiceName binPath= "`"$exePath`"" start= auto
sc.exe start $ServiceName

Write-Host "Service installed and started."