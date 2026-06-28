#if MOTIONKIT_PHYSICS
using System;
using UnityEngine;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Records a Rigidbody's linear and angular velocity. Pose is captured by the Transform track; this track
    /// restores momentum so a released/thrown object continues believably after playback hands control back to
    /// the physics engine.
    /// </summary>
    [Serializable]
    public sealed class PhysicsTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.physics";
        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = null;
            property = null;
            return false;
        }
    }

    /// <summary>Samples a <see cref="Rigidbody"/> each frame.</summary>
    public sealed class PhysicsTrackRecorder : SampledTrackRecorder<PhysicsTrackData>
    {
        private Rigidbody _body;
        protected override int ChannelCount { get { return 6; } }
        protected override void CacheComponents() { _body = Target != null ? Target.GetComponent<Rigidbody>() : null; }
        protected override bool IsValid { get { return _body != null; } }

        protected override void Sample(float[] buffer)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 v = _body.linearVelocity;
#else
            Vector3 v = _body.velocity;
#endif
            Vector3 w = _body.angularVelocity;
            buffer[0] = v.x; buffer[1] = v.y; buffer[2] = v.z;
            buffer[3] = w.x; buffer[4] = w.y; buffer[5] = w.z;
        }
    }

    /// <summary>Applies recorded velocities.</summary>
    public sealed class PhysicsTrackPlayer : SampledTrackPlayer<PhysicsTrackData>
    {
        private Rigidbody _body;
        protected override bool CacheComponents() { _body = Target.GetComponent<Rigidbody>(); return _body != null; }

        protected override void Apply(float[] values)
        {
            Vector3 v = new Vector3(values[0], values[1], values[2]);
#if UNITY_6000_0_OR_NEWER
            _body.linearVelocity = v;
#else
            _body.velocity = v;
#endif
            _body.angularVelocity = new Vector3(values[3], values[4], values[5]);
        }
    }

    /// <summary>Track module for <see cref="PhysicsTrackData"/>.</summary>
    [TrackModule(Order = 70)]
    public sealed class PhysicsTrackModule : TrackModuleBase<PhysicsTrackData, PhysicsTrackRecorder, PhysicsTrackPlayer>
    {
        public PhysicsTrackModule() : base(typeof(Rigidbody)) { }
        public override string Id { get { return PhysicsTrackData.TypeId; } }
        public override string DisplayName { get { return "Physics"; } }
    }
}
#endif
