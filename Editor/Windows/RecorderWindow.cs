using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Editor.AssetGeneration;
using AjoyGames.MotionKit.Editor.Session;
using AjoyGames.MotionKit.Editor.Timeline;
using AjoyGames.MotionKit.Playback;

namespace AjoyGames.MotionKit.Editor.Windows
{
    /// <summary>
    /// The MotionKit Recorder window. Drives the full workflow: select objects, choose a profile and tracks,
    /// record into a transient session, preview/scrub/trim on the built-in timeline, then save the recording
    /// asset and generate derived assets.
    /// </summary>
    public sealed class RecorderWindow : EditorWindow
    {
        private readonly RecordingController _controller = new RecordingController();
        private readonly ObjectSelectionModel _selection = new ObjectSelectionModel();
        private readonly TimelineEditorView _timeline = new TimelineEditorView();
        private readonly Dictionary<string, bool> _trackEnabled = new Dictionary<string, bool>();

        private RecordingSession _session;
        private RecorderProfile _profile;
        private string _outputFolder = "Assets/Recordings";
        private string _recordingName = "New Recording";
        private bool _multiClip;
        private bool _generatePrefab = true;
        private bool _generateAnimation = true;
        private bool _generatePreview = true;

        private Vector2 _scroll;
        private float _previewTime;
        private bool _previewPlaying;
        private InteractionPlaybackEngine _previewEngine;
        private double _lastTick;

        [MenuItem("Window/MotionKit/Recorder")]
        public static void ShowWindow()
        {
            var window = GetWindow<RecorderWindow>("MotionKit Recorder");
            window.minSize = new Vector2(420, 520);
        }

        private void OnEnable()
        {
            _lastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnTick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnTick;
            if (_controller.IsRecording) _controller.Stop();
        }

        private void OnTick()
        {
            double now = EditorApplication.timeSinceStartup;
            float dt = (float)(now - _lastTick);
            _lastTick = now;

            if (_controller.IsRecording)
            {
                Repaint();
                return;
            }

            if (_previewPlaying && _previewEngine != null)
            {
                _previewEngine.Tick(dt);
                _previewTime = _previewEngine.State.Time;
                if (!_previewEngine.IsActive) _previewPlaying = false;
                Repaint();
            }
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("MotionKit Recorder", EditorStyles.boldLabel);
            DrawRegistryStatus();
            EditorGUILayout.Space();

            DrawRecordingSettings();
            EditorGUILayout.Space();
            DrawObjectSelection();
            EditorGUILayout.Space();
            DrawTrackTypes();
            EditorGUILayout.Space();
            DrawRecordControls();
            EditorGUILayout.Space();
            DrawSessionAndTimeline();

            EditorGUILayout.EndScrollView();
        }

        private void DrawRegistryStatus()
        {
            if (ModuleRegistry.TrackModules.Count == 0)
                EditorGUILayout.HelpBox("No track modules registered yet. Run Tools ▸ MotionKit ▸ Regenerate Module Registry.",
                    MessageType.Warning);
        }

        private void DrawRecordingSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            _recordingName = EditorGUILayout.TextField("Recording Name", _recordingName);
            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (GUILayout.Button("…", GUILayout.Width(28)))
            {
                string abs = EditorUtility.OpenFolderPanel("Output Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs) && abs.StartsWith(Application.dataPath))
                    _outputFolder = "Assets" + abs.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();

            _profile = (RecorderProfile)EditorGUILayout.ObjectField("Profile", _profile, typeof(RecorderProfile), false);
            if (_profile == null)
                EditorGUILayout.HelpBox("No profile assigned — defaults (60 FPS, medium compression) will be used. " +
                    "Create presets via Tools ▸ MotionKit ▸ Setup Wizard.", MessageType.Info);

            using (new EditorGUI.IndentLevelScope())
            {
                _generatePrefab = EditorGUILayout.Toggle("Auto Generate Prefab", _generatePrefab);
                _generateAnimation = EditorGUILayout.Toggle("Auto Generate Animation", _generateAnimation);
                _generatePreview = EditorGUILayout.Toggle("Generate Preview", _generatePreview);
                _multiClip = EditorGUILayout.Toggle("Multi-Clip Export", _multiClip);
                _selection.CaptureChildren = EditorGUILayout.Toggle("Capture Children", _selection.CaptureChildren);
                _selection.CaptureDisabledObjects = EditorGUILayout.Toggle("Capture Disabled", _selection.CaptureDisabledObjects);
            }
        }

        private void DrawObjectSelection()
        {
            EditorGUILayout.LabelField("Objects To Record", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Add Selected Objects"))
                _selection.AddSelection(Selection.gameObjects);

            IReadOnlyList<GameObject> roots = _selection.Roots;
            for (int i = roots.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(roots[i], typeof(GameObject), true);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                    _selection.Remove(roots[i]);
                EditorGUILayout.EndHorizontal();
            }
            if (roots.Count == 0)
                EditorGUILayout.HelpBox("Select GameObjects in the scene and click Add.", MessageType.None);
        }

        private void DrawTrackTypes()
        {
            EditorGUILayout.LabelField("Track Types", EditorStyles.boldLabel);
            IReadOnlyList<SDK.ITrackModule> modules = ModuleRegistry.TrackModules;
            for (int i = 0; i < modules.Count; i++)
            {
                string id = modules[i].Id;
                bool on;
                if (!_trackEnabled.TryGetValue(id, out on)) on = true;
                _trackEnabled[id] = EditorGUILayout.ToggleLeft(modules[i].DisplayName, on);
            }
        }

        private void DrawRecordControls()
        {
            EditorGUILayout.LabelField("Record Controls", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            bool canStart = !_controller.IsRecording && _selection.Roots.Count > 0;
            using (new EditorGUI.DisabledScope(!canStart))
            {
                if (GUILayout.Button(_session == null ? "Start Recording" : "Continue Recording"))
                    StartRecording();
            }

            using (new EditorGUI.DisabledScope(!_controller.IsRecording))
            {
                if (!_controller.IsPaused)
                {
                    if (GUILayout.Button("Pause")) _controller.Pause();
                }
                else
                {
                    if (GUILayout.Button("Resume")) _controller.Resume();
                }
                if (GUILayout.Button("Stop")) StopRecording();
            }
            EditorGUILayout.EndHorizontal();

            if (_controller.IsRecording)
                EditorGUILayout.HelpBox("Recording… " + _controller.CurrentTime.ToString("0.00") + "s", MessageType.Info);
        }

        private void DrawSessionAndTimeline()
        {
            if (_session == null || !_session.HasContent) return;

            EditorGUILayout.LabelField("Session & Timeline", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_previewPlaying ? "Pause" : "Play", GUILayout.Width(60))) TogglePreview();
            if (GUILayout.Button("Stop", GUILayout.Width(60))) StopPreview();
            if (GUILayout.Button("◀", GUILayout.Width(28))) StepFrame(-1);
            if (GUILayout.Button("▶", GUILayout.Width(28))) StepFrame(1);
            using (new EditorGUI.DisabledScope(!_session.CanUndo))
                if (GUILayout.Button("Undo", GUILayout.Width(56))) { _session.Undo(); InvalidatePreview(); }
            using (new EditorGUI.DisabledScope(!_session.CanRedo))
                if (GUILayout.Button("Redo", GUILayout.Width(56))) { _session.Redo(); InvalidatePreview(); }
            EditorGUILayout.EndHorizontal();

            Rect rect = GUILayoutUtility.GetRect(100, 180, GUILayout.ExpandWidth(true));
            EditorGUI.BeginChangeCheck();
            float newTime = _timeline.Draw(rect, _session, _previewTime);
            if (EditorGUI.EndChangeCheck())
            {
                _previewTime = newTime;
                _previewPlaying = false;
                EnsurePreview();
                if (_previewEngine != null) _previewEngine.Seek(_previewTime);
            }

            EditorGUILayout.LabelField("Time: " + _previewTime.ToString("0.000") + "s  /  " +
                                       _session.Recording.Duration.ToString("0.000") + "s  (" +
                                       _session.Recording.FrameCount + " frames, " +
                                       _session.Recording.Objects.Count + " objects)", EditorStyles.miniLabel);

            DrawClipRanges();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save & Generate Assets", GUILayout.Height(28))) SaveAndGenerate();
            if (GUILayout.Button("Cancel Session", GUILayout.Width(120), GUILayout.Height(28))) CancelSession();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawClipRanges()
        {
            if (!_multiClip) return;
            EditorGUILayout.LabelField("Clip Ranges (Multi-Clip Export)", EditorStyles.miniBoldLabel);
            List<ClipRange> ranges = _session.Recording.ClipRanges;
            for (int i = 0; i < ranges.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                ClipRange r = ranges[i];
                r.Name = EditorGUILayout.TextField(r.Name, GUILayout.Width(120));
                r.StartFrame = EditorGUILayout.IntField(r.StartFrame, GUILayout.Width(60));
                r.EndFrame = EditorGUILayout.IntField(r.EndFrame, GUILayout.Width(60));
                ranges[i] = r;
                if (GUILayout.Button("✕", GUILayout.Width(24))) { ranges.RemoveAt(i); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Clip Range", GUILayout.Width(140)))
                ranges.Add(new ClipRange("Clip", 0, _session.Recording.FrameCount));
        }

        // ----- Actions -----

        private void StartRecording()
        {
            if (_session == null)
                _session = RecordingSession.Create(_recordingName, ResolveFps());

            var enabled = new HashSet<string>();
            foreach (KeyValuePair<string, bool> kv in _trackEnabled)
                if (kv.Value) enabled.Add(kv.Key);
            foreach (SDK.ITrackModule m in ModuleRegistry.TrackModules)
                if (!_trackEnabled.ContainsKey(m.Id)) enabled.Add(m.Id);

            _controller.Start(_session, _profile, _selection, enabled);
        }

        private void StopRecording()
        {
            _controller.Stop();
            _previewTime = 0f;
            InvalidatePreview();
        }

        private int ResolveFps() { return _profile != null ? _profile.Fps : 60; }

        private void TogglePreview()
        {
            if (_previewPlaying) { if (_previewEngine != null) _previewEngine.Pause(); _previewPlaying = false; }
            else { EnsurePreview(); if (_previewEngine != null) { _previewEngine.Play(); _previewPlaying = true; } }
        }

        private void StopPreview()
        {
            _previewPlaying = false;
            if (_previewEngine != null) _previewEngine.Stop();
            _previewTime = 0f;
            EnsurePreview();
            if (_previewEngine != null) _previewEngine.Seek(0f);
        }

        private void StepFrame(int dir)
        {
            _previewPlaying = false;
            int fps = _session.Recording.Fps;
            _previewTime = Mathf.Clamp(_previewTime + dir / (float)fps, 0f, _session.Recording.Duration);
            EnsurePreview();
            if (_previewEngine != null) _previewEngine.Seek(_previewTime);
        }

        private void EnsurePreview()
        {
            if (_session == null || _session.Recording == null || _selection.Roots.Count == 0) return;
            if (_previewEngine == null)
            {
                _previewEngine = new InteractionPlaybackEngine();
                _previewEngine.Prepare(_session.Recording, _selection.Roots);
            }
        }

        private void InvalidatePreview()
        {
            _previewEngine = null;
            _previewPlaying = false;
        }

        private void SaveAndGenerate()
        {
            if (_session == null || _session.Recording == null) return;

            _session.Recording.RecordingName = _recordingName;
            var settings = new AssetBuildSettings
            {
                OutputFolder = _outputFolder,
                GeneratePrefab = _generatePrefab,
                GenerateAnimation = _generateAnimation,
                GeneratePreview = _generatePreview,
                MultiClip = _multiClip
            };

            AssetBuildResult result = AssetBuilder.Build(_session.Recording, _selection.Roots, settings);
            if (result != null && result.Prefab != null)
                EditorGUIUtility.PingObject(result.Prefab);

            _session = null;
            InvalidatePreview();
        }

        private void CancelSession()
        {
            if (_session != null) _session.Cancel();
            _session = null;
            InvalidatePreview();
        }
    }
}
