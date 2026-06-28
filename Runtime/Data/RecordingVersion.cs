using System;
using UnityEngine;

namespace AjoyGames.MotionKit.Data
{
    /// <summary>
    /// Versioning header embedded in every <see cref="InteractionRecording"/>. Drives automatic migration so
    /// that recordings authored by older MotionKit versions are upgraded on load without breaking user assets.
    /// </summary>
    [Serializable]
    public struct RecordingVersion
    {
        /// <summary>Schema version of the recording file. Incremented whenever the data format changes.</summary>
        public const int CurrentFileVersion = 1;

        /// <summary>Human-readable MotionKit package version (kept in sync with package.json / CHANGELOG).</summary>
        public const string CurrentPackageVersion = "1.0.0";

        [Tooltip("Schema version of this recording. Older values trigger migration on load.")]
        public int FileVersion;

        [Tooltip("MotionKit package version that last wrote this recording.")]
        public string PackageVersion;

        [Tooltip("Unity editor version that authored this recording.")]
        public string UnityVersion;

        [Tooltip("ISO-8601 creation timestamp (UTC).")]
        public string CreatedDateUtc;

        [Tooltip("ISO-8601 last-modified timestamp (UTC).")]
        public string LastModifiedDateUtc;

        /// <summary>Creates a version header stamped with the current file/package version and now() timestamps.</summary>
        public static RecordingVersion CreateCurrent(string unityVersion)
        {
            string now = DateTime.UtcNow.ToString("o");
            return new RecordingVersion
            {
                FileVersion = CurrentFileVersion,
                PackageVersion = CurrentPackageVersion,
                UnityVersion = unityVersion,
                CreatedDateUtc = now,
                LastModifiedDateUtc = now
            };
        }

        /// <summary>Updates the last-modified timestamp and current package version in place.</summary>
        public void Touch()
        {
            LastModifiedDateUtc = DateTime.UtcNow.ToString("o");
            PackageVersion = CurrentPackageVersion;
        }
    }
}
