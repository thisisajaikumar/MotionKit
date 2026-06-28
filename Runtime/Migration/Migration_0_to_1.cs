using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Migration
{
    /// <summary>
    /// Baseline migration from file version 0 (legacy or hand-authored assets that predate the versioning
    /// header) to version 1. Chooses a binding mode based on whether persistent ids are present and refreshes
    /// the cached duration. Serves as the template for future migrations.
    /// </summary>
    [RecordingMigration]
    public sealed class Migration_0_to_1 : IRecordingMigration
    {
        public int FromVersion { get { return 0; } }
        public int ToVersion { get { return 1; } }

        public void Migrate(InteractionRecording recording)
        {
            bool anyPersistentId = false;
            for (int i = 0; i < recording.Objects.Count; i++)
            {
                if (!string.IsNullOrEmpty(recording.Objects[i].PersistentId))
                {
                    anyPersistentId = true;
                    break;
                }
            }

            recording.BindingMode = anyPersistentId ? BindingMode.PersistentId : BindingMode.HierarchyPathOnly;
            recording.RecalculateDuration();
            recording.SortEvents();
        }
    }
}
