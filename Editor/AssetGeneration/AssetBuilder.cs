using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Editor.Preview;

namespace AjoyGames.MotionKit.Editor.AssetGeneration
{
    /// <summary>Options controlling which artifacts the asset builder produces.</summary>
    public sealed class AssetBuildSettings
    {
        /// <summary>Project-relative parent folder (a per-recording subfolder is created beneath it).</summary>
        public string OutputFolder = "Assets/Recordings";

        public bool GeneratePrefab = true;
        public bool GenerateAnimation = true;
        public bool GeneratePreview = true;

        /// <summary>When true and the recording defines clip ranges, export one clip per range.</summary>
        public bool MultiClip = false;

        public int PreviewSize = 256;
    }

    /// <summary>Paths and references produced by an asset build.</summary>
    public sealed class AssetBuildResult
    {
        public InteractionRecording Recording;
        public string RecordingPath;
        public GameObject Prefab;
        public RecordingMetadata Metadata;
        public string PreviewPath;
        public readonly List<string> ClipPaths = new List<string>();
    }

    /// <summary>
    /// Repeatable asset-generation pipeline. Consumes an <see cref="InteractionRecording"/> (the source of
    /// truth) and produces Recording.asset, AnimationClip(s), a prefab, a preview and metadata. Can be re-run at
    /// any time to regenerate derived assets without re-recording; clip/metadata assets are overwritten in place
    /// so existing references stay intact.
    /// </summary>
    public static class AssetBuilder
    {
        /// <summary>Builds (or regenerates) all enabled artifacts for <paramref name="recording"/>.</summary>
        public static AssetBuildResult Build(InteractionRecording recording, IReadOnlyList<GameObject> sourceRoots,
            AssetBuildSettings settings)
        {
            if (recording == null)
            {
                Debug.LogError("[MotionKit] AssetBuilder.Build called with a null recording.");
                return null;
            }
            if (settings == null) settings = new AssetBuildSettings();

            var result = new AssetBuildResult();
            string folderName = AssetPathUtil.Sanitize(recording.RecordingName);
            string folder = AssetPathUtil.EnsureFolder(settings.OutputFolder.TrimEnd('/') + "/" + folderName);

            // 1. Recording.asset (source of truth).
            recording = SaveRecording(recording, folder + "/Recording.asset", result);

            // 2. Metadata (preview filled in below).
            RecordingMetadata metadata = MetadataAssetBuilder.Build(recording);

            GameObject previewRoot = FirstNonNull(sourceRoots);

            // 3. Preview.
            if (settings.GeneratePreview)
            {
                Texture2D tex = PreviewRenderer.Render(previewRoot, settings.PreviewSize);
                if (tex != null)
                {
                    string previewPath = folder + "/Preview.png";
                    File.WriteAllBytes(Path.GetFullPath(previewPath), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    AssetDatabase.ImportAsset(previewPath, ImportAssetOptions.ForceUpdate);
                    metadata.Preview = AssetDatabase.LoadAssetAtPath<Texture2D>(previewPath);
                    result.PreviewPath = previewPath;
                }
            }

            // 4. Animation clip(s).
            if (settings.GenerateAnimation)
            {
                List<AnimationClipBaker.BakedClip> clips = AnimationClipBaker.Bake(recording, settings.MultiClip);
                for (int i = 0; i < clips.Count; i++)
                {
                    string clipPath = folder + "/" + clips[i].Name + ".anim";
                    CreateOrReplace(clips[i].Clip, clipPath);
                    result.ClipPaths.Add(clipPath);
                }
            }

            // 5. Metadata asset.
            result.Metadata = CreateOrReplace(metadata, folder + "/Metadata.asset");

            // 6. Prefab (needs the source hierarchy).
            if (settings.GeneratePrefab)
            {
                string prefabPath = folder + "/" + folderName + ".prefab";
                result.Prefab = PrefabBuilder.Build(recording, sourceRoots, prefabPath, result.Metadata);
            }

            EditorUtility.SetDirty(recording);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MotionKit] Built assets for '" + recording.RecordingName + "' in '" + folder + "'.");
            return result;
        }

        private static GameObject FirstNonNull(IReadOnlyList<GameObject> roots)
        {
            if (roots == null) return null;
            for (int i = 0; i < roots.Count; i++)
                if (roots[i] != null) return roots[i];
            return null;
        }

        private static InteractionRecording SaveRecording(InteractionRecording recording, string path,
            AssetBuildResult result)
        {
            if (AssetDatabase.Contains(recording))
            {
                result.Recording = recording;
                result.RecordingPath = AssetDatabase.GetAssetPath(recording);
                EditorUtility.SetDirty(recording);
                return recording;
            }

            recording.hideFlags = HideFlags.None;
            InteractionRecording existing = AssetDatabase.LoadAssetAtPath<InteractionRecording>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(recording, existing);
                Object.DestroyImmediate(recording);
                result.Recording = existing;
                result.RecordingPath = path;
                return existing;
            }

            AssetDatabase.CreateAsset(recording, path);
            result.Recording = recording;
            result.RecordingPath = path;
            return recording;
        }

        private static T CreateOrReplace<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                if (asset != existing) Object.DestroyImmediate(asset);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
