using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit.Data;
using AjoyGames.MotionKit.Editor.Session;

namespace AjoyGames.MotionKit.Editor.Timeline
{
    /// <summary>
    /// Lightweight, self-contained timeline editor (independent of Unity Timeline). Draws a frame ruler,
    /// playhead scrubber, per-track keyframe rows, event markers and draggable trim handles, with horizontal
    /// zoom and frame snapping. Returns the (possibly user-changed) playhead time.
    /// </summary>
    public sealed class TimelineEditorView
    {
        private const float RulerHeight = 20f;
        private const float RowHeight = 18f;
        private const float HandleWidth = 6f;

        /// <summary>Pixels per second.</summary>
        public float Zoom = 120f;

        /// <summary>Left scroll offset, in seconds.</summary>
        public float ScrollSeconds;

        /// <summary>Snap scrubbing/trim to whole frames.</summary>
        public bool SnapToFrames = true;

        private int _dragMode; // 0 none, 1 playhead, 2 trim start, 3 trim end

        /// <summary>Draws the timeline and returns the resulting playhead time.</summary>
        public float Draw(Rect rect, RecordingSession session, float playhead)
        {
            InteractionRecording rec = session != null ? session.Recording : null;
            if (rec == null)
            {
                EditorGUI.LabelField(rect, "No recording.", EditorStyles.centeredGreyMiniLabel);
                return playhead;
            }

            float duration = Mathf.Max(0.0001f, rec.Duration);
            int fps = Mathf.Max(1, rec.Fps);

            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.16f));

            var rulerRect = new Rect(rect.x, rect.y, rect.width, RulerHeight);
            DrawRuler(rulerRect, duration, fps);

            float y = rect.y + RulerHeight + 2f;
            for (int o = 0; o < rec.Objects.Count; o++)
            {
                RecordedObject ro = rec.Objects[o];
                for (int t = 0; t < ro.Tracks.Count; t++)
                {
                    var rowRect = new Rect(rect.x, y, rect.width, RowHeight);
                    DrawTrackRow(rowRect, ro, ro.Tracks[t]);
                    y += RowHeight + 1f;
                    if (y > rect.yMax - RowHeight) break;
                }
            }

            DrawEventMarkers(rect, rulerRect, rec);
            DrawTrimHandles(rect, session);
            playhead = HandleInput(rect, session, playhead, duration, fps);
            DrawPlayhead(rect, playhead);

            return Mathf.Clamp(playhead, 0f, duration);
        }

        private void DrawRuler(Rect rulerRect, float duration, int fps)
        {
            EditorGUI.DrawRect(rulerRect, new Color(0.22f, 0.22f, 0.22f));
            float secondsVisible = rulerRect.width / Zoom;
            float step = ChooseStep(secondsVisible);
            GUIStyle style = EditorStyles.miniLabel;

            for (float t = Mathf.Ceil(ScrollSeconds / step) * step; t <= ScrollSeconds + secondsVisible; t += step)
            {
                float x = TimeToX(rulerRect, t);
                if (x < rulerRect.x || x > rulerRect.xMax) continue;
                EditorGUI.DrawRect(new Rect(x, rulerRect.y, 1f, rulerRect.height), new Color(0.35f, 0.35f, 0.35f));
                EditorGUI.LabelField(new Rect(x + 2f, rulerRect.y, 50f, rulerRect.height), t.ToString("0.0") + "s", style);
            }
        }

        private void DrawTrackRow(Rect rowRect, RecordedObject ro, RecordedTrack track)
        {
            EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.2f, 0.2f));
            EditorGUI.LabelField(new Rect(rowRect.x + 4f, rowRect.y, 160f, rowRect.height),
                ro.DisplayName + " · " + track.Label, EditorStyles.miniLabel);

            Color tick = track.Enabled ? new Color(0.4f, 0.7f, 1f) : new Color(0.4f, 0.4f, 0.4f);
            Tracks.TransformTrackData tt = track as Tracks.TransformTrackData;
            if (tt != null)
            {
                float[] times = tt.Times;
                for (int i = 0; i < times.Length; i++)
                {
                    float x = TimeToX(rowRect, times[i]);
                    if (x < rowRect.x + 170f || x > rowRect.xMax) continue;
                    EditorGUI.DrawRect(new Rect(x, rowRect.y + 3f, 2f, rowRect.height - 6f), tick);
                }
            }
            else
            {
                float x0 = TimeToX(rowRect, 0f);
                float x1 = TimeToX(rowRect, track.Duration);
                EditorGUI.DrawRect(new Rect(Mathf.Max(x0, rowRect.x + 170f), rowRect.y + 6f,
                    Mathf.Max(2f, x1 - x0), rowRect.height - 12f), tick);
            }
        }

        private void DrawEventMarkers(Rect rect, Rect rulerRect, InteractionRecording rec)
        {
            for (int i = 0; i < rec.Events.Count; i++)
            {
                float x = TimeToX(rulerRect, rec.Events[i].Time);
                if (x < rect.x || x > rect.xMax) continue;
                EditorGUI.DrawRect(new Rect(x - 1f, rect.y, 2f, rect.height), new Color(1f, 0.8f, 0.2f, 0.7f));
            }
        }

        private void DrawTrimHandles(Rect rect, RecordingSession session)
        {
            float startX = TimeToX(rect, session.TrimStart);
            float endX = TimeToX(rect, session.EffectiveTrimEnd);

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, Mathf.Max(0, startX - rect.x), rect.height),
                new Color(0f, 0f, 0f, 0.45f));
            EditorGUI.DrawRect(new Rect(endX, rect.y, Mathf.Max(0, rect.xMax - endX), rect.height),
                new Color(0f, 0f, 0f, 0.45f));

            EditorGUI.DrawRect(new Rect(startX - HandleWidth * 0.5f, rect.y, HandleWidth, rect.height),
                new Color(0.3f, 1f, 0.4f, 0.8f));
            EditorGUI.DrawRect(new Rect(endX - HandleWidth * 0.5f, rect.y, HandleWidth, rect.height),
                new Color(1f, 0.4f, 0.3f, 0.8f));
        }

        private void DrawPlayhead(Rect rect, float playhead)
        {
            float x = TimeToX(rect, playhead);
            if (x >= rect.x && x <= rect.xMax)
                EditorGUI.DrawRect(new Rect(x - 1f, rect.y, 2f, rect.height), Color.white);
        }

        private float HandleInput(Rect rect, RecordingSession session, float playhead, float duration, int fps)
        {
            Event e = Event.current;

            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                Zoom = Mathf.Clamp(Zoom * (1f - e.delta.y * 0.05f), 10f, 2000f);
                e.Use();
                return playhead;
            }

            float startX = TimeToX(rect, session.TrimStart);
            float endX = TimeToX(rect, session.EffectiveTrimEnd);

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                float mx = e.mousePosition.x;
                if (Mathf.Abs(mx - startX) <= HandleWidth) _dragMode = 2;
                else if (Mathf.Abs(mx - endX) <= HandleWidth) _dragMode = 3;
                else
                {
                    _dragMode = 1;
                    playhead = SnapTime(XToTime(rect, mx), fps);
                }
                e.Use();
                GUI.changed = true;
            }
            else if (e.type == EventType.MouseDrag && _dragMode != 0)
            {
                float time = SnapTime(XToTime(rect, e.mousePosition.x), fps);
                switch (_dragMode)
                {
                    case 1: playhead = time; break;
                    case 2: session.TrimStart = Mathf.Clamp(time, 0f, session.EffectiveTrimEnd); break;
                    case 3: session.TrimEnd = Mathf.Clamp(time, session.TrimStart, duration); break;
                }
                e.Use();
                GUI.changed = true;
            }
            else if (e.type == EventType.MouseUp)
            {
                _dragMode = 0;
            }

            return playhead;
        }

        private float SnapTime(float time, int fps)
        {
            time = Mathf.Max(0f, time);
            if (!SnapToFrames) return time;
            return Mathf.Round(time * fps) / fps;
        }

        private float TimeToX(Rect rect, float time) { return rect.x + (time - ScrollSeconds) * Zoom; }

        private float XToTime(Rect rect, float x) { return (x - rect.x) / Zoom + ScrollSeconds; }

        private static float ChooseStep(float secondsVisible)
        {
            float raw = secondsVisible / 10f;
            float[] steps = { 0.05f, 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 30f, 60f };
            for (int i = 0; i < steps.Length; i++)
                if (steps[i] >= raw) return steps[i];
            return 60f;
        }
    }
}
