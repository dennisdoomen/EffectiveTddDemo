using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentManagement.Specs._05_TestDataBuilders;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using LiquidProjections.ExampleHost.Events;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;
using Xunit;

namespace DocumentManagement.Specs._03_BetterNaming
{
    public class StatisticsControllerSpecs
    {
        [Fact]
        public async Task When_a_document_is_activated_it_should_be_included_in_the_active_count()
        {
            // Arrange
            var eventSource = new MemoryEventSource();

            using (var documentStore = InMemoryRavenTestDriver.Instance.GetDocumentStore())
            {
                IndexCreation.CreateIndexes(typeof(CountsProjector).Assembly, documentStore);

                var countsProjector = new CountsProjector(new Dispatcher(eventSource.Subscribe),
                    () => documentStore.OpenAsyncSession());

                await countsProjector.Start();

                Guid countryCode = Guid.NewGuid();
                string documentNumber = "123";
                string countryName = "Netherlands";
                string kind = "Filming";
                string newState = "Active";

                using (var session = documentStore.OpenAsyncSession())
                {
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
                }

                // Act
                await eventSource.Write(new StateTransitionedEvent
                {
                    DocumentNumber = documentNumber,
                    State = newState
                });

                // Assert
                var webHostBuilder = new WebHostBuilder()
                    .Configure(b => b.UseStatistics(documentStore.OpenAsyncSession));

                using (var testServer = new TestServer(webHostBuilder))
                using (var httpClient = testServer.CreateClient())
                {
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(
                        $"/statistics/CountsPerState?country={countryCode}&kind={kind}");

                    string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
                    Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);

                    JToken jtokenElement = JToken.Parse(responseBody).Children().FirstOrDefault();

                    Assert.NotNull(jtokenElement);
                    Assert.Equal(countryCode.ToString(), jtokenElement.Value<string>("Country"));
                    Assert.Equal(countryName, jtokenElement.Value<string>("CountryName"));
                    Assert.Equal(kind, jtokenElement.Value<string>("Kind"));
                    Assert.Equal(newState, jtokenElement.Value<string>("State"));
                    Assert.Equal(1, jtokenElement.Value<int>("Count"));
                }
            }
        }
    }

}