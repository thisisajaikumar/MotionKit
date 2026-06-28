#if MOTIONKIT_AUDIO
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Events.Handlers
{
    /// <summary>
    /// Re-fires recorded audio events (play / stop / volume / pitch) on the playback root's AudioSource. The
    /// action is stored in <see cref="RecordedEvent.IntPayload"/> and the value in
    /// <see cref="RecordedEvent.FloatPayload"/>.
    /// </summary>
    [EventHandlerModule(HandlerId)]
    public sealed class AudioEventHandler : IRecordedEventHandler
    {
        /// <summary>Stable handler id persisted in recordings.</summary>
        public const string HandlerId = "audio";

        private AudioSource _source;

        string IRecordedEventHandler.HandlerId { get { return HandlerId; } }

        public void Prepare(InteractionEventBinder binder, GameObject root)
        {
            _source = root != null ? root.GetComponentInChildren<AudioSource>(true) : null;
        }

        public void Invoke(in RecordedEvent recordedEvent)
        {
            if (_source == null) return;
            switch ((AudioAction)recordedEvent.IntPayload)
            {
                case AudioAction.Play: _source.Play(); break;
                case AudioAction.Stop: _source.Stop(); break;
                case AudioAction.SetVolume: _source.volume = recordedEvent.FloatPayload; break;
                case AudioAction.SetPitch: _source.pitch = recordedEvent.FloatPayload; break;
            }
        }

        public void OnStop()
        {
            if (_source != null && _source.isPlaying) _source.Stop();
        }
    }

    /// <summary>Audio actions, stored in <see cref="RecordedEvent.IntPayload"/>.</summary>
    public enum AudioAction
    {
        Play = 0,
        Stop = 1,
        SetVolume = 2,
        SetPitch = 3
    }
}
#endif
