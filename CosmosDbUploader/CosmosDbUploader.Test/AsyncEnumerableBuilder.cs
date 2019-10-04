using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDbUploader.Test
{
    public class AsyncEnumerableBuilder
    {
        public async IAsyncEnumerable<T> From<T>(params T[] xs)
        {
            foreach (T x in xs)
                yield return await new ValueTask<T>(x);
        }
    }
}
