using UnityEditor;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Migration;

namespace AjoyGames.MotionKit.Editor.Migration
{
    /// <summary>
    /// Auto-migrates recording assets on import so older files are upgraded the moment they enter the project,
    /// not just at play time. Re-saves any recording the migrator changed.
    /// </summary>
    public sealed class RecordingPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            bool changedAny = false;
            for (int i = 0; i < imported.Length; i++)
            {
                if (!imported[i].EndsWith(".asset")) continue;
                InteractionRecording recording = AssetDatabase.LoadAssetAtPath<InteractionRecording>(imported[i]);
                if (recording == null) continue;

                if (RecordingMigrator.EnsureUpToDate(recording))
                {
                    EditorUtility.SetDirty(recording);
                    changedAny = true;
                }
            }
            if (changedAny)
                AssetDatabase.SaveAssets();
        }
    }
}
