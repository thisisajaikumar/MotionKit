using UnityEditor;
using UnityEngine;

namespace AjoyGames.MotionKit.Editor.Preview
{
    /// <summary>
    /// Best-effort thumbnail generator. Renders a GameObject into an off-screen preview scene and returns a
    /// readable <see cref="Texture2D"/>. Failures are swallowed (returns null) so asset generation never breaks
    /// because of preview issues.
    /// </summary>
    public static class PreviewRenderer
    {
        /// <summary>Renders <paramref name="target"/> to a square thumbnail of the given size, or null on failure.</summary>
        public static Texture2D Render(GameObject target, int size = 256)
        {
            if (target == null) return null;

            PreviewRenderUtility preview = null;
            GameObject instance = null;
            try
            {
                preview = new PreviewRenderUtility();
                instance = Object.Instantiate(target);
                instance.hideFlags = HideFlags.HideAndDontSave;

                Bounds bounds = ComputeBounds(instance);
                float radius = Mathf.Max(0.1f, bounds.extents.magnitude);
                float distance = radius * 3.2f + 0.5f;
                Vector3 dir = new Vector3(0.6f, 0.4f, -1f).normalized;
                Vector3 camPos = bounds.center - dir * distance;

                preview.camera.transform.position = camPos;
                preview.camera.transform.rotation = Quaternion.LookRotation(bounds.center - camPos, Vector3.up);
                preview.camera.nearClipPlane = 0.05f;
                preview.camera.farClipPlane = distance * 6f + 10f;
                preview.camera.fieldOfView = 30f;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(0.18f, 0.18f, 0.2f, 1f);

                if (preview.lights != null && preview.lights.Length > 0)
                {
                    preview.lights[0].intensity = 1.2f;
                    preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
                    if (preview.lights.Length > 1)
                    {
                        preview.lights[1].intensity = 0.6f;
                        preview.lights[1].transform.rotation = Quaternion.Euler(-20f, -120f, 0f);
                    }
                }

                preview.AddSingleGO(instance);

                var rect = new Rect(0, 0, size, size);
                preview.BeginStaticPreview(rect);
                preview.camera.Render();
                return preview.EndStaticPreview();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[MotionKit] Preview render failed: " + e.Message);
                return null;
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
                if (preview != null) preview.Cleanup();
            }
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }
    }
}
