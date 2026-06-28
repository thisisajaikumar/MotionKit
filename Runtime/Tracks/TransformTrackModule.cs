using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Track module for <see cref="TransformTrackData"/>. Applies to every GameObject (all have a Transform).
    /// Discovered and registered automatically by the editor code generator.
    /// </summary>
    [TrackModule(Order = 0)]
    public sealed class TransformTrackModule : TrackModuleBase<TransformTrackData, TransformTrackRecorder, TransformTrackPlayer>
    {
        /// <summary>Every GameObject has a Transform, so this module always applies.</summary>
        public TransformTrackModule() : base(typeof(Transform)) { }

        /// <inheritdoc/>
        public override string Id { get { return TransformTrackData.TypeId; } }

        /// <inheritdoc/>
        public override string DisplayName { get { return "Transform"; } }
    }
}
