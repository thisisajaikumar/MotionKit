using System;
using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Events.Handlers
{
    /// <summary>
    /// Re-fires recorded events as plain C# delegate callbacks. Application code registers named callbacks once;
    /// recorded events invoke them by key during playback. This is the lightest extension point for wiring
    /// gameplay logic to a recording without UnityEvents.
    /// </summary>
    [EventHandlerModule(HandlerId)]
    public sealed class DelegateEventHandler : IRecordedEventHandler
    {
        /// <summary>Stable handler id persisted in recordings.</summary>
        public const string HandlerId = "delegate";

        private static readonly Dictionary<string, Action<RecordedEvent>> s_callbacks =
            new Dictionary<string, Action<RecordedEvent>>(StringComparer.Ordinal);

        string IRecordedEventHandler.HandlerId { get { return HandlerId; } }

        /// <summary>Registers (or replaces) a callback fired when an event with <paramref name="key"/> plays.</summary>
        public static void Register(string key, Action<RecordedEvent> callback)
        {
            if (string.IsNullOrEmpty(key) || callback == null) return;
            s_callbacks[key] = callback;
        }

        /// <summary>Removes a previously registered callback.</summary>
        public static void Unregister(string key)
        {
            if (!string.IsNullOrEmpty(key)) s_callbacks.Remove(key);
        }

        public void Prepare(InteractionEventBinder binder, GameObject root) { }

        public void Invoke(in RecordedEvent recordedEvent)
        {
            Action<RecordedEvent> cb;
            if (recordedEvent.Key != null && s_callbacks.TryGetValue(recordedEvent.Key, out cb))
                cb(recordedEvent);
        }

        public void OnStop() { }
    }
}
