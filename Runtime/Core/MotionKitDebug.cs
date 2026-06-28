using UnityEngine;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Lightweight, toggleable diagnostic logging for the recording/playback pipeline. Off by default so it
    /// never spams production builds; enable it from <c>Tools ▸ MotionKit ▸ Verbose Logging</c> (or by setting
    /// <see cref="Verbose"/>) to trace per-stage object/binding counts when debugging multi-object recordings.
    /// </summary>
    public static class MotionKitDebug
    {
        /// <summary>When true, the pipeline emits stage-by-stage diagnostic logs.</summary>
        public static bool Verbose;

        /// <summary>Logs a verbose message (no-op unless <see cref="Verbose"/> is enabled).</summary>
        public static void Log(string message)
        {
            if (Verbose)
                Debug.Log("[MotionKit] " + message);
        }
    }
}
