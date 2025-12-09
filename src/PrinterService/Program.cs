using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var isConsole = args is not null && args.Contains("--console");

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<PrinterHttpService>();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddEventLog();
    });

if (!isConsole)
{
    builder = builder.UseWindowsService();
}

await builder.Build().RunAsync();
