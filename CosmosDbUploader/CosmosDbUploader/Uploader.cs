using CosmosDbUploader.Adapters;
using CosmosDbUploader.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDbUploader
{
    public class Uploader : BackgroundService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<Uploader> _logger;
        private readonly IJsonLoader _console;
        private readonly IBulkExecutorFactory _executorFactory;

        public Uploader(IHostApplicationLifetime lifetime,
            ILogger<Uploader> logger,
            IJsonLoader console,
            IBulkExecutorFactory executorFactory)
        {
            _lifetime = lifetime;
            _logger = logger;
            _console = console;
            _executorFactory = executorFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "RunAsync threw an exception");
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            var documents = new List<Models.Drawing>();
            int i = 0;
            await foreach (string line in _console.LoadAsync())
            {
                var drawing = JsonConvert.DeserializeObject<Models.Drawing>(line);
                if (drawing.recognized)
                {
                    drawing.id = i++.ToString().PadLeft(8, '0');
                    documents.Add(drawing);
                }
            }

            var bulkExecutor = await _executorFactory.CreateAsync();
            var result = await bulkExecutor.BulkImportAsync(documents, cancellationToken: stoppingToken);

            _logger.LogInformation($"Inserted {result.NumberOfDocumentsImported} documents");
            _logger.LogInformation($"Consumed {result.TotalRequestUnitsConsumed} RU");
            _logger.LogInformation($"Finished in {result.TotalTimeTaken.TotalSeconds} sec");
        }
    }
}
