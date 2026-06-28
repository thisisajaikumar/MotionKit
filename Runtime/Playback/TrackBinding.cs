using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Playback
{
    /// <summary>
    /// A resolved, ready-to-evaluate pairing of a track player and its live target. Stored in a pre-sized list
    /// by the playback engine so evaluation never allocates.
    /// </summary>
    public readonly struct TrackBinding
    {
        /// <summary>The player applying the track to the target.</summary>
        public readonly ITrackPlayer Player;

        /// <summary>The resolved target GameObject.</summary>
        public readonly GameObject Target;

        public TrackBinding(ITrackPlayer player, GameObject target)
        {
            Player = player;
            Target = target;
        }
    }
}
