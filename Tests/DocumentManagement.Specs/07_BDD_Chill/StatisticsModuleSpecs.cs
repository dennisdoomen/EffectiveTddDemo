using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using DocumentManagement.Events;
using DocumentManagement.Modularization;
using DocumentManagement.Specs._05_TestDataBuilders;
using DocumentManagement.Statistics;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Xunit;

namespace DocumentManagement.Specs._07_BDD_Chill
{
    namespace StatisticsModuleSpecs
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
                    
                    var modules = new ModuleRegistry(new StatisticsModule());
                
                    var host = new TestHostBuilder()
                        .Using(The<IDocumentStore>())
                        .Using(The<MemoryEventSource>())
                        .WithModules(modules)
                        .Build();

                    UseThe(host);
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
                var host = The<IHost>().GetTestClient();
                
                HttpResponseMessage response = await host.GetAsync(
                    $"http://localhost/statistics/metrics/CountsPerState?country={countryCode}&kind=Filming");

                string body = await response.Content.ReadAsStringAsync();

                JToken counterElement = JToken.Parse(body).Children().FirstOrDefault();

                Assert.NotNull(counterElement);
                Assert.Equal(countryCode.ToString(), counterElement.Value<string>("country"));
                Assert.Equal("Netherlands", counterElement.Value<string>("countryName"));
                Assert.Equal("Filming", counterElement.Value<string>("kind"));
                Assert.Equal("Active", counterElement.Value<string>("state"));
                Assert.Equal(1, counterElement.Value<int>("count"));
            }
        }
    }
}