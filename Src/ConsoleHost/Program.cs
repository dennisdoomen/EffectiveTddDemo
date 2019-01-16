using System;
using System.Threading.Tasks;
using DocumentManagement;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;
using Raven.Embedded;

namespace ConsoleHost
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var eventStore = new JsonFileEventStore("ExampleEvents.zip", 100);

            IDocumentStore documentStore = BuildDocumentStore();

            var dispatcher = new Dispatcher(eventStore.Subscribe);

            IStartableModule module = null;
            
            IWebHostBuilder hostBuilder = WebHost
                .CreateDefaultBuilder(args)
                .UseUrls("http://*:9000")
                .ConfigureServices(services =>
                {

                })
                .Configure(appBuilder =>
                {
                     module = appBuilder.UseDocumentStatisticsModule(documentStore, dispatcher);
                });
                

            using (var host = hostBuilder.Build())
            {
                host.Start();
                await module.Start();

                Console.WriteLine("The statistics module is running. ");
                Console.WriteLine($"Try http://localhost:9000/Statistics/CountsPerState?country=6df7e2ac-6f06-420a-a0b5-14fb3865e850&kind=permit");
                Console.WriteLine($"Examine the Raven DB at http://localhost:9001");

                Console.ReadLine();
            }
        }

        private static IDocumentStore BuildDocumentStore()
        {
            EmbeddedServer.Instance.StartServer(new ServerOptions
            {
                DataDirectory = ".\\",
                ServerUrl = "http://127.0.0.1:9001",
                
            });

            IDocumentStore documentStore = EmbeddedServer.Instance.GetDocumentStore(new DatabaseOptions("embedded")
            {
                Conventions = new DocumentConventions
                {
                    MaxNumberOfRequestsPerSession = 200
                }
            });

            return documentStore;
        }
    }
}