using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Editor.AssetGeneration;
using AjoyGames.MotionKit.Editor.Validation;

namespace AjoyGames.MotionKit.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="InteractionRecording"/>: shows a version/summary panel and repeatable asset
    /// actions (regenerate, rename, duplicate, delete) so derived assets can be rebuilt without re-recording.
    /// </summary>
    [CustomEditor(typeof(InteractionRecording))]
    public sealed class InteractionRecordingInspector : UnityEditor.Editor
    {
        private GameObject _sourceRoot;
        private bool _multiClip;
        private readonly List<ValidationMessage> _validation = new List<ValidationMessage>();

        public override void OnInspectorGUI()
        {
            var rec = (InteractionRecording)target;

            RecordingValidator.Validate(rec, _validation);
            for (int i = 0; i < _validation.Count; i++)
                EditorGUILayout.HelpBox(_validation[i].Text, _validation[i].Severity);
            if (_validation.Count > 0) EditorGUILayout.Space();

            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Name", rec.RecordingName);
            EditorGUILayout.LabelField("Duration", rec.Duration.ToString("0.000") + "s  (" +
                                       rec.FrameCount + " frames @ " + rec.Fps + " fps)");
            EditorGUILayout.LabelField("Objects / Events", rec.Objects.Count + " objects, " + rec.Events.Count + " events");
            EditorGUILayout.LabelField("Binding Mode", rec.BindingMode.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel);
            RecordingVersion v = rec.Version;
            EditorGUILayout.LabelField("File / Package", "v" + v.FileVersion + " · " + v.PackageVersion);
            EditorGUILayout.LabelField("Unity", v.UnityVersion);
            EditorGUILayout.LabelField("Created", v.CreatedDateUtc);
            EditorGUILayout.LabelField("Modified", v.LastModifiedDateUtc);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Regenerate Assets", EditorStyles.boldLabel);
            _sourceRoot = (GameObject)EditorGUILayout.ObjectField("Source Root (for prefab)", _sourceRoot, typeof(GameObject), true);
            _multiClip = EditorGUILayout.Toggle("Multi-Clip Export", _multiClip);
            if (GUILayout.Button("Regenerate"))
                Regenerate(rec);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rename")) Rename(rec);
            if (GUILayout.Button("Duplicate")) Duplicate(rec);
            if (GUILayout.Button("Delete")) Delete(rec);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(true))
                DrawDefaultInspector();
        }

        private void Regenerate(InteractionRecording rec)
        {
            string assetPath = AssetDatabase.GetAssetPath(rec);
            string perRecordingFolder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string outputFolder = Path.GetDirectoryName(perRecordingFolder).Replace('\\', '/');

            var settings = new AssetBuildSettings
            {
                OutputFolder = outputFolder,
                GeneratePrefab = _sourceRoot != null,
                GenerateAnimation = true,
                GeneratePreview = _sourceRoot != null,
                MultiClip = _multiClip
            };
            AssetBuilder.Build(rec, new[] { _sourceRoot }, settings);
        }

        private static void Rename(InteractionRecording rec)
        {
            rec.RecordingName = rec.RecordingName + "_Renamed";
            EditorUtility.SetDirty(rec);
            AssetDatabase.SaveAssets();
        }

        private static void Duplicate(InteractionRecording rec)
        {
            string path = AssetDatabase.GetAssetPath(rec);
            string copy = AssetDatabase.GenerateUniqueAssetPath(path);
            if (AssetDatabase.CopyAsset(path, copy))
            {
                AssetDatabase.Refresh();
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<InteractionRecording>(copy));
            }
        }

        private static void Delete(InteractionRecording rec)
        {
            if (EditorUtility.DisplayDialog("Delete Recording",
                    "Delete '" + rec.RecordingName + "'? This cannot be undone.", "Delete", "Cancel"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(rec));
            }
        }
    }
}
