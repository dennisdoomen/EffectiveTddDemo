using System;
using System.IO;
using Microsoft.Owin.Hosting;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Raven.Database.Server;

namespace LiquidProjections.ExampleHost
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var eventStore = new JsonFileEventStore("ExampleEvents.zip", 100);

            EmbeddableDocumentStore store = BuildDocumentStore(".\\", 9001);

            var dispatcher = new Dispatcher(eventStore.Subscribe);

            var bootstrapper = new CountsProjector(dispatcher, store.OpenAsyncSession);

            var startOptions = new StartOptions($"http://localhost:9000");
            using (WebApp.Start(startOptions, builder => builder.UseStatistics(() => store.OpenAsyncSession())))
            {
                bootstrapper.Start().Wait();

                Console.WriteLine($"HTTP endpoint available at http://localhost:9000/api/Statistics/CountsPerState");
                Console.WriteLine($"Management Studio available at http://localhost:9001");

                Console.ReadLine();
            }
        }

        private static EmbeddableDocumentStore BuildDocumentStore(string rootDir, int? studioPort)
        {
            var dataDir = Path.Combine(rootDir, "Projections");
            var documentStore = new EmbeddableDocumentStore
            {
                DataDirectory = dataDir,
                DefaultDatabase = "Default",
                Conventions =
                {
                    MaxNumberOfRequestsPerSession = 100,
                    ShouldCacheRequest = (url) => false
                },
                Configuration =
                {
                    DisableInMemoryIndexing = true,
                    DataDirectory = dataDir,
                    CompiledIndexCacheDirectory = Path.Combine(rootDir, "CompiledIndexCache"),
                    DefaultStorageTypeName = "Esent",
                },
                EnlistInDistributedTransactions = false,
            };

            documentStore.Configuration.Settings.Add("Raven/Esent/CacheSizeMax", "256");
            documentStore.Configuration.Settings.Add("Raven/Esent/MaxVerPages", "32");
            documentStore.Configuration.Settings.Add("Raven/MemoryCacheLimitMegabytes", "512");
            documentStore.Configuration.Settings.Add("Raven/MaxNumberOfItemsToIndexInSingleBatch", "4096");
            documentStore.Configuration.Settings.Add("Raven/MaxNumberOfItemsToPreFetchForIndexing", "4096");
            documentStore.Configuration.Settings.Add("Raven/InitialNumberOfItemsToIndexInSingleBatch", "64");

            if (studioPort.HasValue)
            {
                NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(studioPort.Value);
                documentStore.UseEmbeddedHttpServer = true;
                documentStore.Configuration.Port = studioPort.Value;
            }

            documentStore.Initialize();

            IndexCreation.CreateIndexes(typeof(Program).Assembly, documentStore);

            return documentStore;
        }
    }
}