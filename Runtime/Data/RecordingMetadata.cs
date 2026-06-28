using UnityEngine;

namespace AjoyGames.MotionKit.Data
{
    /// <summary>
    /// Lightweight, queryable summary asset generated alongside a recording. Lets tools and runtime code
    /// inspect a recording's shape (duration, counts, versions, preview) without loading all track data.
    /// </summary>
    public sealed class RecordingMetadata : ScriptableObject
    {
        [Tooltip("The recording this metadata describes.")]
        public InteractionRecording Recording;

        [Tooltip("Generated preview thumbnail.")]
        public Texture2D Preview;

        public string RecordingName;
        public float Duration;
        public int Fps;
        public int FrameCount;
        public int ObjectCount;
        public int TrackCount;
        public int EventCount;

        public int FileVersion;
        public string PackageVersion;
        public string UnityVersion;
        public string CreatedDateUtc;
        public string LastModifiedDateUtc;

        /// <summary>Populates the summary fields from a recording.</summary>
        public void PopulateFrom(InteractionRecording recording)
        {
            Recording = recording;
            if (recording == null) return;

            RecordingName = recording.RecordingName;
            Duration = recording.Duration;
            Fps = recording.Fps;
            FrameCount = recording.FrameCount;
            ObjectCount = recording.Objects.Count;
            EventCount = recording.Events.Count;

            int tracks = 0;
            for (int i = 0; i < recording.Objects.Count; i++)
                tracks += recording.Objects[i].Tracks.Count;
            TrackCount = tracks;

            FileVersion = recording.Version.FileVersion;
            PackageVersion = recording.Version.PackageVersion;
            UnityVersion = recording.Version.UnityVersion;
            CreatedDateUtc = recording.Version.CreatedDateUtc;
            LastModifiedDateUtc = recording.Version.LastModifiedDateUtc;
        }
    }
}
