using UnityEngine;
using AjoyGames.MotionKit.Components;

namespace AjoyGames.MotionKit.Samples
{
    /// <summary>
    /// Spawns and plays a generated recording prefab at start, demonstrating the one-call runtime API. Assign a
    /// recording prefab (the <c>*.prefab</c> produced by the recorder) in the inspector.
    /// </summary>
    [AddComponentMenu("MotionKit/Samples/Sample Playback Demo")]
    public sealed class SamplePlaybackDemo : MonoBehaviour
    {
        [SerializeField] private GameObject _recordingPrefab;
        [SerializeField] private bool _loop = true;
        [SerializeField, Min(0.01f)] private float _speed = 1f;

        private void Start()
        {
            if (_recordingPrefab == null)
            {
                Debug.LogWarning("[MotionKit] SamplePlaybackDemo has no recording prefab assigned.", this);
                return;
            }

            InteractionPlayer player = InteractionPlayer.Play(_recordingPrefab, transform.position, transform.rotation);
            if (player != null)
            {
                player.SetLoopInstance(_loop);
                player.SetSpeedInstance(_speed);
            }
        }
    }
}
