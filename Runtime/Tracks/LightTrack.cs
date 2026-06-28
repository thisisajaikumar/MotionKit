using System;
using UnityEngine;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>Records a Light's intensity, color and enabled state.</summary>
    [Serializable]
    public sealed class LightTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.light";
        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = typeof(Light);
            switch (channel)
            {
                case 0: property = "m_Intensity"; return true;
                case 1: property = "m_Color.r"; return true;
                case 2: property = "m_Color.g"; return true;
                case 3: property = "m_Color.b"; return true;
                case 4: property = "m_Enabled"; return true;
                default: property = null; return false;
            }
        }
    }

    /// <summary>Samples a <see cref="Light"/> each frame.</summary>
    public sealed class LightTrackRecorder : SampledTrackRecorder<LightTrackData>
    {
        private Light _light;
        protected override int ChannelCount { get { return 5; } }
        protected override void CacheComponents() { _light = Target != null ? Target.GetComponent<Light>() : null; }
        protected override bool IsValid { get { return _light != null; } }

        protected override void Sample(float[] buffer)
        {
            buffer[0] = _light.intensity;
            Color c = _light.color;
            buffer[1] = c.r; buffer[2] = c.g; buffer[3] = c.b;
            buffer[4] = _light.enabled ? 1f : 0f;
        }
    }

    /// <summary>Applies recorded light state.</summary>
    public sealed class LightTrackPlayer : SampledTrackPlayer<LightTrackData>
    {
        private Light _light;
        protected override bool CacheComponents() { _light = Target.GetComponent<Light>(); return _light != null; }

        protected override void Apply(float[] values)
        {
            _light.intensity = values[0];
            _light.color = new Color(values[1], values[2], values[3], 1f);
            _light.enabled = values[4] > 0.5f;
        }
    }

    /// <summary>Track module for <see cref="LightTrackData"/>.</summary>
    [TrackModule(Order = 40)]
    public sealed class LightTrackModule : TrackModuleBase<LightTrackData, LightTrackRecorder, LightTrackPlayer>
    {
        public LightTrackModule() : base(typeof(Light)) { }
        public override string Id { get { return LightTrackData.TypeId; } }
        public override string DisplayName { get { return "Light"; } }
    }
}
