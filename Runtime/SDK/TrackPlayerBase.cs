using UnityEngine;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Convenience base for players. Resolves and stores the typed track and target; subclasses cache component
    /// references in <see cref="OnPrepare"/> and apply state in <see cref="Evaluate"/>.
    /// </summary>
    /// <typeparam name="TData">Concrete recorded track data type this player consumes.</typeparam>
    public abstract class TrackPlayerBase<TData> : ITrackPlayer where TData : RecordedTrack
    {
        /// <summary>The typed track data bound to this player.</summary>
        protected TData Data { get; private set; }

        /// <summary>The resolved playback target.</summary>
        protected GameObject Target { get; private set; }

        /// <inheritdoc/>
        public bool Prepare(RecordedTrack track, GameObject target)
        {
            Data = track as TData;
            Target = target;
            if (Data == null || target == null)
                return false;
            return OnPrepare();
        }

        /// <inheritdoc/>
        public abstract void Evaluate(float time);

        /// <inheritdoc/>
        public virtual void OnStop() { }

        /// <summary>Cache component references here. Return false to skip the binding (missing components).</summary>
        protected abstract bool OnPrepare();
    }
}
