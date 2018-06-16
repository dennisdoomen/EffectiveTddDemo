using System;
using DocumentManagement;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Embedded;

namespace ConsoleHost
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var eventStore = new JsonFileEventStore("ExampleEvents.zip", 100);

            IDocumentStore store = BuildDocumentStore();

            var dispatcher = new Dispatcher(eventStore.Subscribe);

            var bootstrapper = new CountsProjector(dispatcher, store.OpenAsyncSession);

            IWebHost host = WebHost
                .CreateDefaultBuilder(args)
                .UseUrls("http://*:9000")
                .ConfigureServices(services =>
                {

                })
                .Configure(appBuilder =>
                {
                    appBuilder.UseStatistics(store.OpenAsyncSession);
                })
                .Build();

            using (host)
            {
                bootstrapper.Start().Wait();
                host.Start();

                Console.WriteLine($"HTTP endpoint available at http://localhost:9000/api/Statistics/CountsPerState");
                Console.WriteLine($"Management Studio available at http://localhost:9001");


                Console.ReadLine();
            }
        }

        private static IDocumentStore BuildDocumentStore()
        {
            EmbeddedServer.Instance.StartServer(new ServerOptions
            {
                DataDirectory = ".\\",
                ServerUrl = "http://127.0.0.1:9001"
            });

            IDocumentStore documentStore = EmbeddedServer.Instance.GetDocumentStore("embedded");

            IndexCreation.CreateIndexes(typeof(Program).Assembly, documentStore);

            return documentStore;
        }
    }
}