using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using ExampleHost.TddDemoSpecs._05_TestDataBuilders;
using FluentAssertions;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using LiquidProjections.ExampleHost.Events;
using Microsoft.Owin.Builder;
using Newtonsoft.Json.Linq;
using Raven.Client;
using Xunit;

namespace ExampleHost.TddDemoSpecs._08_Assertions
{
    namespace StatisticsControllerSpecs
    {
        public class When_a_document_is_activated : GivenWhenThen
        {
            private Guid countryCode = Guid.NewGuid();

            public When_a_document_is_activated()
            {
                Given(async () =>
                {
                    UseThe(new MemoryEventSource());

                    UseThe(await new RavenDbBuilder().AsInMemory.Build());

                    var projector = new CountsProjector(new Dispatcher(The<MemoryEventSource>()),
                        () => The<IDocumentStore>().OpenAsyncSession());

                    await projector.Start();

                    using (var session = The<IDocumentStore>().OpenAsyncSession())
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
                });

                When(async () =>
                {
                    await The<MemoryEventSource>().Write(new StateTransitionedEvent
                    {
                        DocumentNumber = "123",
                        State = "Active"
                    });
                });
            }

            [Fact]
            public async Task It_should_be_included_in_the_active_count()
            {
                var appBuilder = new AppBuilder();
                appBuilder.UseStatistics(() => The<IDocumentStore>().OpenAsyncSession());
                var appFunc = appBuilder.Build();

                var httpClient = new HttpClient(new OwinHttpMessageHandler(appFunc));

                HttpResponseMessage response = await httpClient.GetAsync(
                    $"http://localhost/api/Statistics/CountsPerState?country={countryCode}&kind=Filming");

                string body = await response.Content.ReadAsStringAsync();

                JToken counterElement = JToken.Parse(body).Children().FirstOrDefault();

                counterElement.Should().NotBeNull();
                counterElement.Value<string>("Country").Should().Be(countryCode.ToString());
                counterElement.Value<string>("CountryName").Should().Be("Netherlands");
                counterElement.Value<string>("Kind").Should().Be("Filming");
                counterElement.Value<string>("State").Should().Be("Active");
                counterElement.Value<int>("Count").Should().Be(1);
            }
        }
    }
}