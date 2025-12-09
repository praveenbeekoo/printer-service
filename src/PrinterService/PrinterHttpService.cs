using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Drawing.Printing;

namespace PrinterService;

public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsOffline { get; set; }
    public bool IsBusy { get; set; }
}

public record PrintRequest(string Printer, string Data);

public class PrinterHttpService : BackgroundService
{
    private readonly HttpListener _listener = new HttpListener();
    private readonly string _prefix = "http://localhost:8080/";
    private readonly ILogger<PrinterHttpService> _logger;

    private readonly bool _dryRun = Environment.GetEnvironmentVariable("DRY_RUN") == "1";

    public PrinterHttpService(ILogger<PrinterHttpService> logger)
    {
        _logger = logger;
        _logger.LogInformation("PrinterHttpService starting (dryRun={dryRun})", _dryRun);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Prefixes.Add(_prefix);
        _listener.Start();
        _ = Task.Run(() => ListenLoop(stoppingToken));
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _listener.Stop(); } catch { }
        return base.StopAsync(cancellationToken);
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch
            {
                break;
            }
            _ = Task.Run(() => HandleRequest(ctx));
        }
    }

    private async Task HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;
            if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/printers")
            {
                var printers = GetPrinters();
                var json = JsonSerializer.Serialize(printers);
                var data = Encoding.UTF8.GetBytes(json);
                res.ContentType = "application/json";
                res.ContentLength64 = data.Length;
                await res.OutputStream.WriteAsync(data, 0, data.Length);
                _logger.LogInformation("/printers returned {count} printers", printers.Count);
                return;
            }

            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/print")
            {
                using var sr = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = await sr.ReadToEndAsync();
                PrintRequest? pr;
                try { pr = JsonSerializer.Deserialize<PrintRequest>(body); } catch { pr = null; }
                if (pr == null || string.IsNullOrWhiteSpace(pr.Printer) || string.IsNullOrWhiteSpace(pr.Data))
                {
                    res.StatusCode = 400;
                    await WriteString(res, "Invalid payload");
                    return;
                }

                try
                {
                    var bytes = EncodingHelper.HexStringToBytes(pr.Data);
                    if (_dryRun)
                    {
                        _logger.LogInformation("Dry-run: would send {len} bytes to printer {printer}", bytes.Length, pr.Printer);
                    }
                    else
                    {
                        RawPrinterHelper.SendBytesToPrinter(pr.Printer, bytes);
                        _logger.LogInformation("Sent {len} bytes to printer {printer}", bytes.Length, pr.Printer);
                    }
                    await WriteString(res, "OK");
                }
                catch (FormatException fex)
                {
                    _logger.LogWarning(fex, "Invalid HEX payload");
                    res.StatusCode = 400;
                    await WriteString(res, fex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Print failed");
                    res.StatusCode = 500;
                    await WriteString(res, ex.Message);
                }
                return;
            }

            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/print/base64")
            {
                using var sr = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = await sr.ReadToEndAsync();
                PrintRequest? pr;
                try { pr = JsonSerializer.Deserialize<PrintRequest>(body); } catch { pr = null; }
                if (pr == null || string.IsNullOrWhiteSpace(pr.Printer) || string.IsNullOrWhiteSpace(pr.Data))
                {
                    res.StatusCode = 400;
                    await WriteString(res, "Invalid payload");
                    return;
                }
                try
                {
                    if (!EncodingHelper.TryDecodeBase64(pr.Data, out var bytes))
                    {
                        _logger.LogWarning("Invalid Base64 payload");
                        res.StatusCode = 400;
                        await WriteString(res, "Invalid Base64");
                        return;
                    }
                    if (_dryRun)
                    {
                        _logger.LogInformation("Dry-run: would send {len} bytes (base64) to printer {printer}", bytes.Length, pr.Printer);
                    }
                    else
                    {
                        RawPrinterHelper.SendBytesToPrinter(pr.Printer, bytes);
                        _logger.LogInformation("Sent {len} bytes (base64) to printer {printer}", bytes.Length, pr.Printer);
                    }
                    await WriteString(res, "OK");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Base64 print failed");
                    res.StatusCode = 500;
                    await WriteString(res, ex.Message);
                }
                return;
            }

            res.StatusCode = 404;
            _logger.LogWarning("Not found: {method} {path}", req.HttpMethod, req.Url.AbsolutePath);
            await WriteString(res, "Not found");
        }
        catch (Exception ex)
        {
            try { ctx.Response.StatusCode = 500; await WriteString(ctx.Response, ex.Message); } catch { }
        }
        finally
        {
            try { ctx.Response.OutputStream.Close(); } catch { }
        }
    }

    private async Task WriteString(HttpListenerResponse res, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        res.ContentType = "text/plain";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    }

    private static List<PrinterInfo> GetPrinters()
    {
        var list = new List<PrinterInfo>();
        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            list.Add(new PrinterInfo { Name = name, IsOffline = false, IsBusy = false });
        }
        return list;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Drawing.Printing;

public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsOffline { get; set; }
    public bool IsBusy { get; set; }
}

public record PrintRequest(string Printer, string Data);

public class PrinterHttpService : BackgroundService
{
    private readonly HttpListener _listener = new HttpListener();
    private readonly string _prefix = "http://localhost:8080/";




















































































































































}    }        return bytes;        }            bytes[i] = Convert.ToByte(cleaned.Substring(i * 2, 2), 16);        {        for (int i = 0; i < len; i++)        var bytes = new byte[len];        var len = cleaned.Length / 2;        if (cleaned.Length % 2 != 0) throw new ArgumentException("Invalid HEX string");        var cleaned = hex.Replace(" ", string.Empty).Replace("-", string.Empty);    {    private static byte[] HexStringToBytes(string hex)    }        return list;        }            list.Add(new PrinterInfo { Name = name, IsOffline = false, IsBusy = false });        {        foreach (string name in PrinterSettings.InstalledPrinters)        var list = new List<PrinterInfo>();    {    private static List<PrinterInfo> GetPrinters()    }        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);        res.ContentLength64 = bytes.Length;        res.ContentType = "text/plain";        var bytes = Encoding.UTF8.GetBytes(text);    {    private async Task WriteString(HttpListenerResponse res, string text)    }        }            try { ctx.Response.OutputStream.Close(); } catch { }        {        finally        }            try { ctx.Response.StatusCode = 500; await WriteString(ctx.Response, ex.Message); } catch { }        {        catch (Exception ex)        }            await WriteString(res, "Not found");            res.StatusCode = 404;            }                return;                }                    await WriteString(res, ex.Message);                    res.StatusCode = 500;                {                catch (Exception ex)                }                    await WriteString(res, "OK");                    RawPrinterHelper.SendBytesToPrinter(pr.Printer, bytes);                    catch { res.StatusCode = 400; await WriteString(res, "Invalid Base64"); return; }                    try { bytes = Convert.FromBase64String(pr.Data); }                    byte[] bytes;                {                try                }                    return;                    await WriteString(res, "Invalid payload");                    res.StatusCode = 400;                {                if (pr == null || string.IsNullOrWhiteSpace(pr.Printer) || string.IsNullOrWhiteSpace(pr.Data))                try { pr = JsonSerializer.Deserialize<PrintRequest>(body); } catch { pr = null; }                PrintRequest? pr;                var body = await sr.ReadToEndAsync();                using var sr = new StreamReader(req.InputStream, req.ContentEncoding);            {            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/print/base64")            }                return;                }                    await WriteString(res, ex.Message);                    res.StatusCode = 500;                {                catch (Exception ex)                }                    await WriteString(res, "OK");                    RawPrinterHelper.SendBytesToPrinter(pr.Printer, bytes);                    var bytes = HexStringToBytes(pr.Data);                {                try                }                    return;                    await WriteString(res, "Invalid payload");                    res.StatusCode = 400;                {                if (pr == null || string.IsNullOrWhiteSpace(pr.Printer) || string.IsNullOrWhiteSpace(pr.Data))                try { pr = JsonSerializer.Deserialize<PrintRequest>(body); } catch { pr = null; }                PrintRequest? pr;                var body = await sr.ReadToEndAsync();                using var sr = new StreamReader(req.InputStream, req.ContentEncoding);            {            if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/print")            }                return;                await res.OutputStream.WriteAsync(data, 0, data.Length);                res.ContentLength64 = data.Length;                res.ContentType = "application/json";                var data = Encoding.UTF8.GetBytes(json);                var json = JsonSerializer.Serialize(printers);                var printers = GetPrinters();            {            if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/printers")            var res = ctx.Response;            var req = ctx.Request;        {        try    {    private async Task HandleRequest(HttpListenerContext ctx)    }        }            _ = Task.Run(() => HandleRequest(ctx));            }                break;            {            catch            }                ctx = await _listener.GetContextAsync();            {            try            HttpListenerContext ctx;        {        while (!ct.IsCancellationRequested)    {    private async Task ListenLoop(CancellationToken ct)    }        return base.StopAsync(cancellationToken);        try { _listener.Stop(); } catch { }    {    public override Task StopAsync(CancellationToken cancellationToken)    }        return Task.CompletedTask;        _ = Task.Run(() => ListenLoop(stoppingToken));        _listener.Start();        _listener.Prefixes.Add(_prefix);    {n    protected override Task ExecuteAsync(CancellationToken stoppingToken)