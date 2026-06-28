using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.Events;

namespace AjoyGames.MotionKit.Data
{
    /// <summary>
    /// The single source of truth for a recording. Stores the versioning header, recorded objects (each with
    /// its polymorphic tracks), the universal event stream, and optional clip ranges for multi-clip export.
    /// AnimationClips, prefabs, previews and metadata are <i>generated from</i> this asset and can be
    /// regenerated at any time without re-recording.
    /// </summary>
    [CreateAssetMenu(menuName = "MotionKit/Interaction Recording", fileName = "Recording")]
    public sealed class InteractionRecording : ScriptableObject
    {
        [SerializeField] private RecordingVersion _version;
        [SerializeField] private string _recordingName;
        [SerializeField] private int _fps = 60;
        [SerializeField] private float _duration;
        [SerializeField] private BindingMode _bindingMode = BindingMode.PersistentId;
        [SerializeField] private List<RecordedObject> _objects = new List<RecordedObject>();
        [SerializeField] private List<RecordedEvent> _events = new List<RecordedEvent>();
        [SerializeField] private List<ClipRange> _clipRanges = new List<ClipRange>();

        /// <summary>Versioning header (file/package/unity version + timestamps).</summary>
        public RecordingVersion Version
        {
            get { return _version; }
            set { _version = value; }
        }

        /// <summary>Display / asset name of the recording.</summary>
        public string RecordingName
        {
            get { return _recordingName; }
            set { _recordingName = value; }
        }

        /// <summary>Sampling rate the recording was captured at.</summary>
        public int Fps
        {
            get { return _fps; }
            set { _fps = Mathf.Max(1, value); }
        }

        /// <summary>Total duration in seconds (cached; refresh via <see cref="RecalculateDuration"/>).</summary>
        public float Duration { get { return _duration; } }

        /// <summary>Total frame count derived from duration and fps.</summary>
        public int FrameCount { get { return Mathf.RoundToInt(_duration * _fps); } }

        /// <summary>How objects are resolved at playback time.</summary>
        public BindingMode BindingMode
        {
            get { return _bindingMode; }
            set { _bindingMode = value; }
        }

        /// <summary>Recorded objects and their tracks.</summary>
        public List<RecordedObject> Objects { get { return _objects; } }

        /// <summary>The universal event stream (time-ordered after <see cref="SortEvents"/>).</summary>
        public List<RecordedEvent> Events { get { return _events; } }

        /// <summary>User-defined clip boundaries for multi-clip export.</summary>
        public List<ClipRange> ClipRanges { get { return _clipRanges; } }

        /// <summary>Recomputes cached duration from the longest track across all objects and events.</summary>
        public float RecalculateDuration()
        {
            float max = 0f;
            for (int i = 0; i < _objects.Count; i++)
            {
                float d = _objects[i].Duration;
                if (d > max) max = d;
            }
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Time > max) max = _events[i].Time;
            }
            _duration = max;
            return _duration;
        }

        /// <summary>Sorts the event stream by time (stable enough for evaluation cursors).</summary>
        public void SortEvents()
        {
            _events.Sort((a, b) => a.Time.CompareTo(b.Time));
        }
    }
}
