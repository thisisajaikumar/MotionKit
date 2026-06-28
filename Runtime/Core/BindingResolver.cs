using System;
using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.Data;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Resolves recorded objects to live GameObjects under a playback root. Builds a GUID→object map once
    /// (allocation-free thereafter) and resolves by <see cref="PersistentObjectId"/> first, falling back to
    /// hierarchy path for legacy recordings. Renames, reparenting and prefab nesting do not break GUID binding.
    /// </summary>
    public sealed class BindingResolver
    {
        private readonly Dictionary<string, GameObject> _byId = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly List<PersistentObjectId> _idBuffer = new List<PersistentObjectId>(64);
        private Transform _root;

        /// <summary>Scans <paramref name="root"/> and caches all persistent ids beneath it. Reusable per playback.</summary>
        public void Build(GameObject root)
        {
            _byId.Clear();
            _root = root != null ? root.transform : null;
            if (root == null) return;

            AddIds(root);
        }

        /// <summary>
        /// Scans several roots at once and caches every persistent id beneath them. Used by the editor preview,
        /// which drives the original (separate) scene objects rather than a single container prefab. Path
        /// fallback uses the first non-null root; GUID resolution covers objects under any of them.
        /// </summary>
        public void Build(System.Collections.Generic.IReadOnlyList<GameObject> roots)
        {
            _byId.Clear();
            _root = null;
            if (roots == null) return;

            for (int r = 0; r < roots.Count; r++)
            {
                GameObject root = roots[r];
                if (root == null) continue;
                if (_root == null) _root = root.transform;
                AddIds(root);
            }
        }

        private void AddIds(GameObject root)
        {
            _idBuffer.Clear();
            root.GetComponentsInChildren(true, _idBuffer);
            for (int i = 0; i < _idBuffer.Count; i++)
            {
                PersistentObjectId poid = _idBuffer[i];
                if (poid != null && poid.HasId && !_byId.ContainsKey(poid.Id))
                    _byId.Add(poid.Id, poid.gameObject);
            }
        }

        /// <summary>Resolves a recorded object to a live GameObject, or null when it cannot be found.</summary>
        public GameObject Resolve(RecordedObject recorded, BindingMode mode)
        {
            if (recorded == null) return null;

            GameObject byId;
            if (mode == BindingMode.PersistentId && !string.IsNullOrEmpty(recorded.PersistentId) &&
                _byId.TryGetValue(recorded.PersistentId, out byId) && byId != null)
                return byId;

            return ResolveByPath(recorded.HierarchyPath);
        }

        /// <summary>Walks the hierarchy path (relative to the root) by transform name.</summary>
        public GameObject ResolveByPath(string path)
        {
            if (_root == null) return null;
            if (string.IsNullOrEmpty(path)) return _root.gameObject;

            Transform current = _root;
            int start = 0;
            int firstSlash = path.IndexOf('/');
            string firstSegment = firstSlash < 0 ? path : path.Substring(0, firstSlash);
            if (firstSegment == _root.name)
                start = firstSlash < 0 ? path.Length : firstSlash + 1;

            while (start < path.Length && current != null)
            {
                int slash = path.IndexOf('/', start);
                int end = slash < 0 ? path.Length : slash;
                int len = end - start;
                current = FindChildByName(current, path, start, len);
                start = slash < 0 ? path.Length : slash + 1;
            }

            return current != null ? current.gameObject : null;
        }

        private static Transform FindChildByName(Transform parent, string source, int start, int length)
        {
            int count = parent.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = parent.GetChild(i);
                string name = child.name;
                if (name.Length == length && string.CompareOrdinal(name, 0, source, start, length) == 0)
                    return child;
            }
            return null;
        }

        /// <summary>Computes a hierarchy path for <paramref name="target"/> relative to <paramref name="root"/>.</summary>
        public static string ComputePath(Transform root, Transform target)
        {
            if (target == null) return string.Empty;
            if (root == null || target == root) return target.name;

            var stack = new List<string>(8);
            Transform t = target;
            while (t != null && t != root)
            {
                stack.Add(t.name);
                t = t.parent;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                sb.Append(stack[i]);
                if (i > 0) sb.Append('/');
            }
            return sb.ToString();
        }
    }
}
