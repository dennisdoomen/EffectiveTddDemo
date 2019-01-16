namespace DocumentManagement.Events
{
    internal class StateRevertedEvent
    {
        public string DocumentNumber { get; set; }
        public string State { get; set; }
    }
}