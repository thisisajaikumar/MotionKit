using UnityEditor;
using AjoyGames.MotionKit;

namespace AjoyGames.MotionKit.Editor
{
    /// <summary>
    /// Editor menu toggle for <see cref="MotionKitDebug.Verbose"/>, persisted in EditorPrefs and restored on
    /// editor load. Lets users turn pipeline diagnostics on/off without code.
    /// </summary>
    [InitializeOnLoad]
    internal static class MotionKitDebugMenu
    {
        private const string MenuPath = "Tools/MotionKit/Verbose Logging";
        private const string PrefKey = "MotionKit.VerboseLogging";

        static MotionKitDebugMenu()
        {
            MotionKitDebug.Verbose = EditorPrefs.GetBool(PrefKey, false);
        }

        [MenuItem(MenuPath, false, 100)]
        private static void Toggle()
        {
            MotionKitDebug.Verbose = !MotionKitDebug.Verbose;
            EditorPrefs.SetBool(PrefKey, MotionKitDebug.Verbose);
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, MotionKitDebug.Verbose);
            return true;
        }
    }
}
