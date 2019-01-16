using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Chill;
using DocumentManagement.Events;
using DocumentManagement.Specs._05_TestDataBuilders;
using LiquidProjections;
using LiquidProjections.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Xunit;

namespace DocumentManagement.Specs._07_BDD_Chill
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
                    
                    IStartableModule module = null;
                    
                    var webHostBuilder = new WebHostBuilder().Configure(b =>
                    {
                        module = b.UseDocumentStatisticsModule(The<IDocumentStore>(), new Dispatcher(The<MemoryEventSource>().Subscribe));
                    });

                    UseThe(new TestServer(webHostBuilder));
                    UseThe(The<TestServer>().CreateClient());

                    await module.Start();
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
                HttpResponseMessage response = await The<HttpClient>().GetAsync(
                    $"http://localhost/Statistics/CountsPerState?country={countryCode}&kind=Filming");

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
}