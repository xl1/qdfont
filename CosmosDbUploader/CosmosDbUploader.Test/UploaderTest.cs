using CosmosDbUploader.Adapters;
using CosmosDbUploader.Cosmos;
using CosmosDbUploader.Models;
using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDbUploader.Test
{
    [TestClass]
    public class UploaderTest
    {
        private readonly Mock<IHostApplicationLifetime> _lifetime = new Mock<IHostApplicationLifetime>();
        private readonly Mock<ILogger<Uploader>> _logger = new Mock<ILogger<Uploader>>();
        private readonly Mock<IJsonLoader> _console = new Mock<IJsonLoader>();
        private readonly Mock<IBulkExecutor> _executor = new Mock<IBulkExecutor>();
        private readonly Mock<IBulkExecutorFactory> _executorFactory = new Mock<IBulkExecutorFactory>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly AsyncEnumerableBuilder _async = new AsyncEnumerableBuilder();

        [TestInitialize]
        public void Initialize()
        {
            _executor.Setup(e => e.BulkImportAsync(It.IsAny<List<Drawing>>(), false, true, null, null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Microsoft.Azure.CosmosDB.BulkExecutor.BulkImport.BulkImportResponse()));
            _executorFactory.Setup(f => f.CreateAsync())
                .Returns(Task.FromResult(_executor.Object));
        }

        [TestMethod]
        public async Task RunAsync_DoesNotStopsApplication()
        {
            _console.Setup(c => c.LoadAsync()).Returns(_async.From<string>());

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);

            _lifetime.Verify(l => l.StopApplication(), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(Newtonsoft.Json.JsonReaderException))]
        public async Task RunAsync_ThrowsOnInvalidJsonInput()
        {
            _console.Setup(c => c.LoadAsync()).Returns(_async.From("invalid json"));

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);
        }

        [TestMethod]
        public async Task RunAsync_UploadDocuments()
        {
            _console.SetupSequence(c => c.LoadAsync())
                .Returns(_async.From("{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}"));

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);

            _executor.Verify(e =>
                e.BulkImportAsync(It.Is<List<Drawing>>(l => l.Count == 1), false, true, null, null, _cts.Token));
        }

        [TestMethod]
        public async Task RunAsync_IgnoresUnrecognizedValues()
        {
            _console.SetupSequence(c => c.LoadAsync())
                .Returns(_async.From(
                    "{\"word\": \"test\", \"recognized\": false, \"drawing\": [[[], []]]}",
                    "{\"word\": \"test\", \"recognized\": false, \"drawing\": [[[], []]]}"
                ));

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);

            _executor.Verify(e =>
                e.BulkImportAsync(It.IsAny<List<Drawing>>(), false, true, null, null, _cts.Token), Times.Never);
        }

        [TestMethod]
        public async Task RunAsync_SetsDocumentIds()
        {
            _console.SetupSequence(c => c.LoadAsync())
                .Returns(_async.From(
                    "{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}",
                    "{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}",
                    "{\"word\": \"test\", \"recognized\": false, \"drawing\": [[[], []]]}",
                    "{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}"
                ));

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);

            _executor.Verify(e =>
                e.BulkImportAsync(It.Is<List<Drawing>>(xs => Matches(xs)), false, true, null, null, _cts.Token));
        }

        private static bool Matches(List<Drawing> documents) =>
            documents.Count == 3 &&
            documents[0].id == "00000000" &&
            documents[1].id == "00000001" &&
            documents[2].id == "00000002";
    }
}
