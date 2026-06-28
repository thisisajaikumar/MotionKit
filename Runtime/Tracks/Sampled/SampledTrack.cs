using System;
using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Tracks.Sampled
{
    /// <summary>One serialized scalar channel of a sampled track.</summary>
    [Serializable]
    public sealed class FloatChannel
    {
        [SerializeField] public float[] Values = Array.Empty<float>();
    }

    /// <summary>
    /// Reusable base for tracks that record a fixed set of scalar channels sharing one time axis. Provides
    /// shared storage, allocation-free evaluation and optional AnimationClip baking, so concrete tracks only
    /// describe how to read/write their component.
    /// </summary>
    [Serializable]
    public abstract class SampledTrackData : RecordedTrack, IBakableTrack
    {
        [SerializeField] protected float[] _times = Array.Empty<float>();
        [SerializeField] protected FloatChannel[] _channels = Array.Empty<FloatChannel>();

        /// <summary>Shared sample times.</summary>
        public float[] Times { get { return _times; } }

        /// <summary>Number of scalar channels.</summary>
        public int ChannelCount { get { return _channels.Length; } }

        /// <inheritdoc/>
        public override float Duration { get { return _times.Length > 0 ? _times[_times.Length - 1] : 0f; } }

        /// <inheritdoc/>
        public override int KeyframeCount { get { return _times.Length; } }

        /// <summary>Raw values for a channel.</summary>
        public float[] GetChannelValues(int index) { return _channels[index].Values; }

        /// <summary>Assigns finalized data (called by the recorder).</summary>
        public void SetData(float[] times, FloatChannel[] channels)
        {
            _times = times ?? Array.Empty<float>();
            _channels = channels ?? Array.Empty<FloatChannel>();
        }

        /// <summary>Evaluates all channels at <paramref name="time"/> into <paramref name="outBuffer"/>.</summary>
        public void Evaluate(float time, ref int cursor, float[] outBuffer)
        {
            int n = _times.Length;
            if (n == 0) return;
            int i = KeyframeSearch.FindLower(_times, n, time, ref cursor);
            float f = KeyframeSearch.SegmentT(_times, n, i, time);
            int j = i + 1 < n ? i + 1 : i;
            int count = Mathf.Min(_channels.Length, outBuffer.Length);
            for (int c = 0; c < count; c++)
            {
                float[] v = _channels[c].Values;
                outBuffer[c] = Mathf.LerpUnclamped(v[i], v[j], f);
            }
        }

        /// <inheritdoc/>
        public void Bake(AnimationClip clip, string path, float startTime, float endTime)
        {
            for (int c = 0; c < _channels.Length; c++)
            {
                Type componentType;
                string property;
                if (!TryGetBakeBinding(c, out componentType, out property))
                    continue;

                var curve = new AnimationCurve();
                float[] values = _channels[c].Values;
                for (int i = 0; i < _times.Length; i++)
                {
                    float t = _times[i];
                    if (t < startTime || t > endTime) continue;
                    curve.AddKey(t - startTime, values[i]);
                }
                clip.SetCurve(path, componentType, property, curve);
            }
        }

        /// <summary>Maps a channel to an AnimationClip binding, or returns false to skip baking it.</summary>
        protected abstract bool TryGetBakeBinding(int channel, out Type componentType, out string property);
    }

    /// <summary>
    /// Reusable recorder base for sampled tracks. Buffers per-channel samples and applies joint linear keyframe
    /// reduction (a frame is dropped only when every channel is representable by its neighbours).
    /// </summary>
    public abstract class SampledTrackRecorder<TData> : TrackRecorderBase<TData> where TData : SampledTrackData, new()
    {
        private readonly List<float> _times = new List<float>(256);
        private List<float>[] _channels;
        private float[] _buffer;
        private int _channelCount;

        protected override void OnInitialize()
        {
            _channelCount = Mathf.Max(1, ChannelCount);
            _channels = new List<float>[_channelCount];
            for (int i = 0; i < _channelCount; i++) _channels[i] = new List<float>(256);
            _buffer = new float[_channelCount];
            CacheComponents();
        }

        protected override void OnBeginRecord(float time)
        {
            _times.Clear();
            for (int i = 0; i < _channels.Length; i++) _channels[i].Clear();
        }

        public override void RecordFrame(float time)
        {
            if (!IsValid) return;
            Sample(_buffer);
            _times.Add(time);
            for (int c = 0; c < _channelCount; c++)
                _channels[c].Add(_buffer[c]);
        }

        public override RecordedTrack EndRecord()
        {
            var data = new TData();
            int n = _times.Count;
            if (n == 0)
            {
                data.SetData(Array.Empty<float>(), Array.Empty<FloatChannel>());
                return data;
            }

            List<int> kept = Reduce(Context.ScalarEpsilon);
            int k = kept.Count;

            var times = new float[k];
            var channels = new FloatChannel[_channelCount];
            for (int c = 0; c < _channelCount; c++)
                channels[c] = new FloatChannel { Values = new float[k] };

            for (int i = 0; i < k; i++)
            {
                int src = kept[i];
                times[i] = _times[src];
                for (int c = 0; c < _channelCount; c++)
                    channels[c].Values[i] = _channels[c][src];
            }

            data.SetData(times, channels);
            return data;
        }

        private List<int> Reduce(float epsilon)
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
                float ta = _times[prev], tb = _times[next];
                float denom = tb - ta;
                float f = denom <= 0f ? 0f : (_times[i] - ta) / denom;

                bool representable = true;
                for (int c = 0; c < _channelCount; c++)
                {
                    float interpolated = Mathf.LerpUnclamped(_channels[c][prev], _channels[c][next], f);
                    if (Mathf.Abs(interpolated - _channels[c][i]) > epsilon) { representable = false; break; }
                }
                if (!representable) kept.Add(i);
            }
            kept.Add(n - 1);
            return kept;
        }

        /// <summary>Number of channels this recorder writes.</summary>
        protected abstract int ChannelCount { get; }

        /// <summary>Cache the component(s) to sample.</summary>
        protected abstract void CacheComponents();

        /// <summary>True if the cached component is present and sampling can proceed.</summary>
        protected abstract bool IsValid { get; }

        /// <summary>Fill <paramref name="buffer"/> with the current channel values.</summary>
        protected abstract void Sample(float[] buffer);
    }

    /// <summary>Reusable player base for sampled tracks.</summary>
    public abstract class SampledTrackPlayer<TData> : TrackPlayerBase<TData> where TData : SampledTrackData
    {
        private float[] _buffer;
        private int _cursor;

        protected override bool OnPrepare()
        {
            _buffer = new float[Mathf.Max(1, Data.ChannelCount)];
            _cursor = 0;
            return Data.KeyframeCount > 0 && CacheComponents();
        }

        public override void Evaluate(float time)
        {
            Data.Evaluate(time, ref _cursor, _buffer);
            Apply(_buffer);
        }

        /// <summary>Cache the component(s) to drive. Return false to skip the binding.</summary>
        protected abstract bool CacheComponents();

        /// <summary>Apply interpolated channel <paramref name="values"/> to the component.</summary>
        protected abstract void Apply(float[] values);
    }
}
