using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Editor.AssetGeneration
{
    /// <summary>
    /// Bakes <see cref="AnimationClip"/>s from a recording. Any track implementing <see cref="IBakableTrack"/>
    /// contributes curves, so new bakable tracks require no changes here. Supports single-clip output and
    /// multi-clip export driven by the recording's <see cref="ClipRange"/>s.
    /// </summary>
    public static class AnimationClipBaker
    {
        /// <summary>A baked clip together with its output file name (without extension).</summary>
        public readonly struct BakedClip
        {
            public readonly string Name;
            public readonly AnimationClip Clip;
            public BakedClip(string name, AnimationClip clip) { Name = name; Clip = clip; }
        }

        /// <summary>
        /// Bakes one clip per <see cref="ClipRange"/> when ranges exist and <paramref name="multiClip"/> is true,
        /// otherwise a single clip spanning the full recording.
        /// </summary>
        public static List<BakedClip> Bake(InteractionRecording recording, bool multiClip)
        {
            var result = new List<BakedClip>();
            if (recording == null) return result;

            if (multiClip && recording.ClipRanges.Count > 0)
            {
                float fps = Mathf.Max(1, recording.Fps);
                for (int i = 0; i < recording.ClipRanges.Count; i++)
                {
                    ClipRange range = recording.ClipRanges[i];
                    float start = range.StartFrame / fps;
                    float end = range.EndFrame / fps;
                    string name = string.IsNullOrEmpty(range.Name) ? ("Clip_" + i) : range.Name;
                    result.Add(new BakedClip(AssetPathUtil.Sanitize(name), BakeClip(recording, start, end)));
                }
            }
            else
            {
                string name = string.IsNullOrEmpty(recording.RecordingName) ? "Recording" : recording.RecordingName;
                result.Add(new BakedClip(AssetPathUtil.Sanitize(name), BakeClip(recording, 0f, recording.Duration)));
            }

            return result;
        }

        /// <summary>Bakes a single clip covering [<paramref name="start"/>, <paramref name="end"/>] seconds.</summary>
        public static AnimationClip BakeClip(InteractionRecording recording, float start, float end)
        {
            var clip = new AnimationClip
            {
                frameRate = Mathf.Max(1, recording.Fps),
                name = recording.RecordingName
            };

            List<RecordedObject> objects = recording.Objects;
            for (int o = 0; o < objects.Count; o++)
            {
                RecordedObject ro = objects[o];
                string path = ro.HierarchyPath ?? string.Empty;
                List<RecordedTrack> tracks = ro.Tracks;
                for (int t = 0; t < tracks.Count; t++)
                {
                    IBakableTrack bakable = tracks[t] as IBakableTrack;
                    if (bakable != null && tracks[t].Enabled)
                        bakable.Bake(clip, path, start, end);
                }
            }

            clip.EnsureQuaternionContinuity();
            return clip;
        }
    }
}
