using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using DocumentManagement.Specs._05_TestDataBuilders;
using FluentAssertions;
using LiquidProjections;
using LiquidProjections.ExampleHost.Events;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq.Indexing;
using Xunit;

namespace DocumentManagement.Specs._08_Assertions
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

                    SetThe<IDocumentStore>().To(new RavenDocumentStoreBuilder().Build());

                    var projector = new CountsProjector(new Dispatcher(The<MemoryEventSource>().Subscribe),
                        () => The<IDocumentStore>().OpenAsyncSession());

                    await projector.Start();

                    countryCode = Guid.NewGuid();

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
            public async Task Then_it_should_be_included_in_the_active_count()
            {
                var webHostBuilder = new WebHostBuilder()
                    .Configure(b => b.UseStatistics(The<IDocumentStore>().OpenAsyncSession));

                using (var testServer = new TestServer(webHostBuilder))
                using (var httpClient = testServer.CreateClient())
                {
                    HttpResponseMessage response = await httpClient.GetAsync(
                        $"http://localhost/Statistics/CountsPerState?country={countryCode}&kind=Filming");

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
}