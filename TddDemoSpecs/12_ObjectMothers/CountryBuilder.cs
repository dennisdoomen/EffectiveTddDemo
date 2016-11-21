using System;
using System.Threading.Tasks;
using LiquidProjections.ExampleHost.Events;

namespace ExampleHost.TddDemoSpecs._12_ObjectMothers
{
    public class CountryBuilder
    {
        private readonly string name;
        private readonly Func<object, Task> writeEvent;

        public CountryBuilder(string name, Func<object, Task> writeEvent)
        {
            this.name = name;
            this.writeEvent = writeEvent;
        }

        public CountryBuilder Was => this;

        public Task RegisteredAs(Guid code)
        {
            return writeEvent(new CountryRegisteredEvent
            {
                Code = code.ToString(),
                Name = name
            });
        }
    }
}