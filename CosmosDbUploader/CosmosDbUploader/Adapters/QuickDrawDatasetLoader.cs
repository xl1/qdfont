using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace CosmosDbUploader.Adapters
{
    public class QuickDrawDatasetLoader : IJsonLoader
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Uri _categoryUri = new Uri("https://raw.githubusercontent.com/googlecreativelab/quickdraw-dataset/master/categories.txt");
        private readonly Uri _baseUri = new Uri("https://storage.googleapis.com/quickdraw_dataset/full/simplified/");

        public QuickDrawDatasetLoader(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async IAsyncEnumerable<string> LoadAsync()
        {
            var client = _httpClientFactory.CreateClient();
            await foreach (var category in LoadLinesAsync(client, _categoryUri))
            {
                var uri = new Uri(_baseUri, $"{category}.ndjson");
                await foreach (var line in LoadLinesAsync(client, uri))
                    yield return line;
            }
        }

        private async IAsyncEnumerable<string> LoadLinesAsync(HttpClient client, Uri uri)
        {
            var message = await client.GetAsync(uri);
            message.EnsureSuccessStatusCode();

            using var stream = await message.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
                yield return (await reader.ReadLineAsync())!;
        }
    }
}
