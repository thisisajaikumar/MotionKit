using System.Collections.Generic;
using UnityEngine;

namespace AjoyGames.MotionKit.Playback
{
    /// <summary>
    /// Lightweight object pool for spawned recording instances. Reuses deactivated instances to avoid
    /// Instantiate/Destroy churn and GC spikes when playing many recordings.
    /// </summary>
    public sealed class RecordingPool
    {
        /// <summary>Process-wide shared pool used by the static <c>InteractionPlayer</c> API.</summary>
        public static RecordingPool Shared { get; } = new RecordingPool();

        private readonly Dictionary<GameObject, Stack<GameObject>> _free =
            new Dictionary<GameObject, Stack<GameObject>>();

        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab =
            new Dictionary<GameObject, GameObject>();

        /// <summary>Retrieves an instance of <paramref name="prefab"/> (reused if available).</summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            GameObject instance = null;
            Stack<GameObject> stack;
            if (_free.TryGetValue(prefab, out stack))
            {
                while (stack.Count > 0 && instance == null)
                    instance = stack.Pop();
            }

            if (instance == null)
            {
                instance = Object.Instantiate(prefab, position, rotation, parent);
            }
            else
            {
                Transform t = instance.transform;
                t.SetParent(parent, false);
                t.SetPositionAndRotation(position, rotation);
                instance.SetActive(true);
            }

            _instanceToPrefab[instance] = prefab;
            return instance;
        }

        /// <summary>Returns an instance to its pool (deactivated). Falls back to Destroy if it is not tracked.</summary>
        public void Release(GameObject instance)
        {
            if (instance == null) return;

            GameObject prefab;
            if (!_instanceToPrefab.TryGetValue(instance, out prefab))
            {
                Object.Destroy(instance);
                return;
            }

            _instanceToPrefab.Remove(instance);
            instance.SetActive(false);

            Stack<GameObject> stack;
            if (!_free.TryGetValue(prefab, out stack))
            {
                stack = new Stack<GameObject>();
                _free[prefab] = stack;
            }
            stack.Push(instance);
        }

        /// <summary>Pre-instantiates <paramref name="count"/> inactive instances of <paramref name="prefab"/>.</summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0) return;
            Stack<GameObject> stack;
            if (!_free.TryGetValue(prefab, out stack))
            {
                stack = new Stack<GameObject>(count);
                _free[prefab] = stack;
            }
            for (int i = 0; i < count; i++)
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.SetActive(false);
                stack.Push(instance);
            }
        }

        /// <summary>Returns true if <paramref name="instance"/> originated from this pool.</summary>
        public bool IsPooled(GameObject instance)
        {
            return instance != null && _instanceToPrefab.ContainsKey(instance);
        }
    }
}
