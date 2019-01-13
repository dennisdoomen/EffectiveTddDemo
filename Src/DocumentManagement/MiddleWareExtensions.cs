using System;
using System.Threading.Tasks;
using DocumentManagement.Common;
using DocumentManagement.Statistics;
using LiquidProjections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;

namespace DocumentManagement
{
    public static class ApplicationBuilderExtensions
    {
        public static IStartableModule UseDocumentStatisticsModule(this IApplicationBuilder appBuilder, IDocumentStore ravenDatabase, Dispatcher eventDispatcher)
        {
            var module = new DocumentStatisticsModule(ravenDatabase, eventDispatcher);

            module.AttachToRequestPipeline(appBuilder);
            
            return module;
        }

        private class DocumentStatisticsModule : IStartableModule
        {
            private readonly IDocumentStore ravenDatabase;
            private readonly CountsProjector projector;

            public DocumentStatisticsModule(IDocumentStore ravenDatabase, Dispatcher eventDispatcher)
            {
                this.ravenDatabase = ravenDatabase;
                
                projector = new CountsProjector(eventDispatcher, ravenDatabase.OpenAsyncSession);
                
                new Documents_ByDynamicState().Execute(ravenDatabase);
                new Documents_CountsByStaticState().Execute(ravenDatabase);
            }

            public void AttachToRequestPipeline(IApplicationBuilder appBuilder)
            {
                appBuilder.IsolatedMap(
                    "/statistics", 
                    app => app.UseMvc(), 
                    services =>
                    {
                        services
                            .AddMvc()
                            .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());;
                
                        services.AddSingleton<GetRavenSession>(() => ravenDatabase.OpenAsyncSession());
                    });
            }

            public async Task Start()
            {
                await projector.Start();
            }

            public void Dispose()
            {
                projector.Dispose();
            }
        }
    }

    public interface IStartableModule : IDisposable
    {
        Task Start();
    }
}