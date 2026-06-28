using UnityEngine;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Convenience base for recorders. Caches the target and context and exposes typed lifecycle hooks.
    /// </summary>
    /// <typeparam name="TData">Concrete recorded track data type produced by this recorder.</typeparam>
    public abstract class TrackRecorderBase<TData> : ITrackRecorder where TData : RecordedTrack, new()
    {
        /// <summary>The object being sampled.</summary>
        protected GameObject Target { get; private set; }

        /// <summary>The sampling configuration for the active session.</summary>
        protected RecorderContext Context { get; private set; }

        /// <summary>Session-relative time the recording started.</summary>
        protected float StartTime { get; private set; }

        /// <inheritdoc/>
        public virtual void Initialize(GameObject target, in RecorderContext context)
        {
            Target = target;
            Context = context;
            OnInitialize();
        }

        /// <inheritdoc/>
        public virtual void BeginRecord(float time)
        {
            StartTime = time;
            OnBeginRecord(time);
        }

        /// <inheritdoc/>
        public abstract void RecordFrame(float time);

        /// <inheritdoc/>
        public abstract RecordedTrack EndRecord();

        /// <summary>Override to cache component references after the target/context are assigned.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>Override to reset buffers when recording begins.</summary>
        protected virtual void OnBeginRecord(float time) { }
    }
}
