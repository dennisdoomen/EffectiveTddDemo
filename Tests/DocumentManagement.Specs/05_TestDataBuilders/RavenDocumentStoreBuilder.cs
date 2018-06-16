using System;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.TestDriver;

namespace DocumentManagement.Specs._05_TestDataBuilders
{
    public class RavenDocumentStoreBuilder
    {
        public IDocumentStore Build()
        {
            var ravenDriver = new InMemoryRavenTestDriver();
            var ravenDbDocumentStore = ravenDriver.GetDocumentStore();
            {
                IndexCreation.CreateIndexes(typeof(CountsProjector).Assembly, ravenDbDocumentStore);

                ravenDbDocumentStore.AfterDispose += delegate(object sender, EventArgs args)
                {
                    ravenDriver.Dispose();
                };

                return ravenDbDocumentStore;
            }
        }

        private class InMemoryRavenTestDriver : RavenTestDriver
        {
            public IDocumentStore GetDocumentStore()
            {
                return base.GetDocumentStore();
            }
        }
    }
}