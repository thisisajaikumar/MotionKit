#if MOTIONKIT_DIRECTOR
using UnityEngine;
using UnityEngine.Playables;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Events.Handlers
{
    /// <summary>
    /// Bridges recorded events to Unity's playable director. On playback it (re)starts a named
    /// <see cref="PlayableDirector"/> found under the root, letting a recording trigger a timeline at the
    /// captured moment. The director name is stored in <see cref="RecordedEvent.Key"/>; when empty, the first
    /// director on the root is used.
    /// </summary>
    [EventHandlerModule(HandlerId)]
    public sealed class TimelineSignalHandler : IRecordedEventHandler
    {
        /// <summary>Stable handler id persisted in recordings.</summary>
        public const string HandlerId = "timelineSignal";

        private PlayableDirector[] _directors;

        string IRecordedEventHandler.HandlerId { get { return HandlerId; } }

        public void Prepare(InteractionEventBinder binder, GameObject root)
        {
            _directors = root != null ? root.GetComponentsInChildren<PlayableDirector>(true) : System.Array.Empty<PlayableDirector>();
        }

        public void Invoke(in RecordedEvent recordedEvent)
        {
            if (_directors == null || _directors.Length == 0) return;

            for (int i = 0; i < _directors.Length; i++)
            {
                PlayableDirector d = _directors[i];
                if (d == null) continue;
                if (string.IsNullOrEmpty(recordedEvent.Key) || d.gameObject.name == recordedEvent.Key)
                {
                    d.time = 0d;
                    d.Play();
                    if (!string.IsNullOrEmpty(recordedEvent.Key)) return;
                }
            }
        }

        public void OnStop() { }
    }
}
#endif
