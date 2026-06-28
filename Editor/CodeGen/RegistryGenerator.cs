using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using AjoyGames.MotionKit.SDK;

namespace AjoyGames.MotionKit.Editor.CodeGen
{
    /// <summary>
    /// Editor-only code generator that discovers every <c>[TrackModule]</c>, <c>[EventHandlerModule]</c> and
    /// <c>[RecordingMigration]</c> type via <see cref="TypeCache"/> and emits reflection-free registration code
    /// into each owning assembly's <c>Generated/MotionKitGeneratedRegistry.gen.cs</c> file.
    /// </summary>
    /// <remarks>
    /// This is the only place reflection runs, and it runs only in the editor. The generated files use
    /// <c>RuntimeInitializeOnLoadMethod</c> + <c>InitializeOnLoadMethod</c> so registration happens both in
    /// play/builds (IL2CPP/AOT safe — Unity emits the calls) and at edit time. Files are written only when their
    /// content actually changes, preventing recompile loops.
    /// </remarks>
    public static class RegistryGenerator
    {
        private const string GeneratedFileName = "MotionKitGeneratedRegistry.gen.cs";
        private const string GeneratedFolder = "Generated";
        private const string Nl = "\n";

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += () => Generate(false);
        }

        [MenuItem("Tools/MotionKit/Regenerate Module Registry")]
        public static void RegenerateMenu() { Generate(true); }

        /// <summary>Scans, generates and writes registry files. Returns true if any file changed.</summary>
        public static bool Generate(bool logOnNoChange)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
                return false;

            Dictionary<string, AssemblyModules> byAssembly = CollectByAssembly();
            bool anyChanged = false;

            foreach (KeyValuePair<string, AssemblyModules> pair in byAssembly)
            {
                string assemblyName = pair.Key;
                AssemblyModules modules = pair.Value;

                string asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
                if (string.IsNullOrEmpty(asmdefPath))
                    continue;

                string dir = Path.Combine(Path.GetDirectoryName(asmdefPath), GeneratedFolder);
                string filePath = Path.Combine(dir, GeneratedFileName).Replace('\\', '/');
                string content = BuildFile(assemblyName, modules);

                string existing = File.Exists(filePath) ? File.ReadAllText(filePath) : null;
                if (existing == content)
                    continue;

                try
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(filePath, content);
                    AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                    anyChanged = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[MotionKit] Could not write generated registry for '" + assemblyName +
                                     "' at '" + filePath + "': " + e.Message);
                }
            }

            if (anyChanged)
                AssetDatabase.Refresh();
            else if (logOnNoChange)
                Debug.Log("[MotionKit] Module registry is already up to date.");

            return anyChanged;
        }

        private static Dictionary<string, AssemblyModules> CollectByAssembly()
        {
            var result = new Dictionary<string, AssemblyModules>(StringComparer.Ordinal);

            foreach (Type t in TypeCache.GetTypesWithAttribute<TrackModuleAttribute>())
            {
                if (!IsConcrete(t) || !typeof(ITrackModule).IsAssignableFrom(t) || !HasDefaultCtor(t)) continue;
                GetBucket(result, t).TrackModules.Add(t);
            }

            foreach (Type t in TypeCache.GetTypesWithAttribute<EventHandlerModuleAttribute>())
            {
                if (!IsConcrete(t) || !typeof(IRecordedEventHandler).IsAssignableFrom(t) || !HasDefaultCtor(t)) continue;
                EventHandlerModuleAttribute attr = t.GetCustomAttribute<EventHandlerModuleAttribute>();
                if (attr == null || string.IsNullOrEmpty(attr.HandlerId)) continue;
                GetBucket(result, t).EventHandlers.Add(new KeyValuePair<Type, string>(t, attr.HandlerId));
            }

            foreach (Type t in TypeCache.GetTypesWithAttribute<RecordingMigrationAttribute>())
            {
                if (!IsConcrete(t) || !typeof(IRecordingMigration).IsAssignableFrom(t) || !HasDefaultCtor(t)) continue;
                GetBucket(result, t).Migrations.Add(t);
            }

            foreach (AssemblyModules m in result.Values)
                m.Sort();

            return result;
        }

        private static string BuildFile(string assemblyName, AssemblyModules modules)
        {
            string safe = Sanitize(assemblyName);
            var sb = new StringBuilder(2048);

            sb.Append("// <auto-generated>").Append(Nl);
            sb.Append("//     Generated by MotionKit RegistryGenerator. Do not edit by hand.").Append(Nl);
            sb.Append("//     Reflection-free registration of MotionKit extension modules.").Append(Nl);
            sb.Append("// </auto-generated>").Append(Nl);
            sb.Append("using UnityEngine;").Append(Nl);
            sb.Append("using AjoyGames.MotionKit;").Append(Nl).Append(Nl);
            sb.Append("namespace AjoyGames.MotionKit.Generated").Append(Nl);
            sb.Append("{").Append(Nl);
            sb.Append("    internal static class MotionKitGeneratedRegistry_").Append(safe).Append(Nl);
            sb.Append("    {").Append(Nl);
            sb.Append("        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]").Append(Nl);
            sb.Append("        private static void RegisterRuntime() => Register();").Append(Nl).Append(Nl);
            sb.Append("#if UNITY_EDITOR").Append(Nl);
            sb.Append("        [UnityEditor.InitializeOnLoadMethod]").Append(Nl);
            sb.Append("        private static void RegisterEditor() => Register();").Append(Nl);
            sb.Append("#endif").Append(Nl).Append(Nl);
            sb.Append("        private static void Register()").Append(Nl);
            sb.Append("        {").Append(Nl);

            foreach (Type t in modules.TrackModules)
                AppendConditional(sb, t, "            ModuleRegistry.RegisterTrackModule(new " + TypeRef(t) + "());");

            foreach (KeyValuePair<Type, string> e in modules.EventHandlers)
                AppendConditional(sb, e.Key, "            ModuleRegistry.RegisterEventHandler(\"" + Escape(e.Value) +
                                  "\", () => new " + TypeRef(e.Key) + "());");

            foreach (Type t in modules.Migrations)
                sb.Append("            ModuleRegistry.RegisterMigration(new ").Append(TypeRef(t)).Append("());").Append(Nl);

            if (modules.IsEmpty)
                sb.Append("            // No modules found in this assembly.").Append(Nl);

            sb.Append("        }").Append(Nl);
            sb.Append("    }").Append(Nl);
            sb.Append("}").Append(Nl);
            return sb.ToString();
        }

        private static AssemblyModules GetBucket(Dictionary<string, AssemblyModules> map, Type t)
        {
            string name = t.Assembly.GetName().Name;
            AssemblyModules bucket;
            if (!map.TryGetValue(name, out bucket))
            {
                bucket = new AssemblyModules();
                map[name] = bucket;
            }
            return bucket;
        }

        private static bool IsConcrete(Type t) { return t != null && !t.IsAbstract && !t.IsGenericTypeDefinition && t.IsClass; }

        private static bool HasDefaultCtor(Type t)
        {
            return t.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
        }

        private static string TypeRef(Type t) { return "global::" + t.FullName.Replace('+', '.'); }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        private static string Escape(string s) { return s.Replace("\\", "\\\\").Replace("\"", "\\\""); }

        private static void AppendConditional(StringBuilder sb, Type type, string registration)
        {
            string define = GetOptionalModuleDefine(type.FullName);
            if (define != null) sb.Append("#if ").Append(define).Append(Nl);
            sb.Append(registration).Append(Nl);
            if (define != null) sb.Append("#endif").Append(Nl);
        }

        private static string GetOptionalModuleDefine(string fullName)
        {
            switch (fullName)
            {
                case "AjoyGames.MotionKit.Tracks.AnimatorTrackModule":
                case "AjoyGames.MotionKit.Events.Handlers.AnimatorParameterHandler":
                    return "MOTIONKIT_ANIMATION";
                case "AjoyGames.MotionKit.Tracks.AudioTrackModule":
                case "AjoyGames.MotionKit.Events.Handlers.AudioEventHandler":
                    return "MOTIONKIT_AUDIO";
                case "AjoyGames.MotionKit.Tracks.ParticleTrackModule":
                    return "MOTIONKIT_PARTICLES";
                case "AjoyGames.MotionKit.Tracks.PhysicsTrackModule":
                    return "MOTIONKIT_PHYSICS";
                case "AjoyGames.MotionKit.Tracks.UITrackModule":
                    return "MOTIONKIT_UGUI";
                case "AjoyGames.MotionKit.Events.Handlers.TimelineSignalHandler":
                    return "MOTIONKIT_DIRECTOR";
                default:
                    return null;
            }
        }

        private sealed class AssemblyModules
        {
            public readonly List<Type> TrackModules = new List<Type>();
            public readonly List<KeyValuePair<Type, string>> EventHandlers = new List<KeyValuePair<Type, string>>();
            public readonly List<Type> Migrations = new List<Type>();

            public bool IsEmpty { get { return TrackModules.Count == 0 && EventHandlers.Count == 0 && Migrations.Count == 0; } }

            public void Sort()
            {
                TrackModules.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
                Migrations.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
                EventHandlers.Sort((a, b) => string.CompareOrdinal(a.Key.FullName, b.Key.FullName));
            }
        }
    }
}
