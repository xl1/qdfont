using System.Collections.Generic;

namespace CosmosDbUploader.Adapters
{
    public interface IJsonLoader
    {
        IAsyncEnumerable<string> LoadAsync();
    }
}