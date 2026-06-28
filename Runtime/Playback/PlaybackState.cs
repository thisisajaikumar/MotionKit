namespace AjoyGames.MotionKit.Playback
{
    /// <summary>Mutable playback configuration and cursor for a single playing recording.</summary>
    public sealed class PlaybackState
    {
        /// <summary>Current playback time in seconds (clamped to [0, duration]).</summary>
        public float Time;

        /// <summary>Playback rate multiplier. 1 = real time; supports values &gt; 1 and fractional speeds.</summary>
        public float Speed = 1f;

        /// <summary>When true, playback wraps to the start (or end, when reversed) on reaching the bounds.</summary>
        public bool Loop;

        /// <summary>When true, time advances backwards.</summary>
        public bool Reverse;

        /// <summary>True between Play and Stop.</summary>
        public bool IsPlaying;

        /// <summary>True when paused (still "playing" but not advancing).</summary>
        public bool IsPaused;

        /// <summary>Resets transient cursor/flags to a stopped state, preserving Speed/Loop/Reverse config.</summary>
        public void ResetForStop()
        {
            Time = 0f;
            IsPlaying = false;
            IsPaused = false;
        }
    }
}
