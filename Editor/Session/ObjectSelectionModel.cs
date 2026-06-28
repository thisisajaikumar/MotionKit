using System.Collections.Generic;
using UnityEngine;

namespace AjoyGames.MotionKit.Editor.Session
{
    /// <summary>
    /// Holds the set of root objects the user wants to record plus capture options, and expands them into the
    /// concrete list of GameObjects to sample (optionally including children and inactive objects).
    /// </summary>
    public sealed class ObjectSelectionModel
    {
        private readonly List<GameObject> _roots = new List<GameObject>();

        /// <summary>Also capture descendants of each root.</summary>
        public bool CaptureChildren = true;

        /// <summary>Include inactive GameObjects when expanding.</summary>
        public bool CaptureDisabledObjects = false;

        /// <summary>The user-chosen root objects.</summary>
        public IReadOnlyList<GameObject> Roots { get { return _roots; } }

        /// <summary>Adds a root (ignores nulls and duplicates).</summary>
        public void Add(GameObject go)
        {
            if (go != null && !_roots.Contains(go))
                _roots.Add(go);
        }

        /// <summary>Adds the current Unity selection.</summary>
        public void AddSelection(GameObject[] selection)
        {
            if (selection == null) return;
            for (int i = 0; i < selection.Length; i++)
                Add(selection[i]);
        }

        /// <summary>Removes a root.</summary>
        public void Remove(GameObject go) { _roots.Remove(go); }

        /// <summary>Clears all roots.</summary>
        public void Clear() { _roots.Clear(); }
    }
}
