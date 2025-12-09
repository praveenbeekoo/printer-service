using System.Net;
using System.Printing;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PrinterHttpService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpListener _listener;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _listener = new HttpListener();
            // You can change the port or hostname here or via config in future
            _listener.Prefixes.Add("http://+:8080/");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener.Start();
            _logger.LogInformation("Printer HTTP Service started on http://+:8080/");

            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext? context = null;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (HttpListenerException hlex) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Listener stopped: {Message}", hlex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting HTTP request");
                    continue;
                }

                _ = Task.Run(() => HandleRequestAsync(context, stoppingToken), stoppingToken);
            }

            _listener.Stop();
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken token)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                _logger.LogInformation("Incoming request: {Method} {Url}", request.HttpMethod, request.Url);

                if (request.HttpMethod == "GET" &&
                    request.Url?.AbsolutePath.Equals("/printers", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await HandlePrintersAsync(context);
                }
                else if (request.HttpMethod == "POST" &&
                         request.Url?.AbsolutePath.Equals("/print", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await HandlePrintHexAsync(context);
                }
                else if (request.HttpMethod == "POST" &&
                         request.Url?.AbsolutePath.Equals("/print/base64", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await HandlePrintBase64Async(context);
                }
                else
                {
                    await NotFoundAsync(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling request");
                try
                {
                    context.Response.StatusCode = 500;
                    var msg = Encoding.UTF8.GetBytes("Internal server error");
                    await context.Response.OutputStream.WriteAsync(msg, 0, msg.Length);
                    context.Response.Close();
                }
                catch
                {
                    // ignore secondary errors
                }
            }
        }

        private async Task HandlePrintersAsync(HttpListenerContext ctx)
        {
            try
            {
                var server = new LocalPrintServer();
                var queues = server.GetPrintQueues();

                var printers = queues.Select(q => new PrinterInfo
                {
                    Name = q.FullName,
                    Status = q.QueueStatus.ToString(),
                    IsOffline = q.IsOffline,
                    IsBusy = q.IsBusy,
                    Location = q.Location ?? string.Empty
                }).ToList();

                string json = JsonSerializer.Serialize(printers, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                byte[] buffer = Encoding.UTF8.GetBytes(json);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = buffer.Length;

                await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while enumerating printers");
                ctx.Response.StatusCode = 500;
                var msg = Encoding.UTF8.GetBytes("Failed to enumerate printers");
                await ctx.Response.OutputStream.WriteAsync(msg, 0, msg.Length);
            }
            finally
            {
                ctx.Response.OutputStream.Close();
                ctx.Response.Close();
            }
        }

        private async Task HandlePrintHexAsync(HttpListenerContext ctx)
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }

            PrintHexRequest? printRequest;
            try
            {
                printRequest = JsonSerializer.Deserialize<PrintHexRequest>(body);
            }
            catch
            {
                ctx.Response.StatusCode = 400;
                await WriteTextAsync(ctx, "Invalid JSON");
                return;
            }

            if (printRequest == null ||
                string.IsNullOrWhiteSpace(printRequest.Printer) ||
                string.IsNullOrWhiteSpace(printRequest.Data))
            {
                ctx.Response.StatusCode = 400;
                await WriteTextAsync(ctx, "Missing printer or data");
                return;
            }

            try
            {
                byte[] bytes = HexToBytes(printRequest.Data);
                RawPrinterHelper.SendBytesToPrinter(printRequest.Printer, bytes);
                ctx.Response.StatusCode = 200;
                await WriteTextAsync(ctx, "Printed OK");
            }
            catch (FormatException)
            {
                ctx.Response.StatusCode = 400;
                await WriteTextAsync(ctx, "Invalid HEX data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while printing HEX data");
                ctx.Response.StatusCode = 500;
                await WriteTextAsync(ctx, "Error: " + ex.Message);
            }
        }

        private async Task HandlePrintBase64Async(HttpListenerContext ctx)
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }

            PrintBase64Request? request;
            try
            {
                request = JsonSerializer.Deserialize<PrintBase64Request>(body);
            }
            catch
            {
                ctx.Response.StatusCode = 400;
                await WriteTextAsync(ctx, "Invalid JSON");
                return;
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Printer) ||
                string.IsNullOrWhiteSpace(request.Data))
            {
                ctx.Response.StatusCode = 400;
                await WriteTextAsync(ctx, "Missing printer or data");
                return;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(request.Data);
                RawPrinterHelper.SendBytesToPrinter(request.Printer, bytes);
                ctx.Response.StatusCode = 200;
                await WriteTextAsync(ctx, "Printed OK");
            }
            catch (FormatException)
            {
                ctx.Response.StatusCode = 400;
                await WriteTextAsync(ctx, "Invalid Base64 data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while printing Base64 data");
                ctx.Response.StatusCode = 500;
                await WriteTextAsync(ctx, "Error: " + ex.Message);
            }
        }

        private async Task NotFoundAsync(HttpListenerContext ctx)
        {
            ctx.Response.StatusCode = 404;
            await WriteTextAsync(ctx, "Not found");
        }

        private static async Task WriteTextAsync(HttpListenerContext ctx, string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            ctx.Response.ContentType = "text/plain; charset=utf-8";
            ctx.Response.ContentLength64 = buffer.Length;
            await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            ctx.Response.OutputStream.Close();
            ctx.Response.Close();
        }

        private static byte[] HexToBytes(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");
            if (hex.Length % 2 != 0)
            {
                throw new FormatException("Invalid hex string length");
            }

            int len = hex.Length;
            byte[] bytes = new byte[len / 2];

            for (int i = 0; i < len; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Printer HTTP Service...");
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            return base.StopAsync(cancellationToken);
        }
    }
}