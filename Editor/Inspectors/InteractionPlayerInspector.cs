using UnityEditor;
using UnityEngine;
using AjoyGames.MotionKit.Components;

namespace AjoyGames.MotionKit.Editor.Inspectors
{
    /// <summary>Inspector for <see cref="InteractionPlayer"/> with in-play-mode transport controls.</summary>
    [CustomEditor(typeof(InteractionPlayer))]
    public sealed class InteractionPlayerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var player = (InteractionPlayer)target;
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to control playback.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Transport", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play")) player.PlayInstance();
            if (GUILayout.Button("Pause")) player.PauseInstance();
            if (GUILayout.Button("Resume")) player.ResumeInstance();
            if (GUILayout.Button("Stop")) player.StopInstance();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(player.IsPlaying
                ? "Playing — t=" + player.Engine.State.Time.ToString("0.00") + "s"
                : "Stopped");
            Repaint();
        }
    }
}
