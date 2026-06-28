using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Editor.Session
{
    /// <summary>
    /// Orchestrates a recording pass in the editor: stamps persistent ids, instantiates the right recorders for
    /// each captured object (discovered via <see cref="ModuleRegistry"/>), and drives fixed-rate sampling from
    /// <c>EditorApplication.update</c>. The result is appended into a <see cref="RecordingSession"/>.
    /// </summary>
    public sealed class RecordingController
    {
        private sealed class ActiveTarget
        {
            public RecordedObject Recorded;
            public GameObject GameObject;
            public readonly List<ITrackRecorder> Recorders = new List<ITrackRecorder>();
        }

        private readonly List<ActiveTarget> _targets = new List<ActiveTarget>();
        private readonly List<GameObject> _expandBuffer = new List<GameObject>();

        private RecordingSession _session;
        private RecorderProfile _profile;
        private TimeSampler _sampler;
        private double _lastUpdateTime;
        private bool _paused;

        /// <summary>True while a recording pass is running (possibly paused).</summary>
        public bool IsRecording { get; private set; }

        /// <summary>True while paused.</summary>
        public bool IsPaused { get { return _paused; } }

        /// <summary>Current recorded time in seconds.</summary>
        public float CurrentTime { get { return _sampler != null ? _sampler.Time : 0f; } }

        /// <summary>Begins a recording pass appending into <paramref name="session"/>.</summary>
        public void Start(RecordingSession session, RecorderProfile profile, ObjectSelectionModel selection,
            HashSet<string> enabledTrackIds)
        {
            if (IsRecording) return;
            _session = session;
            _profile = profile != null ? profile : ScriptableObject.CreateInstance<RecorderProfile>();
            _sampler = new TimeSampler(_profile.Fps);
            _sampler.Reset();
            _targets.Clear();

            BuildTargets(selection, enabledTrackIds);

            for (int i = 0; i < _targets.Count; i++)
                for (int r = 0; r < _targets[i].Recorders.Count; r++)
                {
                    _targets[i].Recorders[r].BeginRecord(0f);
                    _targets[i].Recorders[r].RecordFrame(0f);
                }
            _sampler.SkipInitialSample();

            _paused = false;
            IsRecording = true;
            _session.IsRecording = true;
            _lastUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;

            if (MotionKitDebug.Verbose)
            {
                int recorders = 0;
                for (int i = 0; i < _targets.Count; i++) recorders += _targets[i].Recorders.Count;
                MotionKitDebug.Log("Recording started — selected roots: " + selection.Roots.Count +
                                   ", capture targets: " + _targets.Count + ", recorders: " + recorders);
            }
        }

        /// <summary>Pauses sampling without ending the pass.</summary>
        public void Pause() { _paused = true; }

        /// <summary>Resumes sampling.</summary>
        public void Resume()
        {
            _paused = false;
            _lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>Ends the pass, finalizing all recorders into the session.</summary>
        public void Stop()
        {
            if (!IsRecording) return;
            EditorApplication.update -= OnEditorUpdate;
            IsRecording = false;
            _paused = false;

            int objectsAdded = 0;
            int tracksAdded = 0;
            for (int i = 0; i < _targets.Count; i++)
            {
                ActiveTarget target = _targets[i];
                for (int r = 0; r < target.Recorders.Count; r++)
                {
                    RecordedTrack track = target.Recorders[r].EndRecord();
                    if (track != null && track.KeyframeCount > 0)
                    {
                        track.Label = track.TrackTypeId;
                        target.Recorded.Tracks.Add(track);
                        tracksAdded++;
                    }
                }
                if (target.Recorded.Tracks.Count > 0)
                {
                    _session.Recording.Objects.Add(target.Recorded);
                    objectsAdded++;
                }
            }

            _targets.Clear();
            _session.IsRecording = false;
            _session.FinalizeRecording();

            MotionKitDebug.Log("Recording stopped — recorded objects: " + objectsAdded +
                               ", tracks: " + tracksAdded + " (total objects in asset: " +
                               _session.Recording.Objects.Count + ")");
        }

        private void BuildTargets(ObjectSelectionModel selection, HashSet<string> enabledTrackIds)
        {
            IReadOnlyList<GameObject> roots = selection.Roots;
            for (int ri = 0; ri < roots.Count; ri++)
            {
                GameObject root = roots[ri];
                if (root == null) continue;

                ExpandRoot(root, selection, _expandBuffer);
                Transform rootTransform = root.transform;

                for (int oi = 0; oi < _expandBuffer.Count; oi++)
                {
                    GameObject go = _expandBuffer[oi];
                    var recorded = new RecordedObject
                    {
                        DisplayName = go.name,
                        PersistentId = StampPersistentId(go),
                        HierarchyPath = ComputeFullPath(rootTransform, go.transform)
                    };

                    var target = new ActiveTarget { Recorded = recorded, GameObject = go };
                    RecorderContext context = _profile.CreateContext(256);

                    IReadOnlyList<ITrackModule> modules = ModuleRegistry.TrackModules;
                    for (int mi = 0; mi < modules.Count; mi++)
                    {
                        ITrackModule module = modules[mi];
                        if (enabledTrackIds != null && !enabledTrackIds.Contains(module.Id)) continue;
                        if (!module.CanRecord(go)) continue;

                        ITrackRecorder recorder = module.CreateRecorder();
                        recorder.Initialize(go, context);
                        target.Recorders.Add(recorder);
                    }

                    if (target.Recorders.Count > 0)
                        _targets.Add(target);
                }
            }
        }

        private void ExpandRoot(GameObject root, ObjectSelectionModel selection, List<GameObject> result)
        {
            result.Clear();
            if (!selection.CaptureChildren)
            {
                if (selection.CaptureDisabledObjects || root.activeInHierarchy)
                    result.Add(root);
                return;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(selection.CaptureDisabledObjects);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject go = transforms[i].gameObject;
                if (selection.CaptureDisabledObjects || go.activeInHierarchy)
                    result.Add(go);
            }
        }

        // Full path relative to the playback container: includes the selected root's own name as the first
        // segment (e.g. "Machine", "Machine/Door"), so paths resolve consistently when every selected root is
        // re-parented under one container prefab, and so generated AnimationClip bindings are correct.
        private static string ComputeFullPath(Transform root, Transform target)
        {
            if (target == root)
                return root.name;
            return root.name + "/" + BindingResolver.ComputePath(root, target);
        }

        private static string StampPersistentId(GameObject go)
        {
            PersistentObjectId poid = go.GetComponent<PersistentObjectId>();
            if (poid == null)
                poid = Undo.AddComponent<PersistentObjectId>(go);
            string id = poid.EnsureId();
            EditorUtility.SetDirty(poid);
            return id;
        }

        private void OnEditorUpdate()
        {
            if (!IsRecording || _paused) return;

            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastUpdateTime);
            _lastUpdateTime = now;

            _sampler.AddTime(delta);
            float t;
            while (_sampler.TryConsumeSample(out t))
            {
                for (int i = 0; i < _targets.Count; i++)
                {
                    ActiveTarget target = _targets[i];
                    if (target.GameObject == null) continue;
                    for (int r = 0; r < target.Recorders.Count; r++)
                        target.Recorders[r].RecordFrame(t);
                }
            }
        }
    }
}
