using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Samples a Transform every frame, then applies multi-channel keyframe reduction on finalize so that
    /// segments of constant or linear motion collapse to their endpoints.
    /// </summary>
    public sealed class TransformTrackRecorder : TrackRecorderBase<TransformTrackData>
    {
        private Transform _transform;
        private readonly List<float> _times = new List<float>(256);
        private readonly List<Vector3> _positions = new List<Vector3>(256);
        private readonly List<Quaternion> _rotations = new List<Quaternion>(256);
        private readonly List<Vector3> _scales = new List<Vector3>(256);

        protected override void OnInitialize()
        {
            _transform = Target != null ? Target.transform : null;
        }

        protected override void OnBeginRecord(float time)
        {
            _times.Clear();
            _positions.Clear();
            _rotations.Clear();
            _scales.Clear();
        }

        public override void RecordFrame(float time)
        {
            if (_transform == null) return;
            _times.Add(time);
            _positions.Add(_transform.localPosition);
            _rotations.Add(_transform.localRotation);
            _scales.Add(_transform.localScale);
        }

        public override RecordedTrack EndRecord()
        {
            var data = new TransformTrackData();
            int n = _times.Count;
            if (n == 0)
            {
                data.SetData(true, System.Array.Empty<float>(), null, null, null);
                return data;
            }

            List<int> kept = Reduce(Context.PositionEpsilon, Context.RotationEpsilon, Context.ScalarEpsilon);

            int k = kept.Count;
            var times = new float[k];
            var pos = new Vector3[k];
            var rot = new Quaternion[k];
            var scale = new Vector3[k];
            for (int i = 0; i < k; i++)
            {
                int src = kept[i];
                times[i] = _times[src];
                pos[i] = _positions[src];
                rot[i] = _rotations[src];
                scale[i] = _scales[src];
            }

            data.SetData(true, times, pos, rot, scale);
            return data;
        }

        private List<int> Reduce(float posEps, float rotEps, float scaleEps)
        {
            int n = _times.Count;
            var kept = new List<int>(n);
            kept.Add(0);
            if (n <= 2)
            {
                if (n == 2) kept.Add(1);
                return kept;
            }

            for (int i = 1; i < n - 1; i++)
            {
                int prev = kept[kept.Count - 1];
                int next = i + 1;
                if (!Representable(prev, i, next, posEps, rotEps, scaleEps))
                    kept.Add(i);
            }
            kept.Add(n - 1);
            return kept;
        }

        private bool Representable(int a, int i, int b, float posEps, float rotEps, float scaleEps)
        {
            float ta = _times[a];
            float tb = _times[b];
            float denom = tb - ta;
            float f = denom <= 0f ? 0f : (_times[i] - ta) / denom;

            Vector3 p = Vector3.LerpUnclamped(_positions[a], _positions[b], f);
            if (MaxComponentDelta(p, _positions[i]) > posEps) return false;

            Vector3 s = Vector3.LerpUnclamped(_scales[a], _scales[b], f);
            if (MaxComponentDelta(s, _scales[i]) > scaleEps) return false;

            Quaternion q = Quaternion.SlerpUnclamped(_rotations[a], _rotations[b], f);
            if (Quaternion.Angle(q, _rotations[i]) > rotEps) return false;

            return true;
        }

        private static float MaxComponentDelta(Vector3 a, Vector3 b)
        {
            float dx = Mathf.Abs(a.x - b.x);
            float dy = Mathf.Abs(a.y - b.y);
            float dz = Mathf.Abs(a.z - b.z);
            float m = dx > dy ? dx : dy;
            return m > dz ? m : dz;
        }
    }
}
