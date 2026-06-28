using System;
using System.Collections.Generic;
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Central, runtime-reflection-free registry of all MotionKit extension modules (tracks, event handlers,
    /// migrations). Population is <b>push based</b>: the editor code generator emits one
    /// <c>RuntimeInitializeOnLoadMethod</c> per owning assembly that calls the <c>Register*</c> methods below
    /// with direct constructor delegates. No assembly scanning or <c>Activator</c> use ever occurs at runtime,
    /// so the system is safe under IL2CPP, AOT, WebGL, mobile and consoles.
    /// </summary>
    public static class ModuleRegistry
    {
        private static readonly Dictionary<string, ITrackModule> s_trackModulesById =
            new Dictionary<string, ITrackModule>(StringComparer.Ordinal);

        private static readonly Dictionary<Type, ITrackModule> s_trackModulesByDataType =
            new Dictionary<Type, ITrackModule>();

        private static readonly List<ITrackModule> s_trackModules = new List<ITrackModule>();

        private static readonly Dictionary<string, Func<IRecordedEventHandler>> s_eventHandlerFactories =
            new Dictionary<string, Func<IRecordedEventHandler>>(StringComparer.Ordinal);

        private static readonly List<IRecordingMigration> s_migrations = new List<IRecordingMigration>();

        private static bool s_migrationsDirty;

        /// <summary>All registered track modules, ordered by Order then display name.</summary>
        public static IReadOnlyList<ITrackModule> TrackModules { get { return s_trackModules; } }

        /// <summary>Registered universal event handler ids.</summary>
        public static IReadOnlyCollection<string> EventHandlerIds { get { return s_eventHandlerFactories.Keys; } }

        // ----- Registration (called only from generated module initializers) -----

        /// <summary>Registers a track module. Idempotent by <see cref="ITrackModule.Id"/>.</summary>
        public static void RegisterTrackModule(ITrackModule module)
        {
            if (module == null) return;
            if (string.IsNullOrEmpty(module.Id))
            {
                Debug.LogError("[MotionKit] Track module '" + module.GetType().Name + "' has an empty Id and was skipped.");
                return;
            }

            ITrackModule existing;
            if (s_trackModulesById.TryGetValue(module.Id, out existing))
            {
                if (existing.GetType() != module.GetType())
                    Debug.LogWarning("[MotionKit] Track module id '" + module.Id + "' is registered by both '" +
                                     existing.GetType().Name + "' and '" + module.GetType().Name + "'. Keeping the first.");
                return;
            }

            s_trackModulesById.Add(module.Id, module);
            if (module.TrackDataType != null)
                s_trackModulesByDataType[module.TrackDataType] = module;
            s_trackModules.Add(module);
            s_trackModules.Sort(CompareModules);
        }

        /// <summary>Registers an event handler factory. Idempotent by <paramref name="handlerId"/>.</summary>
        public static void RegisterEventHandler(string handlerId, Func<IRecordedEventHandler> factory)
        {
            if (string.IsNullOrEmpty(handlerId) || factory == null) return;
            s_eventHandlerFactories[handlerId] = factory;
        }

        /// <summary>Registers a migration step. Idempotent by (from,to) pair.</summary>
        public static void RegisterMigration(IRecordingMigration migration)
        {
            if (migration == null) return;
            for (int i = 0; i < s_migrations.Count; i++)
            {
                if (s_migrations[i].FromVersion == migration.FromVersion &&
                    s_migrations[i].ToVersion == migration.ToVersion)
                    return;
            }
            s_migrations.Add(migration);
            s_migrationsDirty = true;
        }

        // ----- Queries -----

        /// <summary>Looks up a track module by its stable id.</summary>
        public static bool TryGetTrackModule(string id, out ITrackModule module)
        {
            return s_trackModulesById.TryGetValue(id ?? string.Empty, out module);
        }

        /// <summary>Finds the module that owns the given concrete track data type.</summary>
        public static bool TryGetModuleForData(Type dataType, out ITrackModule module)
        {
            return s_trackModulesByDataType.TryGetValue(dataType, out module);
        }

        /// <summary>Creates the correct player for a recorded track (reflection-free).</summary>
        public static ITrackPlayer CreatePlayerFor(RecordedTrack track)
        {
            if (track == null) return null;
            ITrackModule module;
            return s_trackModulesByDataType.TryGetValue(track.GetType(), out module) ? module.CreatePlayer() : null;
        }

        /// <summary>Creates an event handler instance for the given id, or null if none is registered.</summary>
        public static IRecordedEventHandler CreateEventHandler(string handlerId)
        {
            Func<IRecordedEventHandler> factory;
            return handlerId != null && s_eventHandlerFactories.TryGetValue(handlerId, out factory) ? factory() : null;
        }

        /// <summary>Returns the ordered migration chain (ascending by FromVersion).</summary>
        public static IReadOnlyList<IRecordingMigration> GetMigrations()
        {
            if (s_migrationsDirty)
            {
                s_migrations.Sort((a, b) => a.FromVersion.CompareTo(b.FromVersion));
                s_migrationsDirty = false;
            }
            return s_migrations;
        }

        private static int CompareModules(ITrackModule a, ITrackModule b)
        {
            int byOrder = GetOrder(a).CompareTo(GetOrder(b));
            return byOrder != 0 ? byOrder : string.CompareOrdinal(a.DisplayName, b.DisplayName);
        }

        private static int GetOrder(ITrackModule m)
        {
            IOrderedTrackModule o = m as IOrderedTrackModule;
            return o != null ? o.Order : 0;
        }
    }

    /// <summary>Optional ordering hook a module can implement to influence its position in the editor UI.</summary>
    public interface IOrderedTrackModule
    {
        /// <summary>Lower values sort first.</summary>
        int Order { get; }
    }
}
