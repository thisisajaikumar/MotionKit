using System;
using System.Collections.Generic;
using UnityEngine;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Convenience base for <see cref="ITrackModule"/> implementations that record a single component type.
    /// </summary>
    /// <typeparam name="TData">Concrete recorded track data type.</typeparam>
    /// <typeparam name="TRecorder">Recorder type with a public parameterless constructor.</typeparam>
    /// <typeparam name="TPlayer">Player type with a public parameterless constructor.</typeparam>
    public abstract class TrackModuleBase<TData, TRecorder, TPlayer> : ITrackModule, IOrderedTrackModule
        where TData : RecordedTrack, new()
        where TRecorder : ITrackRecorder, new()
        where TPlayer : ITrackPlayer, new()
    {
        private readonly Type[] _componentTypes;

        /// <param name="recordableComponentTypes">Components that make this module applicable to a target.</param>
        protected TrackModuleBase(params Type[] recordableComponentTypes)
        {
            _componentTypes = recordableComponentTypes ?? Array.Empty<Type>();
        }

        /// <inheritdoc/>
        public abstract string Id { get; }

        /// <inheritdoc/>
        public abstract string DisplayName { get; }

        /// <inheritdoc/>
        public virtual int Order { get { return 0; } }

        /// <inheritdoc/>
        public Type TrackDataType { get { return typeof(TData); } }

        /// <inheritdoc/>
        public IReadOnlyList<Type> RecordableComponentTypes { get { return _componentTypes; } }

        /// <inheritdoc/>
        public virtual bool CanRecord(GameObject target)
        {
            if (target == null) return false;
            if (_componentTypes.Length == 0) return true;
            for (int i = 0; i < _componentTypes.Length; i++)
            {
                if (target.GetComponent(_componentTypes[i]) != null)
                    return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public virtual RecordedTrack CreateData() { return new TData(); }

        /// <inheritdoc/>
        public virtual ITrackRecorder CreateRecorder() { return new TRecorder(); }

        /// <inheritdoc/>
        public virtual ITrackPlayer CreatePlayer() { return new TPlayer(); }
    }
}
