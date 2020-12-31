using DocumentManagement.Modularization;
using LiquidProjections;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;

namespace DocumentManagement.Specs._05_TestDataBuilders
{
    public class TestHostBuilder : TestDataBuilder<IHost>
    {
        private IDocumentStore documentStore;
        private MemoryEventSource eventStore;
        
        private ModuleRegistry modules = new();

        protected override IHost OnBuild()
        {
            var hostBuilder = new HostBuilder().ConfigureWebHostDefaults(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services
                        .AddServicesFrom(modules)
                        .AddSingleton(documentStore ?? InMemoryRavenTestDriver.Instance.GetDocumentStore())
                        .AddSingleton(new Dispatcher(eventStore.Subscribe))
                        .AddMvcCore().ConfigureMvcUsing(modules);
                    
                }).Configure(app =>
                {
                    EndpointRoutingApplicationBuilderExtensions.UseRouting(app);
                    EndpointRoutingApplicationBuilderExtensions.UseEndpoints(app, o => ControllerEndpointRouteBuilderExtensions.MapControllers(o));
                });
            });
            
            return hostBuilder.Start();
        }

        public TestHostBuilder Using(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
            return this;
        }

        public TestHostBuilder Using(MemoryEventSource eventStore)
        {
            this.eventStore = eventStore;
            return this;
        }

        public TestHostBuilder WithModules(ModuleRegistry modules)
        {
            this.modules = modules;
            return this;
        }
    }
}