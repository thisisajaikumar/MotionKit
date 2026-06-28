using System;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Marks a class (implementing <see cref="AjoyGames.MotionKit.SDK.ITrackModule"/>) as a recordable track
    /// module. The MotionKit editor code generator discovers every type carrying this attribute via
    /// <c>UnityEditor.TypeCache</c> and emits a direct, reflection-free registration call into the owning
    /// assembly's generated registry file.
    /// </summary>
    /// <remarks>
    /// Reflection is used <b>only</b> at edit time to discover these types. At runtime the generated code
    /// registers modules through plain constructor calls, keeping the system IL2CPP / AOT / WebGL safe.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TrackModuleAttribute : Attribute
    {
        /// <summary>Optional explicit ordering hint used when listing modules in the editor UI.</summary>
        public int Order { get; set; }
    }
}
