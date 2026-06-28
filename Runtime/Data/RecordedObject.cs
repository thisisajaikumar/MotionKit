using System;
using System.Collections.Generic;
using UnityEngine;

namespace AjoyGames.MotionKit.Data
{
    /// <summary>
    /// One object captured by a recording: its persistent GUID, a fallback hierarchy path, and the polymorphic
    /// set of tracks recorded for it. Tracks are stored via <c>[SerializeReference]</c> so any number of
    /// heterogeneous track types can be persisted in a single asset.
    /// </summary>
    [Serializable]
    public sealed class RecordedObject
    {
        [SerializeField] private string _persistentId;
        [SerializeField] private string _hierarchyPath;
        [SerializeField] private string _displayName;
        [SerializeReference] private List<RecordedTrack> _tracks = new List<RecordedTrack>();

        /// <summary>Persistent GUID of the source object (primary binding key).</summary>
        public string PersistentId
        {
            get { return _persistentId; }
            set { _persistentId = value; }
        }

        /// <summary>Hierarchy path relative to the recording root (legacy / fallback binding key).</summary>
        public string HierarchyPath
        {
            get { return _hierarchyPath; }
            set { _hierarchyPath = value; }
        }

        /// <summary>Display name shown in the timeline editor.</summary>
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        /// <summary>The recorded tracks for this object.</summary>
        public List<RecordedTrack> Tracks { get { return _tracks; } }

        /// <summary>Longest track duration on this object, in seconds.</summary>
        public float Duration
        {
            get
            {
                float max = 0f;
                for (int i = 0; i < _tracks.Count; i++)
                {
                    if (_tracks[i] != null && _tracks[i].Duration > max)
                        max = _tracks[i].Duration;
                }
                return max;
            }
        }
    }
}
