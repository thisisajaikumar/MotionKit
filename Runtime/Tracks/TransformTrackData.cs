using System;
using UnityEngine;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Recorded transform motion stored as a struct-of-arrays: one shared time array aligned with optional
    /// position, rotation and scale channels. Keyframe reduction is applied at record time so static objects
    /// collapse to a couple of keys.
    /// </summary>
    [Serializable]
    public sealed class TransformTrackData : RecordedTrack, SDK.IBakableTrack
    {
        /// <summary>Stable module/track identifier.</summary>
        public const string TypeId = "motionkit.transform";

        [SerializeField] private bool _localSpace = true;
        [SerializeField] private bool _hasPosition = true;
        [SerializeField] private bool _hasRotation = true;
        [SerializeField] private bool _hasScale = true;

        [SerializeField] private float[] _times = Array.Empty<float>();
        [SerializeField] private Vector3[] _positions = Array.Empty<Vector3>();
        [SerializeField] private Quaternion[] _rotations = Array.Empty<Quaternion>();
        [SerializeField] private Vector3[] _scales = Array.Empty<Vector3>();

        public bool LocalSpace { get { return _localSpace; } }
        public bool HasPosition { get { return _hasPosition; } }
        public bool HasRotation { get { return _hasRotation; } }
        public bool HasScale { get { return _hasScale; } }
        public float[] Times { get { return _times; } }
        public Vector3[] Positions { get { return _positions; } }
        public Quaternion[] Rotations { get { return _rotations; } }
        public Vector3[] Scales { get { return _scales; } }

        public override string TrackTypeId { get { return TypeId; } }
        public override float Duration { get { return _times.Length > 0 ? _times[_times.Length - 1] : 0f; } }
        public override int KeyframeCount { get { return _times.Length; } }

        /// <summary>Assigns finalized channel data (called by the recorder after keyframe reduction).</summary>
        public void SetData(bool localSpace, float[] times,
            Vector3[] positions, Quaternion[] rotations, Vector3[] scales)
        {
            _localSpace = localSpace;
            _times = times ?? Array.Empty<float>();
            _hasPosition = positions != null && positions.Length == _times.Length;
            _hasRotation = rotations != null && rotations.Length == _times.Length;
            _hasScale = scales != null && scales.Length == _times.Length;
            _positions = _hasPosition ? positions : Array.Empty<Vector3>();
            _rotations = _hasRotation ? rotations : Array.Empty<Quaternion>();
            _scales = _hasScale ? scales : Array.Empty<Vector3>();
        }

        /// <inheritdoc/>
        public void Bake(AnimationClip clip, string path, float startTime, float endTime)
        {
            if (_times.Length == 0) return;
            if (_hasPosition) BakeVector3(clip, path, "localPosition", _positions, startTime, endTime);
            if (_hasScale) BakeVector3(clip, path, "localScale", _scales, startTime, endTime);
            if (_hasRotation) BakeRotation(clip, path, startTime, endTime);
        }

        private void BakeVector3(AnimationClip clip, string path, string property, Vector3[] values,
            float startTime, float endTime)
        {
            var cx = new AnimationCurve();
            var cy = new AnimationCurve();
            var cz = new AnimationCurve();
            for (int i = 0; i < _times.Length; i++)
            {
                float t = _times[i];
                if (t < startTime || t > endTime) continue;
                float rt = t - startTime;
                cx.AddKey(rt, values[i].x);
                cy.AddKey(rt, values[i].y);
                cz.AddKey(rt, values[i].z);
            }
            clip.SetCurve(path, typeof(Transform), property + ".x", cx);
            clip.SetCurve(path, typeof(Transform), property + ".y", cy);
            clip.SetCurve(path, typeof(Transform), property + ".z", cz);
        }

        private void BakeRotation(AnimationClip clip, string path, float startTime, float endTime)
        {
            var cx = new AnimationCurve();
            var cy = new AnimationCurve();
            var cz = new AnimationCurve();
            var cw = new AnimationCurve();
            for (int i = 0; i < _times.Length; i++)
            {
                float t = _times[i];
                if (t < startTime || t > endTime) continue;
                float rt = t - startTime;
                Quaternion q = _rotations[i];
                cx.AddKey(rt, q.x);
                cy.AddKey(rt, q.y);
                cz.AddKey(rt, q.z);
                cw.AddKey(rt, q.w);
            }
            clip.SetCurve(path, typeof(Transform), "localRotation.x", cx);
            clip.SetCurve(path, typeof(Transform), "localRotation.y", cy);
            clip.SetCurve(path, typeof(Transform), "localRotation.z", cz);
            clip.SetCurve(path, typeof(Transform), "localRotation.w", cw);
        }
    }
}
