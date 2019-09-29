using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
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

        public async Task<IBulkExecutor> CreateAsync(string containerId)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(_cosmos.Value.DatabaseId);
            var collection = _client.CreateDocumentCollectionQuery(databaseUri)
                .Where(c => c.Id == containerId).AsEnumerable().FirstOrDefault();

            var bulkExecutor = new BulkExecutor(_client, collection);
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
