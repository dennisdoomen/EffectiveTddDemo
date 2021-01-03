using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentManagement.Events;
using DocumentManagement.Modularization;
using DocumentManagement.Statistics;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DocumentManagement.Specs._05_TestDataBuilders
{
    public class StatisticsModuleSpecs
    {
        [Fact]
        public async Task When_a_document_is_activated_it_should_be_included_in_the_active_count()
        {
            // Arrange
            var eventStore = new MemoryEventSource();

            using var documentStore = new RavenDocumentStoreBuilder().Build();
            Guid countryCode = Guid.NewGuid();

            using (var session = documentStore.OpenAsyncSession())
            {
                await session.StoreAsync(new CountryLookupBuilder()
                    .IdentifiedBy(countryCode)
                    .Named("Netherlands")
                    .Build());

                await session.StoreAsync(new DocumentCountProjectionBuilder()
                    .WithNumber("123")
                    .InCountry(countryCode)
                    .OfKind("Filming")
                    .Build());

                await session.SaveChangesAsync();
            }
            
            var modules = new ModuleRegistry(new StatisticsModule());

            var host = new TestHostBuilder().Using(documentStore).Using(eventStore).WithModules(modules).Build();
            
            // Act
            await eventStore.Write(new StateTransitionedEvent
            {
                DocumentNumber = "123",
                State = "Active"
            });
                    
            // Assert
            using var httpClient = host.GetTestClient();
            
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(
                $"/statistics/metrics/CountsPerState?country={countryCode}&kind=Filming");

            string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);

            JToken jtokenElement = JToken.Parse(responseBody).Children().FirstOrDefault();

            Assert.NotNull(jtokenElement);
            Assert.Equal(countryCode.ToString(), jtokenElement.Value<string>("country"));
            Assert.Equal("Netherlands", jtokenElement.Value<string>("countryName"));
            Assert.Equal("Filming", jtokenElement.Value<string>("kind"));
            Assert.Equal("Active", jtokenElement.Value<string>("state"));
            Assert.Equal(1, jtokenElement.Value<int>("count"));
        }
    }
}