using UnityEngine;

namespace AjoyGames.MotionKit.Samples
{
    /// <summary>
    /// A simple animated interaction to record: the object bobs, spins and pulses an optional Light. Enter Play
    /// Mode so this drives the object, then capture it with the MotionKit Recorder window.
    /// </summary>
    [AddComponentMenu("MotionKit/Samples/Sample Interaction")]
    public sealed class SampleInteraction : MonoBehaviour
    {
        [SerializeField] private float _bobHeight = 0.5f;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _spinSpeed = 90f;
        [SerializeField] private Light _light;
        [SerializeField] private float _lightBase = 1f;
        [SerializeField] private float _lightPulse = 0.5f;

        private Vector3 _origin;

        private void Awake() { _origin = transform.localPosition; }

        private void Update()
        {
            float t = Time.time;
            transform.localPosition = _origin + Vector3.up * (Mathf.Sin(t * _bobSpeed) * _bobHeight);
            transform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
            if (_light != null)
                _light.intensity = _lightBase + Mathf.Abs(Mathf.Sin(t * _bobSpeed)) * _lightPulse;
        }
    }
}
