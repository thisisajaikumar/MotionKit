#if MOTIONKIT_UGUI
using System;
using UnityEngine;
using UnityEngine.UI;
using AjoyGames.MotionKit.SDK;
using AjoyGames.MotionKit.Tracks.Sampled;

namespace AjoyGames.MotionKit.Tracks
{
    /// <summary>
    /// Records UI state: CanvasGroup alpha, Graphic (Image/Text) color, and RectTransform anchored position and
    /// size. Channels with no matching component are recorded as defaults and skipped on playback.
    /// </summary>
    [Serializable]
    public sealed class UITrackData : SampledTrackData
    {
        public const string TypeId = "motionkit.ui";
        public override string TrackTypeId { get { return TypeId; } }

        protected override bool TryGetBakeBinding(int channel, out Type componentType, out string property)
        {
            componentType = null;
            property = null;
            return false;
        }
    }

    /// <summary>Samples CanvasGroup / Graphic / RectTransform each frame.</summary>
    public sealed class UITrackRecorder : SampledTrackRecorder<UITrackData>
    {
        private RectTransform _rect;
        private CanvasGroup _group;
        private Graphic _graphic;

        protected override int ChannelCount { get { return 9; } }

        protected override void CacheComponents()
        {
            _rect = Target != null ? Target.GetComponent<RectTransform>() : null;
            _group = Target != null ? Target.GetComponent<CanvasGroup>() : null;
            _graphic = Target != null ? Target.GetComponent<Graphic>() : null;
        }

        protected override bool IsValid { get { return _rect != null || _group != null || _graphic != null; } }

        protected override void Sample(float[] buffer)
        {
            buffer[0] = _group != null ? _group.alpha : 1f;
            Color c = _graphic != null ? _graphic.color : Color.white;
            buffer[1] = c.r; buffer[2] = c.g; buffer[3] = c.b; buffer[4] = c.a;
            Vector2 ap = _rect != null ? _rect.anchoredPosition : Vector2.zero;
            Vector2 sd = _rect != null ? _rect.sizeDelta : Vector2.zero;
            buffer[5] = ap.x; buffer[6] = ap.y; buffer[7] = sd.x; buffer[8] = sd.y;
        }
    }

    /// <summary>Applies recorded UI state.</summary>
    public sealed class UITrackPlayer : SampledTrackPlayer<UITrackData>
    {
        private RectTransform _rect;
        private CanvasGroup _group;
        private Graphic _graphic;

        protected override bool CacheComponents()
        {
            _rect = Target.GetComponent<RectTransform>();
            _group = Target.GetComponent<CanvasGroup>();
            _graphic = Target.GetComponent<Graphic>();
            return _rect != null || _group != null || _graphic != null;
        }

        protected override void Apply(float[] values)
        {
            if (_group != null) _group.alpha = values[0];
            if (_graphic != null) _graphic.color = new Color(values[1], values[2], values[3], values[4]);
            if (_rect != null)
            {
                _rect.anchoredPosition = new Vector2(values[5], values[6]);
                _rect.sizeDelta = new Vector2(values[7], values[8]);
            }
        }
    }

    /// <summary>Track module for <see cref="UITrackData"/>.</summary>
    [TrackModule(Order = 60)]
    public sealed class UITrackModule : TrackModuleBase<UITrackData, UITrackRecorder, UITrackPlayer>
    {
        public UITrackModule() : base(typeof(CanvasGroup), typeof(Graphic)) { }
        public override string Id { get { return UITrackData.TypeId; } }
        public override string DisplayName { get { return "UI"; } }
    }
}
#endif
