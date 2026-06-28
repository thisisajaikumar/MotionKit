using System;
using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Events.Handlers
{
    /// <summary>
    /// Re-fires recorded events as named "commands" that receive the playback root, enabling user-defined event
    /// types (spawn, toggle, custom gameplay) without touching MotionKit core code.
    /// </summary>
    [EventHandlerModule(HandlerId)]
    public sealed class CommandEventHandler : IRecordedEventHandler
    {
        /// <summary>Stable handler id persisted in recordings.</summary>
        public const string HandlerId = "command";

        private static readonly Dictionary<string, Action<GameObject, RecordedEvent>> s_commands =
            new Dictionary<string, Action<GameObject, RecordedEvent>>(StringComparer.Ordinal);

        private GameObject _root;

        string IRecordedEventHandler.HandlerId { get { return HandlerId; } }

        /// <summary>Registers (or replaces) a named command handler.</summary>
        public static void Register(string command, Action<GameObject, RecordedEvent> handler)
        {
            if (string.IsNullOrEmpty(command) || handler == null) return;
            s_commands[command] = handler;
        }

        /// <summary>Removes a registered command.</summary>
        public static void Unregister(string command)
        {
            if (!string.IsNullOrEmpty(command)) s_commands.Remove(command);
        }

        public void Prepare(InteractionEventBinder binder, GameObject root) { _root = root; }

        public void Invoke(in RecordedEvent recordedEvent)
        {
            Action<GameObject, RecordedEvent> cmd;
            if (recordedEvent.Key != null && s_commands.TryGetValue(recordedEvent.Key, out cmd))
                cmd(_root, recordedEvent);
        }

        public void OnStop() { _root = null; }
    }
}
