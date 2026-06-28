using System;
using UnityEngine;

namespace AjoyGames.MotionKit.Events
{
    /// <summary>
    /// A single recorded event occurrence. The universal event system records the <i>invocation</i> of an event
    /// (not its result); during playback the matching <see cref="SDK.IRecordedEventHandler"/> re-fires it at
    /// <see cref="Time"/>. A small fixed payload covers the common cases without per-event allocations.
    /// </summary>
    [Serializable]
    public struct RecordedEvent
    {
        /// <summary>Session-relative time of the invocation, in seconds.</summary>
        [SerializeField] public float Time;

        /// <summary>Identifier of the handler that should re-fire this event (e.g. "unityEvent", "animatorParam").</summary>
        [SerializeField] public string HandlerId;

        /// <summary>Handler-specific key: the named slot, animator parameter name, command name, etc.</summary>
        [SerializeField] public string Key;

        /// <summary>Generic float payload (e.g. animator float, volume, pitch).</summary>
        [SerializeField] public float FloatPayload;

        /// <summary>Generic int payload (e.g. animator int / bool-as-int, enum value).</summary>
        [SerializeField] public int IntPayload;

        /// <summary>Generic string payload (e.g. command argument, clip name).</summary>
        [SerializeField] public string StringPayload;
    }
}
