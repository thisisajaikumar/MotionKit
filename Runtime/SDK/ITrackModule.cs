using System;
using System.Collections.Generic;
using UnityEngine;

namespace AjoyGames.MotionKit.SDK
{
    /// <summary>
    /// Describes a recordable track type and acts as the reflection-free factory for its data, recorder and
    /// player instances. Implement this and mark the class with <c>[TrackModule]</c>; the editor code generator
    /// will register it automatically.
    /// </summary>
    public interface ITrackModule
    {
        /// <summary>Stable identifier persisted on tracks. Must equal the produced track's <c>TrackTypeId</c>.</summary>
        string Id { get; }

        /// <summary>Human readable name shown in the recorder UI.</summary>
        string DisplayName { get; }

        /// <summary>Concrete <see cref="RecordedTrack"/> type this module produces (used to map data to player).</summary>
        Type TrackDataType { get; }

        /// <summary>
        /// Component types this module can record. The recorder offers this module for a target only when the
        /// target has at least one of these components. Empty means "applies to any GameObject".
        /// </summary>
        IReadOnlyList<Type> RecordableComponentTypes { get; }

        /// <summary>True if this module should attempt to record the supplied target in the current context.</summary>
        bool CanRecord(GameObject target);

        /// <summary>Create an empty data container (reflection-free; just <c>new</c>).</summary>
        RecordedTrack CreateData();

        /// <summary>Create a recorder instance (reflection-free; just <c>new</c>).</summary>
        ITrackRecorder CreateRecorder();

        /// <summary>Create a player instance (reflection-free; just <c>new</c>).</summary>
        ITrackPlayer CreatePlayer();
    }
}
