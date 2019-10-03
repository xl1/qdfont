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
        private Mock<IHostApplicationLifetime> _lifetime;
        private Mock<ILogger<Uploader>> _logger;
        private Mock<IConsoleReader> _console;
        private Mock<IBulkExecutor> _executor;
        private Mock<IBulkExecutorFactory> _executorFactory;
        private CancellationTokenSource _cts;

        [TestInitialize]
        public void Initialize()
        {
            _lifetime = new Mock<IHostApplicationLifetime>();
            _logger = new Mock<ILogger<Uploader>>();
            _console = new Mock<IConsoleReader>();
            _executor = new Mock<IBulkExecutor>();
            _executor.Setup(e => e.BulkImportAsync(It.IsAny<List<Drawing>>(), false, true, null, null, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Microsoft.Azure.CosmosDB.BulkExecutor.BulkImport.BulkImportResponse()));
            _executorFactory = new Mock<IBulkExecutorFactory>();
            _executorFactory.Setup(f => f.CreateAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(_executor.Object));
            _cts = new CancellationTokenSource();
        }

        [TestMethod]
        public async Task RunAsync_StopsApplication()
        {
            _console.Setup(c => c.ReadLine()).Returns((string)null);

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);

            _lifetime.Verify(l => l.StopApplication());
        }

        [TestMethod]
        public async Task RunAsync_UploadDocuments()
        {
            _console.SetupSequence(c => c.ReadLine())
                .Returns("{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}")
                .Returns((string)null);

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);

            _executor.Verify(e =>
                e.BulkImportAsync(It.Is<List<Drawing>>(l => l.Count == 1), false, true, null, null, _cts.Token));
        }

        [TestMethod]
        public async Task RunAsync_IgnoresUnrecognizedValues()
        {
            _console.SetupSequence(c => c.ReadLine())
                .Returns("{\"word\": \"test\", \"recognized\": false, \"drawing\": [[[], []]]}")
                .Returns("{\"word\": \"test\", \"recognized\": false, \"drawing\": [[[], []]]}")
                .Returns((string)null);

            using var uploader = new Uploader(_lifetime.Object, _logger.Object, _console.Object, _executorFactory.Object);
            await uploader.RunAsync(_cts.Token);

            _executor.Verify(e =>
                e.BulkImportAsync(It.Is<List<Drawing>>(l => l.Count == 0), false, true, null, null, _cts.Token));
        }

        [TestMethod]
        public async Task RunAsync_SetsDocumentIds()
        {
            _console.SetupSequence(c => c.ReadLine())
                .Returns("{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}")
                .Returns("{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}")
                .Returns("{\"word\": \"test\", \"recognized\": false, \"drawing\": [[[], []]]}")
                .Returns("{\"word\": \"test\", \"recognized\": true, \"drawing\": [[[], []]]}")
                .Returns((string)null);

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
