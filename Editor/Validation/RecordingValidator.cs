using System.Collections.Generic;
using UnityEditor;
using AjoyGames.MotionKit.Data;

namespace AjoyGames.MotionKit.Editor.Validation
{
    /// <summary>A single validation finding for a recording.</summary>
    public readonly struct ValidationMessage
    {
        public readonly MessageType Severity;
        public readonly string Text;
        public ValidationMessage(MessageType severity, string text) { Severity = severity; Text = text; }
    }

    /// <summary>
    /// Surfaces common recording problems (empty data, unresolved bindings, stale version) so users get
    /// actionable warnings instead of silent playback failures.
    /// </summary>
    public static class RecordingValidator
    {
        /// <summary>Validates a recording and appends findings to <paramref name="results"/>.</summary>
        public static void Validate(InteractionRecording recording, List<ValidationMessage> results)
        {
            results.Clear();
            if (recording == null)
            {
                results.Add(new ValidationMessage(MessageType.Error, "Recording is null."));
                return;
            }

            if (recording.Objects.Count == 0 && recording.Events.Count == 0)
                results.Add(new ValidationMessage(MessageType.Warning, "Recording has no objects or events."));

            if (recording.Duration <= 0f)
                results.Add(new ValidationMessage(MessageType.Info, "Recording duration is zero."));

            if (recording.Version.FileVersion < RecordingVersion.CurrentFileVersion)
                results.Add(new ValidationMessage(MessageType.Warning,
                    "File version " + recording.Version.FileVersion + " is older than current " +
                    RecordingVersion.CurrentFileVersion + "; it will be migrated on load."));

            int missingIds = 0, emptyTracks = 0;
            for (int o = 0; o < recording.Objects.Count; o++)
            {
                RecordedObject ro = recording.Objects[o];
                if (recording.BindingMode == BindingMode.PersistentId && string.IsNullOrEmpty(ro.PersistentId))
                    missingIds++;
                for (int t = 0; t < ro.Tracks.Count; t++)
                    if (ro.Tracks[t] != null && ro.Tracks[t].KeyframeCount == 0)
                        emptyTracks++;
            }

            if (missingIds > 0)
                results.Add(new ValidationMessage(MessageType.Warning,
                    missingIds + " object(s) lack a persistent id and will fall back to path binding."));
            if (emptyTracks > 0)
                results.Add(new ValidationMessage(MessageType.Info, emptyTracks + " track(s) contain no keyframes."));
        }
    }
}
