namespace DocumentManagement.Events
{
    internal class AreaRestrictedEvent
    {
        public string DocumentNumber { get; set; }
        public string Area { get; set; }
    }
}