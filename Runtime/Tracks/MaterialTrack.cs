using System;
using UnityEngine;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Records a Renderer's primary color (RGBA). Playback applies it through a MaterialPropertyBlock so no
    /// material instances are created. Color baking into clips is intentionally skipped because material
    /// bindings are unreliable; the recording drives it at runtime instead.
    /// </summary>
    [Serializable]
    public sealed class MaterialTrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.material";

        [SerializeField] private string _propertyName = "_BaseColor";

        /// <summary>Shader color property sampled/applied (e.g. _BaseColor for URP, _Color for built-in).</summary>
        public string PropertyName
        {
            get { return _propertyName; }
            set { _propertyName = value; }
        }

        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = null;
            property = null;
            return false;
        }
    }

    /// <summary>Samples a <see cref="Renderer"/> color each frame.</summary>
    public sealed class MaterialTrackRecorder : SampledTrackRecorder<MaterialTrackData>
    {
        private Renderer _renderer;
        private string _property;

        protected override int ChannelCount { get { return 4; } }

        protected override void CacheComponents()
        {
            _renderer = Target != null ? Target.GetComponent<Renderer>() : null;
            _property = "_BaseColor";
            Material mat = _renderer != null ? _renderer.sharedMaterial : null;
            if (mat != null && !mat.HasProperty(_property) && mat.HasProperty("_Color"))
                _property = "_Color";
        }

        protected override bool IsValid { get { return _renderer != null && _renderer.sharedMaterial != null; } }

        protected override void Sample(float[] buffer)
        {
            Color c = _renderer.sharedMaterial.HasProperty(_property)
                ? _renderer.sharedMaterial.GetColor(_property)
                : Color.white;
            buffer[0] = c.r; buffer[1] = c.g; buffer[2] = c.b; buffer[3] = c.a;
        }

        public override RecordedTrack EndRecord()
        {
            var data = (MaterialTrackData)base.EndRecord();
            data.PropertyName = _property;
            return data;
        }
    }

    /// <summary>Applies recorded color via a MaterialPropertyBlock.</summary>
    public sealed class MaterialTrackPlayer : SampledTrackPlayer<MaterialTrackData>
    {
        private Renderer _renderer;
        private int _propertyId;
        private MaterialPropertyBlock _block;

        protected override bool CacheComponents()
        {
            _renderer = Target.GetComponent<Renderer>();
            if (_renderer == null) return false;
            _propertyId = Shader.PropertyToID(string.IsNullOrEmpty(Data.PropertyName) ? "_BaseColor" : Data.PropertyName);
            _block = new MaterialPropertyBlock();
            return true;
        }

        protected override void Apply(float[] values)
        {
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(_propertyId, new Color(values[0], values[1], values[2], values[3]));
            _renderer.SetPropertyBlock(_block);
        }
    }

    /// <summary>Track module for <see cref="MaterialTrackData"/>.</summary>
    [TrackModule(Order = 30)]
    public sealed class MaterialTrackModule : TrackModuleBase<MaterialTrackData, MaterialTrackRecorder, MaterialTrackPlayer>
    {
        public MaterialTrackModule() : base(typeof(Renderer)) { }
        public override string Id { get { return MaterialTrackData.TypeId; } }
        public override string DisplayName { get { return "Material"; } }
    }
}
