using UnityEngine;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Fixed-rate sampling clock shared by editor and play-mode recording. The driver feeds it raw delta time
    /// and pulls quantized sample timestamps, guaranteeing an even, fps-locked cadence regardless of the
    /// driver's own update rate.
    /// </summary>
    public sealed class TimeSampler
    {
        private float _accumulator;
        private int _frameIndex;

        /// <summary>Seconds between samples (1 / fps).</summary>
        public float Interval { get; private set; }

        /// <summary>Most recently emitted sample time, in seconds.</summary>
        public float Time { get; private set; }

        /// <summary>Number of samples emitted so far.</summary>
        public int FrameIndex { get { return _frameIndex; } }

        /// <param name="fps">Target sampling rate (clamped to at least 1).</param>
        public TimeSampler(int fps)
        {
            Interval = 1f / Mathf.Max(1, fps);
        }

        /// <summary>Resets the clock to time zero. Call when (re)starting a recording.</summary>
        public void Reset()
        {
            _accumulator = 0f;
            _frameIndex = 0;
            Time = 0f;
        }

        /// <summary>
        /// Marks the t=0 sample as already taken (an initial pose captured at <c>BeginRecord</c>) so the first
        /// emitted sample is at <see cref="Interval"/> instead of duplicating time zero.
        /// </summary>
        public void SkipInitialSample()
        {
            _frameIndex = 1;
        }

        /// <summary>Accumulates elapsed time. Pair with repeated <see cref="TryConsumeSample"/> calls.</summary>
        public void AddTime(float deltaTime)
        {
            if (deltaTime > 0f)
                _accumulator += deltaTime;
        }

        /// <summary>Consumes one fixed step if enough time has accumulated, yielding the quantized sample time.</summary>
        public bool TryConsumeSample(out float sampleTime)
        {
            if (_accumulator + 1e-6f >= Interval)
            {
                _accumulator -= Interval;
                sampleTime = _frameIndex * Interval;
                Time = sampleTime;
                _frameIndex++;
                return true;
            }
            sampleTime = Time;
            return false;
        }
    }
}
