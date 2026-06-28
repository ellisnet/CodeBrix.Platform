using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBrix.Platform.Diagnostics.Eventing; //Was previously: Uno.Diagnostics.Eventing

/// <summary>
/// Identifies a tracing event activity.
/// </summary>
public class EventActivity
{
    public EventActivity(long activityId)
    {
        Id = activityId;
    }

    public long Id { get; }
}