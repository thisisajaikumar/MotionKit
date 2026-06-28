using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Events;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Playback
{
    /// <summary>
    /// Drives playback of an <see cref="InteractionRecording"/> onto a live hierarchy. Bindings, players and
    /// event handlers are resolved once in <see cref="Prepare"/>; <see cref="Tick"/> and <see cref="Evaluate"/>
    /// are allocation-free. Supports speed, loop, reverse, pause/resume, seeking and the universal event stream.
    /// </summary>
    public sealed class InteractionPlaybackEngine
    {
        private readonly BindingResolver _resolver = new BindingResolver();
        private readonly List<TrackBinding> _bindings = new List<TrackBinding>(32);
        private readonly Dictionary<string, IRecordedEventHandler> _handlers =
            new Dictionary<string, IRecordedEventHandler>(8, System.StringComparer.Ordinal);

        private readonly PlaybackState _state = new PlaybackState();

        private InteractionRecording _recording;
        private List<RecordedEvent> _events;
        private InteractionEventBinder _eventBinder;
        private float _duration;
        private int _eventCursor;
        private bool _prepared;

        /// <summary>Live playback state (time, speed, loop, reverse, flags).</summary>
        public PlaybackState State { get { return _state; } }

        /// <summary>Total duration of the prepared recording.</summary>
        public float Duration { get { return _duration; } }

        /// <summary>True while playing and not paused.</summary>
        public bool IsActive { get { return _prepared && _state.IsPlaying && !_state.IsPaused; } }

        /// <summary>True once a recording has been bound to a root.</summary>
        public bool IsPrepared { get { return _prepared; } }

        /// <summary>
        /// Resolves all bindings and event handlers for <paramref name="recording"/> under <paramref name="root"/>.
        /// Safe to call again to rebind. Returns false if nothing could be bound.
        /// </summary>
        public bool Prepare(InteractionRecording recording, GameObject root)
        {
            _bindings.Clear();
            _handlers.Clear();
            _prepared = false;

            if (recording == null || root == null)
            {
                Debug.LogError("[MotionKit] Cannot prepare playback: recording or root is null.");
                return false;
            }

            _resolver.Build(root);
            return FinishPrepare(recording, root);
        }

        /// <summary>
        /// Multi-root prepare for the editor preview, which drives the original (separate) scene objects rather
        /// than one container prefab. Binds every recorded object across all <paramref name="roots"/>
        /// (GUID-first), so multi-object recordings preview correctly.
        /// </summary>
        public bool Prepare(InteractionRecording recording, IReadOnlyList<GameObject> roots)
        {
            _bindings.Clear();
            _handlers.Clear();
            _prepared = false;

            if (recording == null)
            {
                Debug.LogError("[MotionKit] Cannot prepare playback: recording is null.");
                return false;
            }

            _resolver.Build(roots);
            return FinishPrepare(recording, FirstNonNull(roots));
        }

        private bool FinishPrepare(InteractionRecording recording, GameObject eventRoot)
        {
            Migration.RecordingMigrator.EnsureUpToDate(recording);

            _recording = recording;
            _duration = recording.Duration > 0f ? recording.Duration : recording.RecalculateDuration();

            BindTracks(recording);
            BindEvents(recording, eventRoot);

            _state.Time = 0f;
            _eventCursor = 0;
            _prepared = _bindings.Count > 0 || (_events != null && _events.Count > 0);

            if (!_prepared)
                Debug.LogWarning("[MotionKit] Recording '" + recording.RecordingName +
                                 "' produced no playable bindings.");

            MotionKitDebug.Log("Prepared playback — recorded objects: " + recording.Objects.Count +
                               ", track bindings: " + _bindings.Count + ", event handlers: " + _handlers.Count);
            return _prepared;
        }

        private static GameObject FirstNonNull(IReadOnlyList<GameObject> roots)
        {
            if (roots == null) return null;
            for (int i = 0; i < roots.Count; i++)
                if (roots[i] != null) return roots[i];
            return null;
        }

        private void BindTracks(InteractionRecording recording)
        {
            List<RecordedObject> objects = recording.Objects;
            for (int o = 0; o < objects.Count; o++)
            {
                RecordedObject ro = objects[o];
                GameObject target = _resolver.Resolve(ro, recording.BindingMode);
                if (target == null)
                {
                    Debug.LogWarning("[MotionKit] Could not resolve recorded object '" + ro.DisplayName +
                                     "' (id '" + ro.PersistentId + "', path '" + ro.HierarchyPath + "').");
                    continue;
                }

                List<RecordedTrack> tracks = ro.Tracks;
                for (int t = 0; t < tracks.Count; t++)
                {
                    RecordedTrack track = tracks[t];
                    if (track == null || !track.Enabled) continue;

                    ITrackPlayer player = ModuleRegistry.CreatePlayerFor(track);
                    if (player == null)
                    {
                        Debug.LogWarning("[MotionKit] No registered module for track type '" + track.TrackTypeId + "'. Skipped.");
                        continue;
                    }

                    if (player.Prepare(track, target))
                        _bindings.Add(new TrackBinding(player, target));
                }
            }
        }

        private void BindEvents(InteractionRecording recording, GameObject root)
        {
            _events = recording.Events;
            if (_events == null || _events.Count == 0) return;

            recording.SortEvents();
            _eventBinder = root != null ? root.GetComponentInChildren<InteractionEventBinder>(true) : null;

            for (int i = 0; i < _events.Count; i++)
            {
                string id = _events[i].HandlerId;
                if (string.IsNullOrEmpty(id) || _handlers.ContainsKey(id)) continue;

                IRecordedEventHandler handler = ModuleRegistry.CreateEventHandler(id);
                if (handler == null)
                {
                    Debug.LogWarning("[MotionKit] No event handler registered for id '" + id + "'. Its events are ignored.");
                    continue;
                }
                handler.Prepare(_eventBinder, root);
                _handlers[id] = handler;
            }
        }

        /// <summary>Begins (or restarts) playback from the current time.</summary>
        public void Play()
        {
            if (!_prepared) return;
            _state.IsPlaying = true;
            _state.IsPaused = false;
        }

        /// <summary>Pauses without unbinding; resume continues from the same time.</summary>
        public void Pause() { _state.IsPaused = true; }

        /// <summary>Resumes after a pause.</summary>
        public void Resume() { _state.IsPaused = false; }

        /// <summary>Stops playback, resets time and notifies players/handlers.</summary>
        public void Stop()
        {
            _state.ResetForStop();
            for (int i = 0; i < _bindings.Count; i++)
                _bindings[i].Player.OnStop();
            foreach (IRecordedEventHandler h in _handlers.Values)
                h.OnStop();
            _eventCursor = 0;
        }

        /// <summary>Sets the playback speed multiplier.</summary>
        public void SetSpeed(float speed) { _state.Speed = speed; }

        /// <summary>Enables or disables looping.</summary>
        public void SetLoop(bool loop) { _state.Loop = loop; }

        /// <summary>Enables or disables reverse playback.</summary>
        public void SetReverse(bool reverse) { _state.Reverse = reverse; }

        /// <summary>Jumps to a specific time and re-evaluates immediately (events are not fired on a seek).</summary>
        public void Seek(float time)
        {
            _state.Time = Mathf.Clamp(time, 0f, _duration);
            _eventCursor = FindEventCursor(_state.Time);
            Evaluate(_state.Time);
        }

        /// <summary>Advances playback by <paramref name="deltaTime"/> seconds, applying state and firing events.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsActive) return;

            float prev = _state.Time;
            float dir = _state.Reverse ? -1f : 1f;
            float next = prev + deltaTime * _state.Speed * dir;

            bool wrapped = false;
            if (next > _duration)
            {
                if (_state.Loop) { next -= _duration; wrapped = true; }
                else { next = _duration; }
            }
            else if (next < 0f)
            {
                if (_state.Loop) { next += _duration; wrapped = true; }
                else { next = 0f; }
            }

            _state.Time = next;
            Evaluate(next);

            if (!_state.Reverse)
            {
                if (wrapped)
                {
                    FireEvents(prev, _duration);
                    _eventCursor = 0;
                    FireEvents(0f, next);
                }
                else
                {
                    FireEvents(prev, next);
                }
            }

            if (!_state.Loop && ((next >= _duration && !_state.Reverse) || (next <= 0f && _state.Reverse)))
                _state.IsPlaying = false;
        }

        /// <summary>Applies every bound track at <paramref name="time"/>. Allocation-free.</summary>
        public void Evaluate(float time)
        {
            for (int i = 0; i < _bindings.Count; i++)
                _bindings[i].Player.Evaluate(time);
        }

        private void FireEvents(float fromExclusive, float toInclusive)
        {
            if (_events == null) return;
            while (_eventCursor < _events.Count)
            {
                RecordedEvent e = _events[_eventCursor];
                if (e.Time <= fromExclusive) { _eventCursor++; continue; }
                if (e.Time > toInclusive) break;
                IRecordedEventHandler handler;
                if (_handlers.TryGetValue(e.HandlerId, out handler))
                    handler.Invoke(in e);
                _eventCursor++;
            }
        }

        private int FindEventCursor(float time)
        {
            if (_events == null) return 0;
            int i = 0;
            while (i < _events.Count && _events[i].Time <= time) i++;
            return i;
        }
    }
}
