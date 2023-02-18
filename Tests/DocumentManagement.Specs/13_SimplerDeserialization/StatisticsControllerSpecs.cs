using System;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using DocumentManagement.Modularization;
using DocumentManagement.Specs._05_TestDataBuilders;
using DocumentManagement.Specs._12_ObjectMothers;
using DocumentManagement.Statistics;
using FluentAssertions.Extensions;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Xunit;

namespace DocumentManagement.Specs._13_SimplerDeserialization
{
    namespace StatisticsControllerSpecs
    {
        public class Given_a_raven_projector_with_an_in_memory_event_source : GivenWhenThen
        {
            protected Given_a_raven_projector_with_an_in_memory_event_source()
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
                    return Task.CompletedTask;
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
                    await A.Country().Was.RegisteredAs(countryCode);
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
                var host = The<IHost>().GetTestClient();
                
                HttpResponseMessage response = await host.GetAsync(
                    $"http://localhost/statistics/metrics/CountsPerState?country={countryCode}&kind=Filming");
                
                await response.Should().BeEquivalentTo(new[]
                {
                    new
                    {
                        State = "Active",
                        Count = 1
                    }
                });
            }
        }
    }
}