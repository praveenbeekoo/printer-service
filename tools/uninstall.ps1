param(
    [string] $ServiceName = 'PrinterService'
)

Write-Output "Stopping service $ServiceName if running"
sc.exe stop $ServiceName | Out-Null
Start-Sleep -Seconds 1
Write-Output "Deleting service $ServiceName"
sc.exe delete $ServiceName

Write-Output "Service $ServiceName deleted (if it existed)."
