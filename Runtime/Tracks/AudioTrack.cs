#if MOTIONKIT_AUDIO
using System;
using UnityEngine;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Records an AudioSource's continuous volume and pitch. Discrete play/stop events are best captured via
    /// the universal event system's audio handler; this track covers the continuous parameters.
    /// </summary>
    [Serializable]
    public sealed class AudioTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.audio";
        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = typeof(AudioSource);
            switch (channel)
            {
                case 0: property = "m_Volume"; return true;
                case 1: property = "m_Pitch"; return true;
                default: property = null; return false;
            }
        }
    }

    /// <summary>Samples an <see cref="AudioSource"/> each frame.</summary>
    public sealed class AudioTrackRecorder : SampledTrackRecorder<AudioTrackData>
    {
        private AudioSource _source;
        protected override int ChannelCount { get { return 2; } }
        protected override void CacheComponents() { _source = Target != null ? Target.GetComponent<AudioSource>() : null; }
        protected override bool IsValid { get { return _source != null; } }

        protected override void Sample(float[] buffer)
        {
            buffer[0] = _source.volume;
            buffer[1] = _source.pitch;
        }
    }

    /// <summary>Applies recorded audio parameters.</summary>
    public sealed class AudioTrackPlayer : SampledTrackPlayer<AudioTrackData>
    {
        private AudioSource _source;
        protected override bool CacheComponents() { _source = Target.GetComponent<AudioSource>(); return _source != null; }

        protected override void Apply(float[] values)
        {
            _source.volume = values[0];
            _source.pitch = values[1];
        }
    }

    /// <summary>Track module for <see cref="AudioTrackData"/>.</summary>
    [TrackModule(Order = 80)]
    public sealed class AudioTrackModule : TrackModuleBase<AudioTrackData, AudioTrackRecorder, AudioTrackPlayer>
    {
        public AudioTrackModule() : base(typeof(AudioSource)) { }
        public override string Id { get { return AudioTrackData.TypeId; } }
        public override string DisplayName { get { return "Audio"; } }
    }
}
#endif
