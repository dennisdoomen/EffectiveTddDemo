using System;
using System.Collections.Generic;

namespace DocumentManagement.Specs._05_TestDataBuilders
{
    public abstract class TestDataBuilder<T>
    {
        private readonly List<Action<T>> modifiers = new List<Action<T>>();

        public T Build()
        {
            T result = OnBuild();

            foreach (Action<T> modifier in modifiers)
            {
                modifier(result);
            }

            return result;
        }

        public TestDataBuilder<T> With(Action<T> modifier)
        {
            modifiers.Add(modifier);
            return this;
        }

        protected abstract T OnBuild();
    }
}