using System;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using DocumentManagement.Events;
using DocumentManagement.Modularization;
using DocumentManagement.Specs._05_TestDataBuilders;
using DocumentManagement.Statistics;
using FluentAssertions;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Xunit;

namespace DocumentManagement.Specs._09_BaseClass
{
    namespace StatisticsModuleSpecs
    {
        public class Given_a_http_controller_talking_to_an_in_memory_event_store : GivenWhenThen
        {
            protected Given_a_http_controller_talking_to_an_in_memory_event_store()
            {
                Given(() =>
                {
                    UseThe(new MemoryEventSource());

                    SetThe<IDocumentStore>().To(new RavenDocumentStoreBuilder().Build());

                    var modules = new ModuleRegistry(new StatisticsModule());
                
                    var host = new TestHostBuilder()
                        .Using(The<IDocumentStore>())
                        .Using(The<MemoryEventSource>())
                        .WithModules(modules)
                        .Build();

                    UseThe(host);
                });
            }

            protected async Task Write(object @event)
            {
                await The<MemoryEventSource>().Write(@event);
            }
        }

        public class When_a_document_is_activated : Given_a_http_controller_talking_to_an_in_memory_event_store
        {
            readonly Guid countryCode = Guid.NewGuid();

            public When_a_document_is_activated()
            {
                Given(async () =>
                {
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
                    await Write(new StateTransitionedEvent
                    {
                        DocumentNumber = "123",
                        State = "Active"
                    });
                });
            }

            [Fact]
            public async Task Then_it_should_be_included_in_the_active_count()
            {
                var host = The<IHost>().GetTestClient();
                
                HttpResponseMessage response = await host.GetAsync(
                    $"http://localhost/statistics/metrics/CountsPerState?country={countryCode}&kind=Filming");

                string body = await response.Content.ReadAsStringAsync();

                var expectation = new[]
                {
                    new
                    {
                        Country = countryCode.ToString(),
                        CountryName = "Netherlands",
                        Kind = "Filming",
                        State = "Active",
                        Count = 1
                    }
                };

                object counters = JsonConvert.DeserializeAnonymousType(body, expectation);

                counters.Should().BeEquivalentTo(expectation);
            }
        }
    }
}