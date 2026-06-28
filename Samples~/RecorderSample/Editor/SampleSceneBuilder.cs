using UnityEditor;
using UnityEngine;

namespace AjoyGames.MotionKit.Samples.Editor
{
    /// <summary>
    /// Builds a ready-to-record sample object in the active scene (a bobbing, spinning, pulsing cube). Avoids
    /// shipping a fragile serialized scene file; instead the sample is constructed deterministically on demand.
    /// </summary>
    public static class SampleSceneBuilder
    {
        [MenuItem("Tools/MotionKit/Create Sample Interaction Object")]
        public static void CreateSample()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "MotionKit Sample Interaction";
            root.transform.position = new Vector3(0f, 1f, 0f);

            var lightGo = new GameObject("Pulse Light");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.color = new Color(0.6f, 0.8f, 1f);

            var interaction = root.AddComponent<SampleInteraction>();
            var so = new SerializedObject(interaction);
            so.FindProperty("_light").objectReferenceValue = light;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create MotionKit Sample");
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            Debug.Log("[MotionKit] Sample created. Open Window ▸ MotionKit ▸ Recorder, click '+ Add Selected Objects', " +
                      "enter Play Mode, then Start/Stop recording and Save & Generate Assets.");
        }
    }
}
