using System.Threading.Tasks;
using Microsoft.Azure.CosmosDB.BulkExecutor;

namespace CosmosDbUploader.Cosmos
{
    public interface IBulkExecutorFactory
    {
        Task<IBulkExecutor> CreateAsync();
    }
}