using System;
using System.Threading.Tasks;
using DocumentManagement.Events;

namespace DocumentManagement.Specs._12_ObjectMothers
{
    public class CountryBuilder
    {
        private readonly string name = "SomeCountry";
        private readonly Func<object, Task> writeEvent;

        public CountryBuilder(Func<object, Task> writeEvent)
        {
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