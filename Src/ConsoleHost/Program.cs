using System;
using System.Threading.Tasks;
using DocumentManagement;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Embedded;

namespace ConsoleHost
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://*:9000");
                    webBuilder.ConfigureServices(sc =>
                    {
                        var eventStore = new JsonFileEventStore("ExampleEvents.zip", 100);
                        var dispatcher = new Dispatcher(eventStore.Subscribe);
                        sc.AddSingleton(dispatcher);

                        sc.AddSingleton<IDocumentStore>(BuildDocumentStore());
                    });
                });

            using var host = hostBuilder.Build();
            
            Console.WriteLine("The statistics module is running. ");
            Console.WriteLine(
                $"Try http://localhost:9000/Statistics/Metrics/CountsPerState?country=6df7e2ac-6f06-420a-a0b5-14fb3865e850&kind=permit");
            Console.WriteLine($"Examine the Raven DB at http://localhost:9001");

            await host.RunAsync();
        }

        private static IDocumentStore BuildDocumentStore()
        {
            EmbeddedServer.Instance.StartServer(new ServerOptions
            {
                DataDirectory = ".\\",
                ServerUrl = "http://127.0.0.1:9001",
                FrameworkVersion = "5.0.1"
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