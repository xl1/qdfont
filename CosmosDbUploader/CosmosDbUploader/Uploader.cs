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
        private const int uploadBatchSize = 100_000;

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
            var bulkExecutor = await _executorFactory.CreateAsync();

            var documents = new List<Models.Drawing>();
            var idCounter = new Dictionary<string, int>();
            await foreach (string line in _console.LoadAsync())
            {
                var drawing = JsonConvert.DeserializeObject<Models.Drawing>(line);
                if (drawing != null && !string.IsNullOrEmpty(drawing.word) && drawing.recognized)
                {
                    int id = idCounter.ContainsKey(drawing.word)
                        ? idCounter[drawing.word] += 1
                        : idCounter[drawing.word] = 0;
                    drawing.id = id.ToString().PadLeft(8, '0');
                    documents.Add(drawing);

                    if (documents.Count >= uploadBatchSize)
                    {
                        await Upload(bulkExecutor, documents, stoppingToken);
                        documents.Clear();
                    }
                }
            }
            await Upload(bulkExecutor, documents, stoppingToken);
        }

        private async Task Upload(Microsoft.Azure.CosmosDB.BulkExecutor.IBulkExecutor bulkExecutor,
            IReadOnlyList<Models.Drawing> documents,
            CancellationToken stoppingToken)
        {
            if (documents.Count == 0) return;

            var result = await bulkExecutor.BulkImportAsync(documents, cancellationToken: stoppingToken);

            _logger.LogInformation($"Inserted {result.NumberOfDocumentsImported} documents");
            _logger.LogInformation($"Consumed {result.TotalRequestUnitsConsumed} RU");
            _logger.LogInformation($"Finished in {result.TotalTimeTaken.TotalSeconds} sec");
        }
    }
}
