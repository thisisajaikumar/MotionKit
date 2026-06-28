using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Editor.AssetGeneration;
using AjoyGames.MotionKit.Editor.CodeGen;

namespace AjoyGames.MotionKit.Editor.Windows
{
    /// <summary>
    /// One-click setup: creates the four built-in recorder profiles (Mobile / Desktop / VR / WebGL), ensures
    /// the output folder exists and regenerates the module registry. Lowers the barrier for new users.
    /// </summary>
    public sealed class SetupWizard : EditorWindow
    {
        private string _profilesFolder = "Assets/MotionKit/Profiles";
        private string _outputFolder = "Assets/Recordings";

        [MenuItem("Tools/MotionKit/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<SetupWizard>("MotionKit Setup");
            window.minSize = new Vector2(420, 220);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MotionKit Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Creates built-in recorder profiles, the output folder, and (re)generates the module registry.",
                MessageType.Info);
            EditorGUILayout.Space();

            _profilesFolder = EditorGUILayout.TextField("Profiles Folder", _profilesFolder);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Built-In Profiles", GUILayout.Height(26)))
                CreateProfiles();
            if (GUILayout.Button("Ensure Output Folder", GUILayout.Height(22)))
                AssetPathUtil.EnsureFolder(_outputFolder);
            if (GUILayout.Button("Regenerate Module Registry", GUILayout.Height(22)))
                RegistryGenerator.Generate(true);
            EditorGUILayout.Space();
            if (GUILayout.Button("Run All", GUILayout.Height(28)))
            {
                CreateProfiles();
                AssetPathUtil.EnsureFolder(_outputFolder);
                RegistryGenerator.Generate(false);
            }
        }

        private void CreateProfiles()
        {
            string folder = AssetPathUtil.EnsureFolder(_profilesFolder);
            foreach (BuiltInProfile preset in System.Enum.GetValues(typeof(BuiltInProfile)))
            {
                string path = folder + "/" + preset + "Profile.asset";
                RecorderProfile profile = AssetDatabase.LoadAssetAtPath<RecorderProfile>(path);
                bool isNew = profile == null;
                if (isNew) profile = ScriptableObject.CreateInstance<RecorderProfile>();
                profile.ApplyPreset(preset);
                if (isNew) AssetDatabase.CreateAsset(profile, path);
                else EditorUtility.SetDirty(profile);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MotionKit] Built-in recorder profiles created/updated in " + folder);
        }
    }
}
