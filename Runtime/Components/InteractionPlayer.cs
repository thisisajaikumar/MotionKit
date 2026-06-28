using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Playback;

namespace AjoyGames.MotionKit.Components
{
    /// <summary>
    /// Runtime facade that plays an <see cref="InteractionRecording"/> on the hierarchy it lives on. Sits on
    /// generated recording prefabs and exposes both instance control and a static convenience API:
    /// <code>
    /// InteractionPlayer.Play(recordingPrefab);
    /// InteractionPlayer.SetSpeed(0.5f);
    /// InteractionPlayer.SetLoop(true);
    /// InteractionPlayer.Stop();
    /// </code>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MotionKit/Interaction Player")]
    public sealed class InteractionPlayer : MonoBehaviour
    {
        [SerializeField] private InteractionRecording _recording;
        [SerializeField] private bool _playOnAwake;
        [SerializeField] private bool _loop;
        [SerializeField] private bool _reverse;
        [SerializeField, Min(0f)] private float _speed = 1f;

        [Tooltip("When spawned via the static pooled API, return the instance to the pool when playback ends.")]
        [SerializeField] private bool _returnToPoolOnComplete = true;

        private readonly InteractionPlaybackEngine _engine = new InteractionPlaybackEngine();
        private bool _prepared;
        private bool _wasActive;
        private GameObject _poolPrefab;

        private static InteractionPlayer s_lastPlayed;

        /// <summary>The recording this player drives.</summary>
        public InteractionRecording Recording
        {
            get { return _recording; }
            set { _recording = value; _prepared = false; }
        }

        /// <summary>The underlying engine (advanced control, scrubbing, seeking).</summary>
        public InteractionPlaybackEngine Engine { get { return _engine; } }

        /// <summary>True while actively playing.</summary>
        public bool IsPlaying { get { return _engine.IsActive; } }

        private void Awake()
        {
            if (_playOnAwake)
                PlayInstance();
        }

        private void Update()
        {
            if (!_prepared) return;

            bool wasActive = _wasActive;
            _engine.Tick(Time.deltaTime);
            _wasActive = _engine.IsActive;

            if (wasActive && !_engine.IsActive && _returnToPoolOnComplete && _poolPrefab != null)
                ReturnToPool();
        }

        // ----- Instance API -----

        /// <summary>Ensures the engine is bound, then starts playback from the start.</summary>
        public void PlayInstance()
        {
            if (!EnsurePrepared()) return;
            _engine.Seek(0f);
            _engine.Play();
            _wasActive = true;
            s_lastPlayed = this;
        }

        /// <summary>Stops playback (resets to time 0).</summary>
        public void StopInstance()
        {
            if (_prepared) _engine.Stop();
            _wasActive = false;
        }

        /// <summary>Pauses playback.</summary>
        public void PauseInstance() { _engine.Pause(); }

        /// <summary>Resumes playback.</summary>
        public void ResumeInstance() { _engine.Resume(); }

        /// <summary>Sets the playback speed multiplier.</summary>
        public void SetSpeedInstance(float speed) { _speed = speed; _engine.SetSpeed(speed); }

        /// <summary>Enables/disables looping.</summary>
        public void SetLoopInstance(bool loop) { _loop = loop; _engine.SetLoop(loop); }

        /// <summary>Enables/disables reverse playback.</summary>
        public void SetReverseInstance(bool reverse) { _reverse = reverse; _engine.SetReverse(reverse); }

        /// <summary>Seeks to an absolute time and evaluates immediately.</summary>
        public void SeekInstance(float time)
        {
            if (EnsurePrepared()) _engine.Seek(time);
        }

        private bool EnsurePrepared()
        {
            if (_prepared) return true;
            if (_recording == null)
            {
                Debug.LogError("[MotionKit] InteractionPlayer on '" + name + "' has no recording assigned.", this);
                return false;
            }
            _prepared = _engine.Prepare(_recording, gameObject);
            _engine.SetSpeed(_speed);
            _engine.SetLoop(_loop);
            _engine.SetReverse(_reverse);
            return _prepared;
        }

        private void ReturnToPool()
        {
            GameObject prefab = _poolPrefab;
            _poolPrefab = null;
            _prepared = false;
            RecordingPool.Shared.Release(gameObject);
            _poolPrefab = prefab;
        }

        // ----- Static convenience API -----

        /// <summary>Spawns <paramref name="recordingPrefab"/> from the shared pool and plays it.</summary>
        public static InteractionPlayer Play(GameObject recordingPrefab)
        {
            return Play(recordingPrefab, Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>Spawns and plays a recording prefab at a given pose.</summary>
        public static InteractionPlayer Play(GameObject recordingPrefab, Vector3 position, Quaternion rotation,
            Transform parent = null)
        {
            if (recordingPrefab == null)
            {
                Debug.LogError("[MotionKit] InteractionPlayer.Play called with a null prefab.");
                return null;
            }

            GameObject instance = RecordingPool.Shared.Get(recordingPrefab, position, rotation, parent);
            InteractionPlayer player = instance.GetComponent<InteractionPlayer>();
            if (player == null)
            {
                Debug.LogError("[MotionKit] Prefab '" + recordingPrefab.name + "' has no InteractionPlayer component.", recordingPrefab);
                RecordingPool.Shared.Release(instance);
                return null;
            }

            player._poolPrefab = recordingPrefab;
            player._prepared = false;
            player.PlayInstance();
            return player;
        }

        /// <summary>Stops the most recently played instance.</summary>
        public static void Stop() { if (s_lastPlayed != null) s_lastPlayed.StopInstance(); }

        /// <summary>Pauses the most recently played instance.</summary>
        public static void Pause() { if (s_lastPlayed != null) s_lastPlayed.PauseInstance(); }

        /// <summary>Resumes the most recently played instance.</summary>
        public static void Resume() { if (s_lastPlayed != null) s_lastPlayed.ResumeInstance(); }

        /// <summary>Sets speed on the most recently played instance.</summary>
        public static void SetSpeed(float speed) { if (s_lastPlayed != null) s_lastPlayed.SetSpeedInstance(speed); }

        /// <summary>Sets loop on the most recently played instance.</summary>
        public static void SetLoop(bool loop) { if (s_lastPlayed != null) s_lastPlayed.SetLoopInstance(loop); }
    }
}
