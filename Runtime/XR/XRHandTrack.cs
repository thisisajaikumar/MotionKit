#if MOTIONKIT_XR_HANDS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.XR
{
    /// <summary>
    /// REFERENCE / SCAFFOLD: records XR hand-tracking joint poses from the active <see cref="XRHandSubsystem"/>
    /// and replays them onto a rigged hand (the target's descendant transforms, in hierarchy order). Only
    /// compiles when <c>com.unity.xr.hands</c> is installed (define <c>MOTIONKIT_XR_HANDS</c>), so non-XR
    /// projects are unaffected.
    /// </summary>
    /// <remarks>
    /// XR package APIs evolve; validate this against your installed XR Hands version. Hand/controller motion of
    /// a rigged hierarchy is also capturable with the generic Transform track by selecting the rig root.
    /// </remarks>
    [System.Serializable]
    public sealed class XRHandTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.xr.hand";

        /// <summary>Joints captured per frame (Wrist + 5 fingers × joints ≈ 26).</summary>
        public const int JointCount = 26;

        /// <summary>Channels per joint: position (3) + rotation (4).</summary>
        public const int ChannelsPerJoint = 7;

        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out System.Type componentType, out string property)
        {
            componentType = null;
            property = null;
            return false;
        }
    }

    /// <summary>Samples hand-tracking joints each frame.</summary>
    public sealed class XRHandTrackRecorder : SampledTrackRecorder<XRHandTrackData>
    {
        private static readonly List<XRHandSubsystem> s_subsystems = new List<XRHandSubsystem>();
        private XRHandSubsystem _subsystem;

        protected override int ChannelCount { get { return XRHandTrackData.JointCount * XRHandTrackData.ChannelsPerJoint; } }

        protected override void CacheComponents()
        {
            s_subsystems.Clear();
            SubsystemManager.GetSubsystems(s_subsystems);
            _subsystem = s_subsystems.Count > 0 ? s_subsystems[0] : null;
        }

        protected override bool IsValid { get { return _subsystem != null; } }

        protected override void Sample(float[] buffer)
        {
            XRHand hand = _subsystem.rightHand;
            for (int i = 0; i < XRHandTrackData.JointCount; i++)
            {
                int b = i * XRHandTrackData.ChannelsPerJoint;
                Pose pose;
                XRHandJoint joint = hand.GetJoint(XRHandJointIDUtility.FromIndex(i));
                if (!joint.TryGetPose(out pose)) pose = Pose.identity;
                buffer[b + 0] = pose.position.x;
                buffer[b + 1] = pose.position.y;
                buffer[b + 2] = pose.position.z;
                buffer[b + 3] = pose.rotation.x;
                buffer[b + 4] = pose.rotation.y;
                buffer[b + 5] = pose.rotation.z;
                buffer[b + 6] = pose.rotation.w;
            }
        }
    }

    /// <summary>Applies recorded joint poses to the target's descendant transforms (in hierarchy order).</summary>
    public sealed class XRHandTrackPlayer : SampledTrackPlayer<XRHandTrackData>
    {
        private Transform[] _joints;

        protected override bool CacheComponents()
        {
            Transform[] all = Target.GetComponentsInChildren<Transform>(true);
            int count = Mathf.Min(all.Length - 1, XRHandTrackData.JointCount);
            if (count <= 0) return false;
            _joints = new Transform[count];
            for (int i = 0; i < count; i++) _joints[i] = all[i + 1];
            return true;
        }

        protected override void Apply(float[] values)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                int b = i * XRHandTrackData.ChannelsPerJoint;
                Transform t = _joints[i];
                if (t == null) continue;
                t.localPosition = new Vector3(values[b + 0], values[b + 1], values[b + 2]);
                t.localRotation = new Quaternion(values[b + 3], values[b + 4], values[b + 5], values[b + 6]);
            }
        }
    }

    /// <summary>Track module for <see cref="XRHandTrackData"/> (registered only when XR Hands is present).</summary>
    [TrackModule(Order = 100)]
    public sealed class XRHandTrackModule : TrackModuleBase<XRHandTrackData, XRHandTrackRecorder, XRHandTrackPlayer>
    {
        public override string Id { get { return XRHandTrackData.TypeId; } }
        public override string DisplayName { get { return "XR Hand"; } }
        public override bool CanRecord(GameObject target) { return target != null; }
    }
}
#endif
