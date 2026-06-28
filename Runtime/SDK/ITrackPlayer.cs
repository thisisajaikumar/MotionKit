using UnityEngine;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Applies a <see cref="RecordedTrack"/> onto a live target at a given time. Implementations must be
    /// allocation-free inside <see cref="Evaluate"/> — cache all component lookups in <see cref="Prepare"/>.
    /// </summary>
    public interface ITrackPlayer
    {
        /// <summary>
        /// Bind the player to its track data and resolved target. Returns false if the target is missing a
        /// required component, in which case the binding is skipped (a warning is logged once by the engine).
        /// </summary>
        bool Prepare(RecordedTrack track, GameObject target);

        /// <summary>Apply the recorded state at <paramref name="time"/> (seconds). Allocation-free.</summary>
        void Evaluate(float time);

        /// <summary>Called when playback stops so the player can restore or release any transient state.</summary>
        void OnStop();
    }
}
