﻿namespace DocumentManagement.Events
{
    public class StateTransitionedEvent
    {
        public string State { get; set; }
        public string DocumentNumber { get; set; }
    }
}