using System;
using UnityEngine;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>Records a Camera's field of view and clip planes (pose comes from the Transform track).</summary>
    [Serializable]
    public sealed class CameraTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.camera";
        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = typeof(Camera);
            switch (channel)
            {
                case 0: property = "field of view"; return true;
                case 1: property = "near clip plane"; return true;
                case 2: property = "far clip plane"; return true;
                default: property = null; return false;
            }
        }
    }

    /// <summary>Samples a <see cref="Camera"/> each frame.</summary>
    public sealed class CameraTrackRecorder : SampledTrackRecorder<CameraTrackData>
    {
        private Camera _camera;
        protected override int ChannelCount { get { return 3; } }
        protected override void CacheComponents() { _camera = Target != null ? Target.GetComponent<Camera>() : null; }
        protected override bool IsValid { get { return _camera != null; } }

        protected override void Sample(float[] buffer)
        {
            buffer[0] = _camera.fieldOfView;
            buffer[1] = _camera.nearClipPlane;
            buffer[2] = _camera.farClipPlane;
        }
    }

    /// <summary>Applies recorded camera state.</summary>
    public sealed class CameraTrackPlayer : SampledTrackPlayer<CameraTrackData>
    {
        private Camera _camera;
        protected override bool CacheComponents() { _camera = Target.GetComponent<Camera>(); return _camera != null; }

        protected override void Apply(float[] values)
        {
            _camera.fieldOfView = values[0];
            _camera.nearClipPlane = values[1];
            _camera.farClipPlane = values[2];
        }
    }

    /// <summary>Track module for <see cref="CameraTrackData"/>.</summary>
    [TrackModule(Order = 50)]
    public sealed class CameraTrackModule : TrackModuleBase<CameraTrackData, CameraTrackRecorder, CameraTrackPlayer>
    {
        public CameraTrackModule() : base(typeof(Camera)) { }
        public override string Id { get { return CameraTrackData.TypeId; } }
        public override string DisplayName { get { return "Camera"; } }
    }
}
