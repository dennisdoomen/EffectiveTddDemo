namespace LiquidProjections.ExampleHost.Events
{
    public class ValidityPeriodApprovedEvent
    {
        public string DocumentNumber { get; set; }
        public int Sequence { get; set; }
    }
}