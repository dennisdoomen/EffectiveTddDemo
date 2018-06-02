using System.Collections.Generic;
using System.Threading.Tasks;
using LiquidProjections.ExampleHost;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;

namespace ExampleHost.TddDemoSpecs
{
    internal class RavenDbBuilder
    {
        private readonly EmbeddableDocumentStore store = new EmbeddableDocumentStore();
        private readonly List<object> documents = new List<object>();

        public RavenDbBuilder AsInMemory
        {
            get
            {
                store.RunInMemory = true;
                store.Configuration.RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true;
                return this;
            }
        }

        public RavenDbBuilder WithManagementStudio
        {
            get
            {
                store.UseEmbeddedHttpServer = true;
                store.Configuration.Port = 38080;
                return this;
            }
        }

        public RavenDbBuilder Containing(object document)
        {
            documents.Add(document);
            return this;
        }

        public async Task<IDocumentStore> Build()
        {
            store.Conventions = new DocumentConvention
            {
#pragma warning disable 618
                DefaultQueryingConsistency = ConsistencyOptions.AlwaysWaitForNonStaleResultsAsOfLastWrite,
#pragma warning restore 618
            };

            store.Initialize();

            IndexCreation.CreateIndexes(typeof(Program).Assembly, store);
            
            using (var session = store.OpenAsyncSession())
            {
                foreach (object document in documents)
                {
                    await session.StoreAsync(document);
                }

                await session.SaveChangesAsync();
            }

            return store;
        }
    }
}