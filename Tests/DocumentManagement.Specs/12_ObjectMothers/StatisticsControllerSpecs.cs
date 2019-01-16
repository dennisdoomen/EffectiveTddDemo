using System;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using DocumentManagement.Specs._05_TestDataBuilders;
using DocumentManagement.Statistics;
using ExampleHost.TddDemoSpecs._12_ObjectMothers;
using FluentAssertions;
using FluentAssertions.Extensions;
using LiquidProjections;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Xunit;

namespace DocumentManagement.Specs._12_ObjectMothers
{
    namespace StatisticsControllerSpecs
    {
        public class Given_a_raven_projector_with_an_in_memory_event_source : GivenWhenThen
        {
            protected Given_a_raven_projector_with_an_in_memory_event_source()
            {
                Given(async () =>
                {
                    UseThe(new MemoryEventSource());

                    SetThe<IDocumentStore>().To(new RavenDocumentStoreBuilder().Build());

                    IStartableModule module = null;

                    var webHostBuilder = new WebHostBuilder().Configure(b =>
                    {
                        module = b.UseDocumentStatisticsModule(The<IDocumentStore>(),
                            new Dispatcher(The<MemoryEventSource>().Subscribe));
                    });

                    UseThe(new TestServer(webHostBuilder));
                    UseThe(The<TestServer>().CreateClient());

                    await module.Start();
                });
            }

            protected EventFactory The => A;

            protected EventFactory A => new EventFactory(async @event =>
            {
                await The<MemoryEventSource>().Write(@event);
            });

        }

        public class When_a_contract_is_active : Given_a_raven_projector_with_an_in_memory_event_source
        {
            readonly Guid countryCode = Guid.NewGuid();

            public When_a_contract_is_active()
            {
                Given(async () =>
                {
                    await A.Country("Netherlands").Was.RegisteredAs(countryCode);
                    await A.Contract("123").OfKind("Filming").InCountry(countryCode).Was.Negotiated();
                    await The.Contract("123").Was.ApprovedForThePeriod(1.January(2016), DateTime.Now.Add(1.Days()));
                });

                When(async () =>
                {
                    await The.Contract("123").Is.TransitionedTo("Active");
                });
            }

            [Fact]
            public async Task Then_it_should_be_included_in_the_active_count()
            {
                HttpResponseMessage response = await The<HttpClient>().GetAsync(
                    $"http://localhost/Statistics/CountsPerState?country={countryCode}&kind=Filming");

                string body = await response.Content.ReadAsStringAsync();

                var expectation = new[]
                {
                    new
                    {
                        State = "Active",
                        Count = 1
                    }
                };

                object counters = JsonConvert.DeserializeAnonymousType(body, expectation);

                counters.Should().BeEquivalentTo(expectation);            }
        }
    }
}