using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBrix.Platform.Diagnostics.Eventing; //Was previously: Uno.Diagnostics.Eventing

public enum EventOpcode : byte
{
    Info,
    Start,
    Stop,
    DataCollectionStart,
    DataCollectionStop,
    Extension,
    Reply,
    Resume,
    Suspend,
    Send,
    Receive = 240
}