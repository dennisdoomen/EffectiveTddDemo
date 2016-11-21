using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiquidProjections
{
    public interface IDispatcher
    {
        IDisposable Subscribe(long? checkpoint, Func<IReadOnlyList<Transaction>, Task> handler);
    }
}