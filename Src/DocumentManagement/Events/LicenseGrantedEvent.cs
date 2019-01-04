﻿using System;

namespace LiquidProjections.ExampleHost.Events
{
    public class LicenseGrantedEvent
    {
        public string Number { get; set; }
        public string Kind { get; set; }
        public Guid Country { get; set; }
        public string InitialState { get; set; }
    }
}