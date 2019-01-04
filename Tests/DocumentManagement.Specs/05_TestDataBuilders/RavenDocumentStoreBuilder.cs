using System;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;

namespace DocumentManagement.Specs._05_TestDataBuilders
{
    public class RavenDocumentStoreBuilder
    {
        public IDocumentStore Build()
        {
            var testDriver = InMemoryRavenTestDriver.Instance;
            var ravenDbDocumentStore = testDriver.GetDocumentStore();
            {
                IndexCreation.CreateIndexes(typeof(CountsProjector).Assembly, ravenDbDocumentStore);

                return ravenDbDocumentStore;
            }
        }
    }
}