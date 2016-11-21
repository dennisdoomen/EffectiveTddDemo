using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiquidProjections.Testing
{
    public class DispatcherSpy : IDispatcher
    {
        private readonly IDispatcher inner;

        public DispatcherSpy(IDispatcher inner)
        {
            this.inner = inner;
        }

        public Action<IReadOnlyList<Transaction>> OnDispatched { get; set; }

        public IDisposable Subscribe(long? checkpoint, Func<IReadOnlyList<Transaction>, Task> handler)
        {
            return inner.Subscribe(checkpoint, async transactions =>
            {
                //Dispatching(transactions);

                await handler(transactions);

                OnDispatched(transactions);
            });
        }
    }
}