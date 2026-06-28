using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AjoyGames.MotionKit.Events
{
    /// <summary>
    /// Lives on a generated recording prefab and exposes named <see cref="UnityEvent"/> slots that recorded
    /// events bind to at playback time. Authors wire concrete listeners (methods, audio, etc.) into these slots
    /// in the inspector; the playback engine invokes them by key, never recording the result.
    /// </summary>
    [AddComponentMenu("MotionKit/Interaction Event Binder")]
    public sealed class InteractionEventBinder : MonoBehaviour
    {
        /// <summary>A named, inspector-wireable UnityEvent slot.</summary>
        [Serializable]
        public sealed class NamedEvent
        {
            [Tooltip("Stable key referenced by recorded events.")]
            public string Key;

            [Tooltip("Listeners invoked when the recorded event fires.")]
            public UnityEvent Event = new UnityEvent();

            [Tooltip("Listeners that receive the recorded float payload.")]
            public FloatUnityEvent FloatEvent = new FloatUnityEvent();
        }

        /// <summary>Concrete serializable UnityEvent&lt;float&gt; (Unity cannot serialize the open generic).</summary>
        [Serializable]
        public sealed class FloatUnityEvent : UnityEvent<float> { }

        [SerializeField] private List<NamedEvent> _events = new List<NamedEvent>();

        private Dictionary<string, NamedEvent> _lookup;

        /// <summary>All configured named events (read-only view for inspectors/tools).</summary>
        public IReadOnlyList<NamedEvent> Events { get { return _events; } }

        private void Awake() { RebuildLookup(); }

        /// <summary>(Re)builds the key→slot map. Call after editing slots at runtime.</summary>
        public void RebuildLookup()
        {
            if (_lookup == null)
                _lookup = new Dictionary<string, NamedEvent>(_events.Count, StringComparer.Ordinal);
            else
                _lookup.Clear();

            for (int i = 0; i < _events.Count; i++)
            {
                NamedEvent e = _events[i];
                if (e != null && !string.IsNullOrEmpty(e.Key))
                    _lookup[e.Key] = e;
            }
        }

        /// <summary>Fires the parameterless slot for <paramref name="key"/> if present.</summary>
        public void Invoke(string key)
        {
            if (_lookup == null) RebuildLookup();
            NamedEvent slot;
            if (_lookup.TryGetValue(key, out slot) && slot.Event != null)
                slot.Event.Invoke();
        }

        /// <summary>Fires the float slot for <paramref name="key"/> if present.</summary>
        public void Invoke(string key, float payload)
        {
            if (_lookup == null) RebuildLookup();
            NamedEvent slot;
            if (_lookup.TryGetValue(key, out slot) && slot.FloatEvent != null)
                slot.FloatEvent.Invoke(payload);
        }

        /// <summary>Ensures a named slot exists (editor/codegen helper).</summary>
        public void EnsureSlot(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            for (int i = 0; i < _events.Count; i++)
                if (_events[i] != null && _events[i].Key == key)
                    return;
            _events.Add(new NamedEvent { Key = key });
            _lookup = null;
        }
    }
}
