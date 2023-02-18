using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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

namespace DocumentManagement.Specs._06_BDD;

public class When_a_document_is_activated : SpecificationContext, IDisposable
{
    private MemoryEventSource eventStore;
    private IDocumentStore documentStore;
    private Guid countryCode;
    private IHost host;

    protected override async Task EstablishContext()
    {
        eventStore = new MemoryEventSource();

        documentStore = new RavenDocumentStoreBuilder().Build();
                
        countryCode = Guid.NewGuid();

        using (var session = documentStore.OpenAsyncSession())
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
                
        host = new TestHostBuilder().Using(documentStore).Using(eventStore).WithModules(modules).Build();
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
        using var httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(
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

    public void Dispose()
    {
        documentStore.Dispose();
        host.Dispose();
    }
}