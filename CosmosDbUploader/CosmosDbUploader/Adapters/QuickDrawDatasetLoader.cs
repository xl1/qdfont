using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace CosmosDbUploader.Adapters
{
    public class QuickDrawDatasetLoader : IJsonLoader
    {
        private readonly QuickDrawDatasetConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Uri _categoryUri = new Uri("https://raw.githubusercontent.com/googlecreativelab/quickdraw-dataset/master/categories.txt");
        private readonly Uri _baseUri = new Uri("https://storage.googleapis.com/quickdraw_dataset/full/simplified/");

        public QuickDrawDatasetLoader(IOptions<QuickDrawDatasetConfig> config, IHttpClientFactory httpClientFactory)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async IAsyncEnumerable<string> LoadAsync()
        {
            var client = _httpClientFactory.CreateClient();
            await foreach (var category in LoadLinesAsync(client, _categoryUri))
            {
                var uri = new Uri(_baseUri, $"{category}.ndjson");
                await foreach (var line in LoadLinesAsync(client, uri, _config.Count))
                    yield return line;
            }
        }

        private async IAsyncEnumerable<string> LoadLinesAsync(HttpClient client, Uri uri, int take = 0)
        {
            using var message = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            message.EnsureSuccessStatusCode();

            using var stream = await message.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            int count = 0;
            while (!reader.EndOfStream)
            {
                yield return (await reader.ReadLineAsync())!;
                if (++count == take)
                    yield break;
            }
        }
    }
}
