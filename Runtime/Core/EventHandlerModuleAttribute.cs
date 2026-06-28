using System;

namespace AjoyGames.MotionKit
{
    /// <summary>
    /// Marks a class (implementing <see cref="AjoyGames.MotionKit.SDK.IRecordedEventHandler"/>) as a universal
    /// event handler. The editor code generator emits a reflection-free factory registration for the handler,
    /// keyed by <see cref="HandlerId"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class EventHandlerModuleAttribute : Attribute
    {
        /// <summary>Stable identifier persisted inside recordings. Must remain constant across versions.</summary>
        public string HandlerId { get; }

        /// <param name="handlerId">Stable identifier persisted inside recordings.</param>
        public EventHandlerModuleAttribute(string handlerId)
        {
            HandlerId = handlerId;
        }
    }
}
