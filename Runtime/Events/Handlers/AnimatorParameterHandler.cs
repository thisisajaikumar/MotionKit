#if MOTIONKIT_ANIMATION
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Events.Handlers
{
    /// <summary>
    /// Re-applies recorded Animator parameter changes during playback. The parameter type is encoded in
    /// <see cref="RecordedEvent.IntPayload"/> (see <see cref="AnimatorParamKind"/>); the value is carried in
    /// <see cref="RecordedEvent.FloatPayload"/>.
    /// </summary>
    [EventHandlerModule(HandlerId)]
    public sealed class AnimatorParameterHandler : IRecordedEventHandler
    {
        /// <summary>Stable handler id persisted in recordings.</summary>
        public const string HandlerId = "animatorParam";

        private Animator _animator;

        string IRecordedEventHandler.HandlerId { get { return HandlerId; } }

        public void Prepare(InteractionEventBinder binder, GameObject root)
        {
            _animator = root != null ? root.GetComponentInChildren<Animator>(true) : null;
        }

        public void Invoke(in RecordedEvent recordedEvent)
        {
            if (_animator == null || string.IsNullOrEmpty(recordedEvent.Key)) return;

            switch ((AnimatorParamKind)recordedEvent.IntPayload)
            {
                case AnimatorParamKind.Trigger:
                    _animator.SetTrigger(recordedEvent.Key);
                    break;
                case AnimatorParamKind.Bool:
                    _animator.SetBool(recordedEvent.Key, recordedEvent.FloatPayload != 0f);
                    break;
                case AnimatorParamKind.Int:
                    _animator.SetInteger(recordedEvent.Key, Mathf.RoundToInt(recordedEvent.FloatPayload));
                    break;
                default:
                    _animator.SetFloat(recordedEvent.Key, recordedEvent.FloatPayload);
                    break;
            }
        }

        public void OnStop() { }
    }

    /// <summary>Animator parameter kinds, stored in <see cref="RecordedEvent.IntPayload"/>.</summary>
    public enum AnimatorParamKind
    {
        Float = 0,
        Int = 1,
        Bool = 2,
        Trigger = 3
    }
}
#endif
