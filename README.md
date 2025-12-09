# Windows Printer HTTP Service

This project is a Windows Service that exposes local printers over a simple HTTP API and supports sending raw ESC/POS commands using HEX or Base64.

## Features

- Enumerate installed printers via HTTP
- Print raw ESC/POS commands via HEX (`/print`)
- Print raw ESC/POS commands via Base64 (`/print/base64`)
- Runs as a Windows Service
- Minimal dependencies (.NET 8, HttpListener, Win32 spooler API)

## Endpoints

### `GET /printers`

Returns a JSON list of printers:

```json
[
  {
    "name": "POS-80",
    "status": "None",
    "isOffline": false,
    "isBusy": false,
    "location": ""
  }
]
```

### `POST /print` (HEX)

```json
{
  "printer": "POS-80",
  "data": "1B40 48 65 6C 6C 6F 0A"
}
```

### `POST /print/base64`

```json
{
  "printer": "POS-80",
  "data": "GxBIZWxsbwo="
}
```

## Build

```bash
dotnet restore ./src/PrinterHttpService/PrinterHttpService.csproj
dotnet publish ./src/PrinterHttpService/PrinterHttpService.csproj -c Release -r win-x64 --self-contained true -o ./build/win-x64
```

## Install as Windows Service

From an elevated PowerShell console:

```powershell
sc create PrinterHttpService binPath= "C:\Path\To\PrinterHttpService.exe"
sc start PrinterHttpService
```

To remove:

```powershell
sc stop PrinterHttpService
sc delete PrinterHttpService
```

## License

MIT (or your preferred license).