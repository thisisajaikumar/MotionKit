using System;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Marks a class (implementing <see cref="AjoyGames.MotionKit.SDK.IRecordingMigration"/>) as a recording
    /// migration step. Migrations are discovered at edit time and registered (reflection-free) so that older
    /// recordings auto-upgrade on load.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RecordingMigrationAttribute : Attribute
    {
    }
}
