using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DocumentManagement.Modularization;
using LiquidProjections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;

namespace DocumentManagement.Statistics
{
    public class StatisticsModule : IModule
    {
        public BaseRoute BaseRoute { get; } = new("statistics");
        
        public IEnumerable<ServiceDescriptor> Dependencies { get; } = new[]
        {
            ServiceDescriptor.Singleton<GetRavenSession>(sp => () => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession())
        };

        public IEnumerable<Type> ControllerTypes { get; } = new[]
        {
            typeof(MetricsController)
        };

        public IEnumerable<ServiceDescriptor> HostedServices { get; } = new[]
        {
            ServiceDescriptor.Transient<IHostedService, StatisticsHostedService>(),
        };
    }

    public class StatisticsHostedService : IHostedService
    {
        private readonly CountsProjector projector;
       
        public StatisticsHostedService(IDocumentStore ravenDatabase, Dispatcher eventDispatcher)
        {
            projector = new CountsProjector(eventDispatcher, ravenDatabase.OpenAsyncSession);
                
            new Documents_ByDynamicState().Execute(ravenDatabase);
            new Documents_CountsByStaticState().Execute(ravenDatabase);
        }

        public  Task StartAsync(CancellationToken cancellationToken)
        {
            return projector.Start();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            projector.Dispose();

            return Task.CompletedTask;
        }
    }
}