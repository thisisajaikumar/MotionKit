using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Data
{
    /// <summary>Preset compression strength controlling keyframe-reduction epsilons and half packing.</summary>
    public enum CompressionLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Aggressive = 4
    }

    /// <summary>
    /// Reusable recording configuration. Ship presets (Mobile / Desktop / VR / WebGL) and let users author
    /// unlimited custom profiles. A profile produces the <see cref="RecorderContext"/> consumed by recorders.
    /// </summary>
    [CreateAssetMenu(menuName = "MotionKit/Recorder Profile", fileName = "RecorderProfile")]
    public sealed class RecorderProfile : ScriptableObject
    {
        [Header("Sampling")]
        [Tooltip("Target capture rate in frames per second.")]
        [Min(1)] public int Fps = 60;

        [Header("Compression")]
        public CompressionLevel Compression = CompressionLevel.Medium;

        [Tooltip("Pack floats to half precision where the precision loss is acceptable.")]
        public bool UseHalfPrecision = false;

        [Header("Capture Scope")]
        [Tooltip("Also record child objects of each selected root.")]
        public bool CaptureChildren = true;

        [Tooltip("Include inactive GameObjects when capturing.")]
        public bool CaptureDisabledObjects = false;

        [Header("Auto Generation")]
        public bool AutoGeneratePrefab = true;
        public bool AutoGenerateAnimation = true;

        /// <summary>Builds the per-session sampling context, deriving epsilons from <see cref="Compression"/>.</summary>
        public RecorderContext CreateContext(int capacityHint)
        {
            float pos, rot, scalar;
            GetEpsilons(Compression, out pos, out rot, out scalar);
            return new RecorderContext(Fps, pos, rot, scalar, UseHalfPrecision, capacityHint);
        }

        /// <summary>Maps a compression level to position/rotation/scalar epsilons used for keyframe reduction.</summary>
        public static void GetEpsilons(CompressionLevel level, out float position, out float rotation, out float scalar)
        {
            switch (level)
            {
                case CompressionLevel.None:
                    position = 0f; rotation = 0f; scalar = 0f; break;
                case CompressionLevel.Low:
                    position = 0.0001f; rotation = 0.05f; scalar = 0.0001f; break;
                case CompressionLevel.Medium:
                    position = 0.001f; rotation = 0.25f; scalar = 0.001f; break;
                case CompressionLevel.High:
                    position = 0.005f; rotation = 0.5f; scalar = 0.005f; break;
                default: // Aggressive
                    position = 0.02f; rotation = 1.0f; scalar = 0.02f; break;
            }
        }

        /// <summary>Applies built-in preset values to this profile (used by the setup wizard).</summary>
        public void ApplyPreset(BuiltInProfile preset)
        {
            switch (preset)
            {
                case BuiltInProfile.Mobile:
                    Fps = 30; Compression = CompressionLevel.High; UseHalfPrecision = true; break;
                case BuiltInProfile.Desktop:
                    Fps = 60; Compression = CompressionLevel.Medium; UseHalfPrecision = false; break;
                case BuiltInProfile.VR:
                    Fps = 72; Compression = CompressionLevel.None; UseHalfPrecision = false; break;
                case BuiltInProfile.WebGL:
                    Fps = 20; Compression = CompressionLevel.Aggressive; UseHalfPrecision = true; break;
            }
        }
    }

    /// <summary>The four shipped built-in profile presets.</summary>
    public enum BuiltInProfile
    {
        Mobile,
        Desktop,
        VR,
        WebGL
    }
}
