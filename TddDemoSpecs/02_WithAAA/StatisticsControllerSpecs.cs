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

namespace ExampleHost.TddDemoSpecs._02_WithAAA
{
    public class StatisticsControllerSpecs
    {
        [Fact]
        public async Task When_a_StateTransitionedEvent_is_applied_to_a_DocumentCountProjection_the_controller_should_return_1()
        {
            //----------------------------------------------------------------------------------------------------
            // Arrange
            //----------------------------------------------------------------------------------------------------
            var memoryEventSource = new MemoryEventSource();

            var ravenDbDocumentStore = new EmbeddableDocumentStore
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

            IndexCreation.CreateIndexes(typeof(Program).Assembly, ravenDbDocumentStore);

            var countsProjector = new CountsProjector(new Dispatcher(memoryEventSource.Subscribe),
                () => ravenDbDocumentStore.OpenAsyncSession());

            await countsProjector.Start();

            Guid countryCode = Guid.NewGuid();
            string documentNumber = "123";
            string countryName = "Netherlands";
            string kind = "Filming";
            string newState = "Active";

            using (var session = ravenDbDocumentStore.OpenAsyncSession())
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

            //----------------------------------------------------------------------------------------------------
            // Act
            //----------------------------------------------------------------------------------------------------
            await memoryEventSource.Write(new StateTransitionedEvent
            {
                DocumentNumber = documentNumber,
                State = newState
            });

            //----------------------------------------------------------------------------------------------------
            // Assert
            //----------------------------------------------------------------------------------------------------
            var appBuilder = new AppBuilder();
            appBuilder.UseStatistics(() => ravenDbDocumentStore.OpenAsyncSession());
            var appFunc = appBuilder.Build();

            var httpClient = new HttpClient(new OwinHttpMessageHandler(appFunc));

            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(
                $"http://localhost/api/Statistics/CountsPerState?country={countryCode}&kind={kind}");

            string responseBody = await httpResponseMessage.Content.ReadAsStringAsync();

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