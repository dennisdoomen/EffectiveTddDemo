using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LiquidProjections;
using LiquidProjections.ExampleHost.Events;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;
using Xunit;

namespace DocumentManagement.Specs._05_TestDataBuilders
{
    public class StatisticsControllerSpecs
    {
        [Fact]
        public async Task When_a_document_is_activated_it_should_be_included_in_the_active_count()
        {
            // Arrange
            var eventStore = new MemoryEventSource();

            using (var documentStore = new RavenDocumentStoreBuilder().Build())
            {
                var projector = new CountsProjector(new Dispatcher(eventStore.Subscribe),
                    () => documentStore.OpenAsyncSession());

                await projector.Start();

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

                // Act
                await eventStore.Write(new StateTransitionedEvent
                {
                    DocumentNumber = "123",
                    State = "Active"
                });

                // Assert
                var webHostBuilder = new WebHostBuilder()
                    .Configure(b => b.UseStatistics(documentStore.OpenAsyncSession));

                using (var testServer = new TestServer(webHostBuilder))
                using (var httpClient = testServer.CreateClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(
                        $"http://localhost/Statistics/CountsPerState?country={countryCode}&kind=Filming");

                    string body = await response.Content.ReadAsStringAsync();

                    JToken counterElement = JToken.Parse(body).Children().FirstOrDefault();

                    Assert.NotNull(counterElement);
                    Assert.Equal(countryCode.ToString(), counterElement.Value<string>("Country"));
                    Assert.Equal("Netherlands", counterElement.Value<string>("CountryName"));
                    Assert.Equal("Filming", counterElement.Value<string>("Kind"));
                    Assert.Equal("Active", counterElement.Value<string>("State"));
                    Assert.Equal(1, counterElement.Value<int>("Count"));
                }
            }
        }
        
    }
}