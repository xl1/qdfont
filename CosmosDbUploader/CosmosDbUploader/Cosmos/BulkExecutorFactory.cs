using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace CosmosDbUploader.Cosmos
{
    public class BulkExecutorFactory : IBulkExecutorFactory, IDisposable
    {
        private readonly IOptions<DbConfig> _cosmos;
        private readonly DocumentClient _client;

        public BulkExecutorFactory(IOptions<DbConfig> cosmos)
        {
            _cosmos = cosmos;
            _client = new DocumentClient(
                new Uri(_cosmos.Value.EndpointUrl),
                _cosmos.Value.AuthorizationKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp,
                });
        }

        public async Task<IBulkExecutor> CreateAsync()
        {
            var databaseUri = UriFactory.CreateDatabaseUri(_cosmos.Value.DatabaseId);
            var collection = new DocumentCollection { Id = _cosmos.Value.DatasetContainerId };
            collection.PartitionKey.Paths.Add("/word");
            collection.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            collection.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/drawing/*" });

            var collectionResponse = await _client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, collection);

            var bulkExecutor = new BulkExecutor(_client, collectionResponse);
            await bulkExecutor.InitializeAsync();

            _client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
            _client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;
            return bulkExecutor;
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
