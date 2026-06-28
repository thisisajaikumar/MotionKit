using UnityEngine;
using AjoyGames.MotionKit.Events;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Re-fires recorded events of a given <see cref="HandlerId"/> during playback. Handlers are the
    /// extensibility point of the universal event system: UnityEvents, C# delegates, animator parameters,
    /// timeline signals, audio events and custom commands are all implemented as handlers and discovered
    /// (reflection-free) via the generated registry.
    /// </summary>
    public interface IRecordedEventHandler
    {
        /// <summary>Identifier this handler is responsible for (matches <see cref="RecordedEvent.HandlerId"/>).</summary>
        string HandlerId { get; }

        /// <summary>Resolve any required references on the playback root before evaluation begins.</summary>
        void Prepare(InteractionEventBinder binder, GameObject root);

        /// <summary>Re-fire the recorded event. Allocation-free.</summary>
        void Invoke(in RecordedEvent recordedEvent);

        /// <summary>Release transient state when playback stops.</summary>
        void OnStop();
    }
}
