using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDbUploader.Adapters
{
    public class ConsoleReader : IJsonLoader
    {
        public async IAsyncEnumerable<string> LoadAsync()
        {
            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                yield return await new ValueTask<string>(line);
            }
        }
    }
}
