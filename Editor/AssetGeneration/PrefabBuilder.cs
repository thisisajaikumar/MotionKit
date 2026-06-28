using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit;
using AjoyGames.MotionKit.Components;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Events;
using AjoyGames.MotionKit.Events.Handlers;

namespace AjoyGames.MotionKit.Editor.AssetGeneration
{
    /// <summary>
    /// Builds a playable prefab whose single root is a container holding a copy of <b>every</b> recorded source
    /// root. This guarantees all recorded objects exist under one playback root, so multi-object recordings
    /// bind and play correctly. Re-stamps persistent ids and wires an <see cref="InteractionPlayer"/>, an
    /// <see cref="InteractionEventBinder"/> (with a slot per recorded UnityEvent key) and an
    /// <see cref="InteractionRecordingReference"/> on the container.
    /// </summary>
    public static class PrefabBuilder
    {
        /// <summary>
        /// Creates / overwrites the prefab at <paramref name="prefabPath"/> from copies of every
        /// <paramref name="sourceRoots"/>. Returns null (and logs) when no source objects are available.
        /// </summary>
        public static GameObject Build(InteractionRecording recording, IReadOnlyList<GameObject> sourceRoots,
            string prefabPath, RecordingMetadata metadata)
        {
            if (!HasAnyRoot(sourceRoots))
            {
                Debug.Log("[MotionKit] Prefab generation skipped: no source objects available (regenerate with the source(s) selected).");
                return null;
            }

            // One container holds a copy of every selected root, so every recorded object is present and
            // resolvable under a single playback root.
            var container = new GameObject(recording.RecordingName);

            try
            {
                for (int i = 0; i < sourceRoots.Count; i++)
                {
                    GameObject src = sourceRoots[i];
                    if (src == null) continue;
                    GameObject copy = Object.Instantiate(src);
                    copy.name = src.name; // strip the "(Clone)" suffix so hierarchy paths resolve
                    copy.transform.SetParent(container.transform, false);
                }

                EnsurePersistentIds(recording, container);

                InteractionPlayer player = GetOrAdd<InteractionPlayer>(container);
                player.Recording = recording;

                InteractionEventBinder binder = GetOrAdd<InteractionEventBinder>(container);
                EnsureEventSlots(recording, binder);

                InteractionRecordingReference reference = GetOrAdd<InteractionRecordingReference>(container);
                reference.Recording = recording;
                reference.Metadata = metadata;

                return PrefabUtility.SaveAsPrefabAsset(container, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(container);
            }
        }

        private static bool HasAnyRoot(IReadOnlyList<GameObject> roots)
        {
            if (roots == null) return false;
            for (int i = 0; i < roots.Count; i++)
                if (roots[i] != null) return true;
            return false;
        }

        private static void EnsurePersistentIds(InteractionRecording recording, GameObject instance)
        {
            var resolver = new BindingResolver();
            resolver.Build(instance);
            var objects = recording.Objects;
            for (int i = 0; i < objects.Count; i++)
            {
                RecordedObject ro = objects[i];
                GameObject go = resolver.ResolveByPath(ro.HierarchyPath);
                if (go == null) continue;
                PersistentObjectId poid = go.GetComponent<PersistentObjectId>();
                if (poid == null) poid = go.AddComponent<PersistentObjectId>();
                poid.Assign(ro.PersistentId);
            }
        }

        private static void EnsureEventSlots(InteractionRecording recording, InteractionEventBinder binder)
        {
            var events = recording.Events;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].HandlerId == UnityEventHandler.HandlerId && !string.IsNullOrEmpty(events[i].Key))
                    binder.EnsureSlot(events[i].Key);
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
