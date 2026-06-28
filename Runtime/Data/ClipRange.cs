using System;
using UnityEngine;

namespace AjoyGames.MotionKit.Data
{
    /// <summary>
    /// A named frame span used by multi-clip export. Users define ranges in the timeline editor (e.g. Idle,
    /// Reach, Grab, Release) and the asset builder bakes one <c>AnimationClip</c> per range.
    /// </summary>
    [Serializable]
    public struct ClipRange
    {
        [Tooltip("Output clip name (also the generated .anim file name).")]
        public string Name;

        [Tooltip("Inclusive start frame.")]
        public int StartFrame;

        [Tooltip("Inclusive end frame.")]
        public int EndFrame;

        public ClipRange(string name, int startFrame, int endFrame)
        {
            Name = name;
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        /// <summary>Number of frames spanned (inclusive).</summary>
        public int FrameCount { get { return Mathf.Max(0, EndFrame - StartFrame + 1); } }
    }

    /// <summary>How a recording resolves its objects at playback time.</summary>
    public enum BindingMode
    {
        /// <summary>Resolve by persistent GUID first, then hierarchy path.</summary>
        PersistentId = 0,

        /// <summary>Legacy recordings that only stored hierarchy paths.</summary>
        HierarchyPathOnly = 1
    }
}
