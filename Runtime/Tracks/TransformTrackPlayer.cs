using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Applies recorded transform motion to a live Transform. Allocation-free: all arrays and the component
    /// reference are cached in <see cref="OnPrepare"/>, and evaluation uses a cursor-hinted keyframe search.
    /// </summary>
    public sealed class TransformTrackPlayer : TrackPlayerBase<TransformTrackData>
    {
        private Transform _transform;
        private float[] _times;
        private Vector3[] _positions;
        private Quaternion[] _rotations;
        private Vector3[] _scales;
        private int _count;
        private int _cursor;

        protected override bool OnPrepare()
        {
            _transform = Target.transform;
            _times = Data.Times;
            _positions = Data.Positions;
            _rotations = Data.Rotations;
            _scales = Data.Scales;
            _count = _times != null ? _times.Length : 0;
            _cursor = 0;
            return _count > 0;
        }

        public override void Evaluate(float time)
        {
            if (_count == 0 || _transform == null) return;

            int i = KeyframeSearch.FindLower(_times, _count, time, ref _cursor);
            float f = KeyframeSearch.SegmentT(_times, _count, i, time);
            int j = i + 1 < _count ? i + 1 : i;

            if (Data.HasPosition)
            {
                Vector3 p = Vector3.LerpUnclamped(_positions[i], _positions[j], f);
                if (Data.LocalSpace) _transform.localPosition = p;
                else _transform.position = p;
            }

            if (Data.HasRotation)
            {
                Quaternion q = Quaternion.SlerpUnclamped(_rotations[i], _rotations[j], f);
                if (Data.LocalSpace) _transform.localRotation = q;
                else _transform.rotation = q;
            }

            if (Data.HasScale)
            {
                _transform.localScale = Vector3.LerpUnclamped(_scales[i], _scales[j], f);
            }
        }
    }
}
