using AjoyGames.MotionKit.Data;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// One step in the recording-upgrade chain. Migrations are applied in ascending order so that a recording
    /// authored with any earlier file version is brought up to the current version without data loss. Mark
    /// implementations with <c>[RecordingMigration]</c> for automatic, reflection-free discovery.
    /// </summary>
    public interface IRecordingMigration
    {
        /// <summary>The file version this migration upgrades <i>from</i>.</summary>
        int FromVersion { get; }

        /// <summary>The file version this migration upgrades <i>to</i> (typically <see cref="FromVersion"/> + 1).</summary>
        int ToVersion { get; }

        /// <summary>Mutates <paramref name="recording"/> in place to the new format. Must be idempotent-safe.</summary>
        void Migrate(InteractionRecording recording);
    }
}
