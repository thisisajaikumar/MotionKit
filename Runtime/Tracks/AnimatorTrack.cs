#if MOTIONKIT_ANIMATION
using System;
using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Records an Animator's parameter values over time (floats, ints and bools; triggers are momentary and
    /// captured via the universal event system instead). Stores parameter names/types alongside the sampled
    /// channels so playback can re-apply them by name.
    /// </summary>
    [Serializable]
    public sealed class AnimatorTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.animator";

        [SerializeField] private string[] _paramNames = Array.Empty<string>();
        [SerializeField] private int[] _paramTypes = Array.Empty<int>(); // 0 float, 1 int, 2 bool

        public string[] ParamNames { get { return _paramNames; } }
        public int[] ParamTypes { get { return _paramTypes; } }

        public override string TrackTypeId { get { return TypeId; } }

        public void SetMetadata(string[] names, int[] types)
        {
            _paramNames = names ?? Array.Empty<string>();
            _paramTypes = types ?? Array.Empty<int>();
        }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = null;
            property = null;
            return false;
        }
    }

    /// <summary>Samples an <see cref="Animator"/>'s parameters each frame.</summary>
    public sealed class AnimatorTrackRecorder : SampledTrackRecorder<AnimatorTrackData>
    {
        private Animator _animator;
        private string[] _names;
        private int[] _types;
        private int[] _hashes;

        protected override int ChannelCount { get { EnsureParams(); return _names.Length; } }

        protected override void CacheComponents() { EnsureParams(); }

        protected override bool IsValid { get { return _animator != null && _names.Length > 0; } }

        private void EnsureParams()
        {
            if (_names != null) return;
            _animator = Target != null ? Target.GetComponent<Animator>() : null;

            var names = new List<string>();
            var types = new List<int>();
            var hashes = new List<int>();
            if (_animator != null)
            {
                AnimatorControllerParameter[] ps = _animator.parameters;
                for (int i = 0; i < ps.Length; i++)
                {
                    int type;
                    switch (ps[i].type)
                    {
                        case AnimatorControllerParameterType.Float: type = 0; break;
                        case AnimatorControllerParameterType.Int: type = 1; break;
                        case AnimatorControllerParameterType.Bool: type = 2; break;
                        default: continue; // skip triggers
                    }
                    names.Add(ps[i].name);
                    types.Add(type);
                    hashes.Add(ps[i].nameHash);
                }
            }
            _names = names.ToArray();
            _types = types.ToArray();
            _hashes = hashes.ToArray();
        }

        protected override void Sample(float[] buffer)
        {
            for (int i = 0; i < _names.Length; i++)
            {
                switch (_types[i])
                {
                    case 0: buffer[i] = _animator.GetFloat(_hashes[i]); break;
                    case 1: buffer[i] = _animator.GetInteger(_hashes[i]); break;
                    default: buffer[i] = _animator.GetBool(_hashes[i]) ? 1f : 0f; break;
                }
            }
        }

        public override RecordedTrack EndRecord()
        {
            var data = (AnimatorTrackData)base.EndRecord();
            data.SetMetadata(_names, _types);
            return data;
        }
    }

    /// <summary>Applies recorded Animator parameters.</summary>
    public sealed class AnimatorTrackPlayer : SampledTrackPlayer<AnimatorTrackData>
    {
        private Animator _animator;
        private int[] _hashes;

        protected override bool CacheComponents()
        {
            _animator = Target.GetComponent<Animator>();
            if (_animator == null) return false;
            string[] names = Data.ParamNames;
            _hashes = new int[names.Length];
            for (int i = 0; i < names.Length; i++)
                _hashes[i] = Animator.StringToHash(names[i]);
            return true;
        }

        protected override void Apply(float[] values)
        {
            int[] types = Data.ParamTypes;
            int count = Mathf.Min(values.Length, _hashes.Length);
            for (int i = 0; i < count; i++)
            {
                switch (types[i])
                {
                    case 0: _animator.SetFloat(_hashes[i], values[i]); break;
                    case 1: _animator.SetInteger(_hashes[i], Mathf.RoundToInt(values[i])); break;
                    default: _animator.SetBool(_hashes[i], values[i] > 0.5f); break;
                }
            }
        }
    }

    /// <summary>Track module for <see cref="AnimatorTrackData"/>.</summary>
    [TrackModule(Order = 20)]
    public sealed class AnimatorTrackModule : TrackModuleBase<AnimatorTrackData, AnimatorTrackRecorder, AnimatorTrackPlayer>
    {
        public AnimatorTrackModule() : base(typeof(Animator)) { }
        public override string Id { get { return AnimatorTrackData.TypeId; } }
        public override string DisplayName { get { return "Animator"; } }
    }
}
#endif
