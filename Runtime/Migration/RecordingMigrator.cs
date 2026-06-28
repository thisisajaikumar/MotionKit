using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Migration
{
    /// <summary>
    /// Runs the registered <see cref="IRecordingMigration"/> chain to bring an older recording up to the current
    /// file version. Safe to call on every load (no-op when already current), so old assets are never broken by
    /// newer MotionKit versions.
    /// </summary>
    public static class RecordingMigrator
    {
        private const int SafetyGuard = 64;

        /// <summary>
        /// Upgrades <paramref name="recording"/> in place if its file version is behind. Returns true if any
        /// migration was applied. Logs a warning when no migration path exists for an intermediate version.
        /// </summary>
        public static bool EnsureUpToDate(InteractionRecording recording)
        {
            if (recording == null) return false;
            if (recording.Version.FileVersion >= RecordingVersion.CurrentFileVersion)
                return false;

            IReadOnlyList<IRecordingMigration> migrations = ModuleRegistry.GetMigrations();
            bool changed = false;
            int guard = 0;

            while (recording.Version.FileVersion < RecordingVersion.CurrentFileVersion && guard++ < SafetyGuard)
            {
                int from = recording.Version.FileVersion;
                IRecordingMigration step = FindStep(migrations, from);
                if (step == null)
                {
                    Debug.LogWarning("[MotionKit] No migration registered from file version " + from +
                                     "; '" + recording.RecordingName + "' left at v" + from + ".");
                    break;
                }

                step.Migrate(recording);
                RecordingVersion v = recording.Version;
                v.FileVersion = step.ToVersion;
                recording.Version = v;
                changed = true;
            }

            if (changed)
            {
                RecordingVersion v = recording.Version;
                v.Touch();
                recording.Version = v;
                Debug.Log("[MotionKit] Migrated '" + recording.RecordingName + "' to file version " +
                          recording.Version.FileVersion + ".");
            }

            return changed;
        }

        private static IRecordingMigration FindStep(IReadOnlyList<IRecordingMigration> migrations, int from)
        {
            for (int i = 0; i < migrations.Count; i++)
                if (migrations[i].FromVersion == from)
                    return migrations[i];
            return null;
        }
    }
}
