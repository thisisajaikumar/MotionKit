using UnityEngine;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Optional capability: a track that can bake itself into an <see cref="AnimationClip"/>. The asset builder
    /// calls this for every track that implements it, so new bakable tracks need no changes to the baker.
    /// Tracks that hold non-curve data (events, state) simply don't implement this.
    /// </summary>
    public interface IBakableTrack
    {
        /// <summary>
        /// Writes this track's curves into <paramref name="clip"/> under <paramref name="path"/>. Only samples
        /// within [<paramref name="startTime"/>, <paramref name="endTime"/>] are written, rebased so the clip
        /// starts at zero.
        /// </summary>
        void Bake(AnimationClip clip, string path, float startTime, float endTime);
    }
}
