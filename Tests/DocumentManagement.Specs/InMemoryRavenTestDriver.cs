using System;
using System.Threading;
using Raven.Client.Documents;
using Raven.TestDriver;

namespace DocumentManagement.Specs
{
    internal class InMemoryRavenTestDriver : RavenTestDriver
    {
        private static readonly Lazy<InMemoryRavenTestDriver> instance =
            new(() =>
            {
                ConfigureServer(new TestServerOptions
                {
                    FrameworkVersion = null
                });

                return new InMemoryRavenTestDriver();
            }, LazyThreadSafetyMode.ExecutionAndPublication);

        private InMemoryRavenTestDriver()
        {
            
        }
        
        

        public IDocumentStore GetDocumentStore()
        {
            return base.GetDocumentStore();
        }

        public static InMemoryRavenTestDriver Instance => instance.Value;
    }
}