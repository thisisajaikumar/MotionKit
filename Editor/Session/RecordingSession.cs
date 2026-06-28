using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Events;

namespace AjoyGames.MotionKit.Editor.Session
{
    /// <summary>
    /// A transient, in-editor recording that is never written to disk until the user explicitly saves. Wraps a
    /// non-asset <see cref="InteractionRecording"/> and provides snapshot-based undo/redo, a non-destructive
    /// trim range, event logging, and a commit path consumed by the asset builder.
    /// </summary>
    /// <remarks>
    /// Undo/redo snapshots use <see cref="EditorJsonUtility"/>, which honors <c>[SerializeReference]</c> so the
    /// polymorphic track list round-trips correctly.
    /// </remarks>
    public sealed class RecordingSession
    {
        private const int MaxUndoDepth = 64;

        private readonly Stack<string> _undo = new Stack<string>();
        private readonly Stack<string> _redo = new Stack<string>();

        /// <summary>The transient recording being edited.</summary>
        public InteractionRecording Recording { get; private set; }

        /// <summary>Non-destructive trim in-point (seconds).</summary>
        public float TrimStart { get; set; }

        /// <summary>Non-destructive trim out-point (seconds); 0 means "use full duration".</summary>
        public float TrimEnd { get; set; }

        /// <summary>True if a recording pass is currently appending into this session.</summary>
        public bool IsRecording { get; set; }

        /// <summary>True when there is at least one captured object or event.</summary>
        public bool HasContent
        {
            get
            {
                return Recording != null && (Recording.Objects.Count > 0 || Recording.Events.Count > 0);
            }
        }

        public bool CanUndo { get { return _undo.Count > 0; } }
        public bool CanRedo { get { return _redo.Count > 0; } }

        /// <summary>Creates a fresh session with an empty transient recording.</summary>
        public static RecordingSession Create(string name, int fps)
        {
            var rec = ScriptableObject.CreateInstance<InteractionRecording>();
            rec.hideFlags = HideFlags.HideAndDontSave;
            rec.RecordingName = string.IsNullOrEmpty(name) ? "New Recording" : name;
            rec.Fps = fps;
            rec.Version = RecordingVersion.CreateCurrent(Application.unityVersion);
            return new RecordingSession { Recording = rec };
        }

        /// <summary>Captures a snapshot for undo before a mutating edit.</summary>
        public void PushUndo()
        {
            if (Recording == null) return;
            _undo.Push(EditorJsonUtility.ToJson(Recording));
            if (_undo.Count > MaxUndoDepth)
            {
                var keep = new List<string>(_undo);
                keep.RemoveAt(keep.Count - 1);
                _undo.Clear();
                for (int i = keep.Count - 1; i >= 0; i--) _undo.Push(keep[i]);
            }
            _redo.Clear();
        }

        /// <summary>Reverts the most recent edit.</summary>
        public void Undo()
        {
            if (_undo.Count == 0 || Recording == null) return;
            _redo.Push(EditorJsonUtility.ToJson(Recording));
            EditorJsonUtility.FromJsonOverwrite(_undo.Pop(), Recording);
            Recording.RecalculateDuration();
        }

        /// <summary>Re-applies an undone edit.</summary>
        public void Redo()
        {
            if (_redo.Count == 0 || Recording == null) return;
            _undo.Push(EditorJsonUtility.ToJson(Recording));
            EditorJsonUtility.FromJsonOverwrite(_redo.Pop(), Recording);
            Recording.RecalculateDuration();
        }

        /// <summary>Records a universal event at the given time.</summary>
        public void LogEvent(in RecordedEvent evt)
        {
            if (Recording != null) Recording.Events.Add(evt);
        }

        /// <summary>Finalizes timing/version metadata after a recording pass.</summary>
        public void FinalizeRecording()
        {
            if (Recording == null) return;
            Recording.RecalculateDuration();
            Recording.SortEvents();
            RecordingVersion v = Recording.Version;
            v.Touch();
            Recording.Version = v;
            if (TrimEnd <= 0f) TrimEnd = Recording.Duration;
        }

        /// <summary>The effective trimmed end (full duration when no out-point is set).</summary>
        public float EffectiveTrimEnd
        {
            get { return TrimEnd > 0f ? TrimEnd : (Recording != null ? Recording.Duration : 0f); }
        }

        /// <summary>Discards the transient recording and clears history.</summary>
        public void Cancel()
        {
            if (Recording != null)
            {
                Object.DestroyImmediate(Recording);
                Recording = null;
            }
            _undo.Clear();
            _redo.Clear();
        }
    }
}
