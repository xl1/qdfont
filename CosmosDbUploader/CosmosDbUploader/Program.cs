using CosmosDbUploader.Adapters;
using CosmosDbUploader.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CosmosDbUploader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.SetBasePath(System.IO.Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json");
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging(log =>
                {
                    log.SetMinimumLevel(LogLevel.Information);
                    log.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<DbConfig>(hostContext.Configuration.GetSection("CosmosDb"));
                    services.AddTransient<IJsonLoader, ConsoleReader>();
                    services.AddSingleton<IBulkExecutorFactory, BulkExecutorFactory>();
                    services.AddHostedService<Uploader>();
                })
                .RunConsoleAsync();
        }
    }
}
