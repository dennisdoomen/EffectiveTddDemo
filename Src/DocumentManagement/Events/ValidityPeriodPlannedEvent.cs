using System;

namespace LiquidProjections.ExampleHost.Events
{
    public class ValidityPeriodPlannedEvent
    {
        public int Sequence { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}