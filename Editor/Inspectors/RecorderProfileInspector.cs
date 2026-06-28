using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit.Data;

namespace AjoyGames.MotionKit.Editor.Inspectors
{
    /// <summary>Inspector for <see cref="RecorderProfile"/> with quick preset buttons.</summary>
    [CustomEditor(typeof(RecorderProfile))]
    public sealed class RecorderProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Apply Preset", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (BuiltInProfile preset in System.Enum.GetValues(typeof(BuiltInProfile)))
            {
                if (GUILayout.Button(preset.ToString()))
                {
                    var profile = (RecorderProfile)target;
                    Undo.RecordObject(profile, "Apply Preset");
                    profile.ApplyPreset(preset);
                    EditorUtility.SetDirty(profile);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
