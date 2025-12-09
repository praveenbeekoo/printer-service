param(
    [Parameter(Mandatory=$true)]
    [string] $ExePath,
    [string] $ServiceName = 'PrinterService'
)

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found: $ExePath"
    exit 1
}

$binPath = '"' + (Resolve-Path $ExePath).ProviderPath + '"'

Write-Output "Creating service $ServiceName with path $binPath"

sc.exe create $ServiceName binPath= $binPath start= auto
if ($LASTEXITCODE -ne 0) { Write-Error "sc create failed"; exit $LASTEXITCODE }

Write-Output "Service created. Start service with: sc start $ServiceName"
