# printer-service

This repository implements the Windows Printer HTTP Service (WPHS) — a lightweight Windows Service that exposes local printers over HTTP and accepts raw ESC/POS data.

## Project: `src/PrinterService`

Quick start (requires .NET 7 SDK):

```cmd
cd src\PrinterService
dotnet publish -c Release -r win-x64 -o publish
```

Install as Windows Service (run as Administrator):

```cmd
sc create PrinterService binPath= "C:\\Path\\To\\PrinterService.exe" start= auto
sc start PrinterService
```

Remove service:

```cmd
sc stop PrinterService
sc delete PrinterService
```

Service listens on `http://localhost:8080/` by default. See the SRS (`srs.txt`) for API details.