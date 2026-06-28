using UnityEngine;
using AjoyGames.MotionKit.Data;

namespace AjoyGames.MotionKit.Components
{
    /// <summary>
    /// Thin holder linking a prefab (or any GameObject) to its source <see cref="InteractionRecording"/> and
    /// optional metadata asset. Lets tooling and custom builders find the recording without a hard dependency
    /// on <see cref="InteractionPlayer"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MotionKit/Interaction Recording Reference")]
    public sealed class InteractionRecordingReference : MonoBehaviour
    {
        [SerializeField] private InteractionRecording _recording;
        [SerializeField] private Object _metadata;

        /// <summary>The recording this object plays / was generated from.</summary>
        public InteractionRecording Recording
        {
            get { return _recording; }
            set { _recording = value; }
        }

        /// <summary>Optional generated metadata asset.</summary>
        public Object Metadata
        {
            get { return _metadata; }
            set { _metadata = value; }
        }
    }
}
