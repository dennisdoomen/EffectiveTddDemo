using System;
using LiquidProjections.ExampleHost;

namespace ExampleHost.TddDemoSpecs._05_TestDataBuilders
{
    public class CountryLookupBuilder
    {
        private Guid code = Guid.NewGuid();
        private string name = Guid.NewGuid().ToString();

        public CountryLookupBuilder IdentifiedBy(Guid countryCode)
        {
            this.code = countryCode;
            return this;
        }

        public CountryLookupBuilder Named(string name)
        {
            this.name = name;
            return this;
        }

        public CountryLookup Build()
        {
            return new CountryLookup
            {
                Id = $"CountryLookup/{code.ToString()}",
                Name = name
            };
        }
    }
}