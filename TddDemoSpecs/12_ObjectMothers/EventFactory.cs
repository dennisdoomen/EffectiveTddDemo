using System;
using System.Threading.Tasks;

namespace ExampleHost.TddDemoSpecs._12_ObjectMothers
{
    public class EventFactory
    {
        private readonly Func<object, Task> writeEvent;

        public EventFactory(Func<object, Task> writeEvent)
        {
            this.writeEvent = writeEvent;
        }

        public CountryBuilder Country(string name)
        {
            return new CountryBuilder(name, writeEvent);
        }

        public ContractBuilder Contract(string number)
        {
            return new ContractBuilder(number, writeEvent);
        }
    }
}