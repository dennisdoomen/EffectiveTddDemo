using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentManagement.Events;
using DocumentManagement.Modularization;
using DocumentManagement.Statistics;
using LiquidProjections;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Xunit;

namespace DocumentManagement.Specs._02_WithAAA
{
    public class StatisticsModuleSpecs
    {
        [Fact]
        public async Task
            When_a_StateTransitionedEvent_is_applied_to_a_DocumentCountProjection_the_controller_should_return_1_active_document()
        {
            // Arrange
            var memoryEventSource = new MemoryEventSource();

            using IDocumentStore ravenDbDocumentStore = InMemoryRavenTestDriver.Instance.GetDocumentStore();

            var modules = new ModuleRegistry(new StatisticsModule());

            var hostBuilder = new HostBuilder().ConfigureWebHostDefaults(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services
                        .AddServicesFrom(modules)
                        .AddSingleton(ravenDbDocumentStore)
                        .AddSingleton(new Dispatcher(memoryEventSource.Subscribe))
                        .AddMvcCore().ConfigureMvcUsing(modules);
                    
                }).Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(o => o.MapControllers());
                });
            });

            using var host = await hostBuilder.StartAsync();

            Guid countryCode = Guid.NewGuid();
            string documentNumber = "123";
            string countryName = "Netherlands";
            string kind = "Filming";
            string newState = "Active";
            
            using var session = ravenDbDocumentStore.OpenAsyncSession();
            
            await session.StoreAsync(new CountryLookup
            {
                Id = $"CountryLookup/{countryCode}",
                Name = countryName
            });

            await session.StoreAsync(new DocumentCountProjection
            {
                Id = $"DocumentCountProjection/{documentNumber}",
                Country = countryCode,
                Kind = kind
            });

            await session.SaveChangesAsync();

            // Act
            await memoryEventSource.Write(new StateTransitionedEvent
            {
                DocumentNumber = documentNumber,
                State = newState
            });

            // Assert
            using var httpClient = host.GetTestClient();

            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(
                $"/statistics/metrics/CountsPerState?country={countryCode}&kind={kind}");

            string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);

            JToken jtokenElement = JToken.Parse(responseBody).Children().FirstOrDefault();

            Assert.NotNull(jtokenElement);
            Assert.Equal(countryCode.ToString(), jtokenElement.Value<string>("country"));
            Assert.Equal(countryName, jtokenElement.Value<string>("countryName"));
            Assert.Equal(kind, jtokenElement.Value<string>("kind"));
            Assert.Equal(newState, jtokenElement.Value<string>("state"));
            Assert.Equal(1, jtokenElement.Value<int>("count"));
        }
    }
}