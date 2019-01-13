using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentManagement.Events;
using DocumentManagement.Statistics;
using LiquidProjections;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;
using Xunit;

namespace DocumentManagement.Specs._04_Constants
{
    public class StatisticsControllerSpecs
    {
        [Fact]
        public async Task When_a_document_is_activated_it_should_be_included_in_the_active_count()
        {
            // Arrange
            var memoryEventSource = new MemoryEventSource();

            using (IDocumentStore ravenDbDocumentStore = InMemoryRavenTestDriver.Instance.GetDocumentStore())
            {
                Guid countryCode = Guid.NewGuid();

                using (var session = ravenDbDocumentStore.OpenAsyncSession())
                {
                    await session.StoreAsync(new CountryLookup
                    {
                        Id = $"CountryLookup/{countryCode}",
                        Name = "Netherlands"
                    });

                    await session.StoreAsync(new DocumentCountProjection
                    {
                        Id = "DocumentCountProjection/123",
                        Country = countryCode,
                        Kind = "Filming"
                    });

                    await session.SaveChangesAsync();
                }

                IStartableModule module = null;

                var webHostBuilder = new WebHostBuilder().Configure(builder =>
                {
                    module = builder.UseDocumentStatisticsModule(ravenDbDocumentStore, new Dispatcher(memoryEventSource.Subscribe));
                });

                using (var testServer = new TestServer(webHostBuilder))
                using (var httpClient = testServer.CreateClient())
                {
                    await module.Start();

                    // Act
                    await memoryEventSource.Write(new StateTransitionedEvent
                    {
                        DocumentNumber = "123",
                        State = "Active"
                    });
                    
                    // Assert
                    HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(
                        $"/statistics/CountsPerState?country={countryCode}&kind=Filming");

                    string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
                    Assert.Equal(HttpStatusCode.OK, httpResponseMessage.StatusCode);

                    JToken jtokenElement = JToken.Parse(responseBody).Children().FirstOrDefault();

                    Assert.NotNull(jtokenElement);
                    Assert.Equal(countryCode.ToString(), jtokenElement.Value<string>("Country"));
                    Assert.Equal("Netherlands", jtokenElement.Value<string>("CountryName"));
                    Assert.Equal("Filming", jtokenElement.Value<string>("Kind"));
                    Assert.Equal("Active", jtokenElement.Value<string>("State"));
                    Assert.Equal(1, jtokenElement.Value<int>("Count"));
                }
            }
        }
    }
}