using System;
using LiquidProjections.ExampleHost;

namespace ExampleHost.TddDemoSpecs._05_TestDataBuilders
{
    public class DocumentCountProjectionBuilder
    {
        private string number = "";
        private Guid countryCode;
        private static int nextNumber = 1;
        private string kind = "SomeKind";

        public DocumentCountProjectionBuilder WithNumber(string number)
        {
            this.number = number;
            return this;
        }

        public DocumentCountProjectionBuilder InCountry(Guid countryCode)
        {
            this.countryCode = countryCode;
            return this;
        }

        public DocumentCountProjection Build()
        {
            string id = (number.Length > 0) ? number : (++nextNumber).ToString();
            return new DocumentCountProjection
            {
                Id = $"DocumentCountProjection/{id}",
                Country = countryCode,
                Kind = kind
            };
        }

        public DocumentCountProjectionBuilder OfKind(string kind)
        {
            this.kind = kind;
            return this;
        }
    }
}