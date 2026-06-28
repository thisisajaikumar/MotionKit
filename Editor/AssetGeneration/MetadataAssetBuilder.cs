using UnityEngine;
using AjoyGames.MotionKit.Data;

namespace AjoyGames.MotionKit.Editor.AssetGeneration
{
    /// <summary>Builds the queryable <see cref="RecordingMetadata"/> summary for a recording.</summary>
    public static class MetadataAssetBuilder
    {
        /// <summary>Creates an unsaved metadata instance populated from <paramref name="recording"/>.</summary>
        public static RecordingMetadata Build(InteractionRecording recording)
        {
            var metadata = ScriptableObject.CreateInstance<RecordingMetadata>();
            metadata.PopulateFrom(recording);
            return metadata;
        }
    }
}
