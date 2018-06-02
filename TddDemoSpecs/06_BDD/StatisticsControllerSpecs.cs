﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ExampleHost.TddDemoSpecs._05_TestDataBuilders;
using LiquidProjections;
using LiquidProjections.ExampleHost;
using LiquidProjections.ExampleHost.Events;
using LiquidProjections.Testing;
using Microsoft.Owin.Builder;
using Newtonsoft.Json.Linq;
using Raven.Client;
using Xunit;

namespace ExampleHost.TddDemoSpecs._06_BDD
{
    namespace StatisticsControllerSpecs
    {
        public class When_a_document_is_activated : SpecificationContext
        {
            private MemoryEventSource eventStore;
            private IDocumentStore ravenDb;
            private Guid countryCode = Guid.NewGuid();

            protected override async Task EstablishContext()
            {
                eventStore = new MemoryEventSource();

                ravenDb = await new RavenDbBuilder().AsInMemory.Build();

                var projector = new CountsProjector(new Dispatcher(eventStore.Subscribe),
                    () => ravenDb.OpenAsyncSession());

                await projector.Start();

                using (var session = ravenDb.OpenAsyncSession())
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
            }

            protected override async Task Because()
            {
                await eventStore.Write(new StateTransitionedEvent
                {
                    DocumentNumber = "123",
                    State = "Active"
                });
            }

            [Fact]
            public async Task It_should_be_included_in_the_active_count()
            {
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
}