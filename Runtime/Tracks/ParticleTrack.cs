#if MOTIONKIT_PARTICLES
using System;
using UnityEngine;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Records a ParticleSystem's emission on/off state so bursts replay at the right moments. Emission is
    /// toggled and the system is played/stopped to follow the recorded curve.
    /// </summary>
    [Serializable]
    public sealed class ParticleTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.particle";
        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = null;
            property = null;
            return false;
        }
    }

    /// <summary>Samples a <see cref="ParticleSystem"/>'s emission state each frame.</summary>
    public sealed class ParticleTrackRecorder : SampledTrackRecorder<ParticleTrackData>
    {
        private ParticleSystem _particles;
        protected override int ChannelCount { get { return 1; } }
        protected override void CacheComponents() { _particles = Target != null ? Target.GetComponent<ParticleSystem>() : null; }
        protected override bool IsValid { get { return _particles != null; } }

        protected override void Sample(float[] buffer)
        {
            buffer[0] = _particles.emission.enabled && _particles.isPlaying ? 1f : 0f;
        }
    }

    /// <summary>Applies recorded emission state.</summary>
    public sealed class ParticleTrackPlayer : SampledTrackPlayer<ParticleTrackData>
    {
        private ParticleSystem _particles;
        protected override bool CacheComponents() { _particles = Target.GetComponent<ParticleSystem>(); return _particles != null; }

        protected override void Apply(float[] values)
        {
            bool on = values[0] > 0.5f;
            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.enabled = on;
            if (on && !_particles.isPlaying) _particles.Play();
            else if (!on && _particles.isPlaying) _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>Track module for <see cref="ParticleTrackData"/>.</summary>
    [TrackModule(Order = 90)]
    public sealed class ParticleTrackModule : TrackModuleBase<ParticleTrackData, ParticleTrackRecorder, ParticleTrackPlayer>
    {
        public ParticleTrackModule() : base(typeof(ParticleSystem)) { }
        public override string Id { get { return ParticleTrackData.TypeId; } }
        public override string DisplayName { get { return "Particle"; } }
    }
}
#endif
