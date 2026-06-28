using UnityEngine;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Samples a live object's state into a <see cref="RecordedTrack"/>. Recorders run in the editor and in
    /// play mode; they are never referenced by name from core code — they are created via their owning
    /// <see cref="ITrackModule"/>.
    /// </summary>
    public interface ITrackRecorder
    {
        /// <summary>Bind the recorder to the object it will sample and provide sampling configuration.</summary>
        void Initialize(GameObject target, in RecorderContext context);

        /// <summary>Called once when recording begins. <paramref name="time"/> is the session-relative start time.</summary>
        void BeginRecord(float time);

        /// <summary>Sample the current state at the given session-relative time. Must not allocate per call.</summary>
        void RecordFrame(float time);

        /// <summary>Finalize and return the recorded track (applies compression / keyframe reduction).</summary>
        RecordedTrack EndRecord();
    }
}
