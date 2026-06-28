using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Events.Handlers
{
    /// <summary>
    /// Re-fires recorded UnityEvent invocations through the prefab's <see cref="InteractionEventBinder"/>. The
    /// recorded <see cref="RecordedEvent.Key"/> selects the named slot; a float payload, when present, drives
    /// the float-typed slot.
    /// </summary>
    [EventHandlerModule(HandlerId)]
    public sealed class UnityEventHandler : IRecordedEventHandler
    {
        /// <summary>Stable handler id persisted in recordings.</summary>
        public const string HandlerId = "unityEvent";

        private InteractionEventBinder _binder;

        string IRecordedEventHandler.HandlerId { get { return HandlerId; } }

        public void Prepare(InteractionEventBinder binder, GameObject root)
        {
            _binder = binder;
        }

        public void Invoke(in RecordedEvent recordedEvent)
        {
            if (_binder == null) return;
            if (recordedEvent.IntPayload != 0)
                _binder.Invoke(recordedEvent.Key, recordedEvent.FloatPayload);
            else
                _binder.Invoke(recordedEvent.Key);
        }

        public void OnStop() { }
    }
}
