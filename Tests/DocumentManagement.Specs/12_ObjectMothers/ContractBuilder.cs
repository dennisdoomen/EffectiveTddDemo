using System;
using System.Threading.Tasks;
using DocumentManagement.Events;

namespace ExampleHost.TddDemoSpecs._12_ObjectMothers
{
    public class ContractBuilder
    {
        private readonly string number = Guid.NewGuid().ToString();
        private readonly Func<object, Task> writeEvent;
        private string kind = "SomeKind";
        private Guid countryCode;
        private static int lastPeriodSequence;

        public ContractBuilder(string number, Func<object, Task> writeEvent)
        {
            this.number = number;
            this.writeEvent = writeEvent;
        }

        public ContractBuilder Was => this;
        public ContractBuilder Is => this;

        public ContractBuilder OfKind(string kind)
        {
            this.kind = kind;
            return this;
        }

        public ContractBuilder InCountry(Guid countryCode)
        {
            this.countryCode = countryCode;
            return this;
        }

        public Task Negotiated()
        {
            return writeEvent(new ContractNegotiatedEvent
            {
                Number = number,
                Country = countryCode,
                InitialState = "Draft",
                Kind = kind
            });
        }

        public async Task ApprovedForThePeriod(DateTime from, DateTime to)
        {
            int sequence = ++lastPeriodSequence;

            await writeEvent(new ValidityPeriodPlannedEvent
            {
                DocumentNumber = number,
                From = from,
                To = to,
                Sequence = sequence
            });

            await writeEvent(new ValidityPeriodApprovedEvent
            {
                DocumentNumber = number,
                Sequence = sequence
            });
        }

        public Task TransitionedTo(string newState)
        {
            return writeEvent(new StateTransitionedEvent
            {
                DocumentNumber = number,
                State = newState
            });
        }
    }
}