using System;
using UnityEngine;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Abstract, serializable base for all recorded track data. Concrete tracks store their samples as parallel
    /// primitive arrays (struct-of-arrays) so playback can evaluate them with zero allocations.
    /// </summary>
    /// <remarks>
    /// Track instances are stored polymorphically on a <see cref="Data.RecordedObject"/> via
    /// <c>[SerializeReference]</c>. Always provide a stable <see cref="TrackTypeId"/> — it is persisted and used
    /// by migrations and tooling.
    /// </remarks>
    [Serializable]
    public abstract class RecordedTrack
    {
        [SerializeField] private string _label;
        [SerializeField] private bool _enabled = true;

        /// <summary>Human readable label shown in the timeline editor (e.g. the source component name).</summary>
        public string Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <summary>When false the track is skipped during playback and asset generation.</summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>Stable identifier for the owning track module. Persisted; must never change for a module.</summary>
        public abstract string TrackTypeId { get; }

        /// <summary>Length of the recorded data in seconds (0 when empty).</summary>
        public abstract float Duration { get; }

        /// <summary>Total number of stored keyframes across all recorded properties.</summary>
        public abstract int KeyframeCount { get; }
    }
}
