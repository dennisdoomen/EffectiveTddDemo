using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using ExampleHost.TddDemoSpecs._12_ObjectMothers;
using FluentAssertions;
using FluentAssertions.Json;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using LiquidProjections.ExampleHost.Events;
using Microsoft.Owin.Builder;
using Newtonsoft.Json.Linq;
using Raven.Client;
using Xunit;

namespace ExampleHost.TddDemoSpecs._11_InitialEvents
{
    namespace StatisticsControllerSpecs
    {
        public class Given_a_raven_projector_with_an_in_memory_event_source : GivenWhenThen
        {
            public Given_a_raven_projector_with_an_in_memory_event_source()
            {
                Given(async () =>
                {
                    UseThe(new MemoryEventSource());
                    UseThe(await new RavenDbBuilder().AsInMemory.Build());

                    var projector = new CountsProjector(new Dispatcher(The<MemoryEventSource>()),
                        () => The<IDocumentStore>().OpenAsyncSession());

                    await projector.Start();

                    var appBuilder = new AppBuilder();
                    appBuilder.UseStatistics(() => The<IDocumentStore>().OpenAsyncSession());
                    var appFunc = appBuilder.Build();

                    UseThe(new HttpClient(new OwinHttpMessageHandler(appFunc)));
                });
            }

            protected async Task Write(object @event)
            {
                await The<MemoryEventSource>().Write(@event);
            }
        }

        public class When_a_contract_is_active : Given_a_raven_projector_with_an_in_memory_event_source
        {
            readonly Guid countryCode = Guid.NewGuid();

            public When_a_contract_is_active()
            {
                Given(async () =>
                {
                    await Write(new CountryRegisteredEvent
                    {
                        Code = countryCode.ToString(),
                        Name = "Netherlands"
                    });

                    await Write(new ContractNegotiatedEvent
                    {
                        Number = "123",
                        Country = countryCode,
                        InitialState = "Draft",
                        Kind = "Filming"
                    });

                    await Write(new ValidityPeriodPlannedEvent
                    {
                        DocumentNumber = "123",
                        From = 1.January(2016),
                        To = 1.January(2017),
                        Sequence = 1
                    });

                    await Write(new ValidityPeriodApprovedEvent
                    {
                        DocumentNumber = "123",
                        Sequence = 1
                    });
                });

                When(async () =>
                {
                    await Write(new StateTransitionedEvent
                    {
                        DocumentNumber = "123",
                        State = "Active"
                    });
                });
            }

            [Fact]
            public async Task It_should_count_that_contract_as_a_live_document()
            {
                HttpResponseMessage response = await The<HttpClient>().GetAsync(
                    $"http://localhost/api/Statistics/CountsPerState?country={countryCode}&kind=Filming");

                string body = await response.Content.ReadAsStringAsync();

                JToken element = JToken.Parse(body).Children().FirstOrDefault();

                element.Should().NotBeNull();
                element.Value<string>("Country").Should().Be(countryCode.ToString());
                element.Value<string>("CountryName").Should().Be("Netherlands");
                element.Value<string>("Kind").Should().Be("Filming");
                element.Value<string>("State").Should().Be("Active");
                element.Value<int>("Count").Should().Be(1);
            }
        }
    }
}