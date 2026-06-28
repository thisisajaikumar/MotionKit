using UnityEngine;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Stamps a GameObject with a stable GUID that survives renames, hierarchy changes, prefab nesting and
    /// scene reloads. Recordings bind to objects by this id first, falling back to hierarchy path only for
    /// legacy data. The editor auto-assigns ids to recorded objects; ids can also be assigned at runtime for
    /// dynamically spawned content.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MotionKit/Persistent Object Id")]
    public sealed class PersistentObjectId : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string _id;

        /// <summary>The persistent GUID (32-char "N" format) or empty if unassigned.</summary>
        public string Id { get { return _id; } }

        /// <summary>True when a non-empty id has been assigned.</summary>
        public bool HasId { get { return !string.IsNullOrEmpty(_id); } }

        /// <summary>Assigns a specific id (used by migration and runtime stamping). No-op if null/empty.</summary>
        public void Assign(string id)
        {
            if (!string.IsNullOrEmpty(id))
                _id = id;
        }

        /// <summary>Assigns a fresh GUID if none is present and returns the effective id.</summary>
        public string EnsureId()
        {
            if (string.IsNullOrEmpty(_id))
                _id = System.Guid.NewGuid().ToString("N");
            return _id;
        }
    }
}
