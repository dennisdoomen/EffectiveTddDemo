using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using LiquidProjections.ExampleHost.Events;
using LiquidProjections.Testing;
using Microsoft.Owin.Builder;
using Newtonsoft.Json.Linq;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Xunit;

namespace ExampleHost.TddDemoSpecs._04_Constants
{
    public class StatisticsControllerSpecs
    {
        [Fact]
        public async Task When_a_document_is_activated_it_should_be_included_in_the_active_count()
        {
            //----------------------------------------------------------------------------------------------------
            // Arrange
            //----------------------------------------------------------------------------------------------------
            var eventStore = new MemoryEventSource();

            var ravenDb = new EmbeddableDocumentStore
            {
                RunInMemory = true,
                Configuration = {RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true},
                Conventions =
                {
#pragma warning disable 618
                    DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite
#pragma warning restore 618
                }
            }.Initialize();

            IndexCreation.CreateIndexes(typeof(Program).Assembly, ravenDb);

            var projector = new CountsProjector(new Dispatcher(eventStore.Subscribe),
                () => ravenDb.OpenAsyncSession());

            await projector.Start();

            Guid countryCode = Guid.NewGuid();

            using (var session = ravenDb.OpenAsyncSession())
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

            //----------------------------------------------------------------------------------------------------
            // Act
            //----------------------------------------------------------------------------------------------------
            await eventStore.Write(new StateTransitionedEvent
            {
                DocumentNumber = "123",
                State = "Active"
            });

            //----------------------------------------------------------------------------------------------------
            // Assert
            //----------------------------------------------------------------------------------------------------
            var appBuilder = new AppBuilder();
            appBuilder.UseStatistics(() => ravenDb.OpenAsyncSession());
            var appFunc = appBuilder.Build();

            var httpClient = new HttpClient(new OwinHttpMessageHandler(appFunc));

            HttpResponseMessage response = await httpClient.GetAsync(
                $"http://localhost/api/Statistics/CountsPerState?country={countryCode}&kind=Filming");

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