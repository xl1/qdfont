using CosmosDbUploader.Adapters;
using CosmosDbUploader.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace CosmosDbUploader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<DbConfig>(hostContext.Configuration.GetSection("CosmosDb"));
                    services.Configure<QuickDrawDatasetConfig>(hostContext.Configuration.GetSection("QuickDrawDataset"));
                    services.AddHttpClient();
                    services.AddTransient<IJsonLoader, QuickDrawDatasetLoader>();
                    services.AddSingleton<IBulkExecutorFactory, BulkExecutorFactory>();
                    services.AddHostedService<Uploader>();
                })
                .RunConsoleAsync();
        }
    }
}
