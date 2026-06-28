using System.Text;
using UnityEditor;

namespace AjoyGames.MotionKit.Editor.AssetGeneration
{
    /// <summary>Helpers for creating and sanitizing asset-database paths/folders.</summary>
    public static class AssetPathUtil
    {
        /// <summary>Ensures a nested folder under "Assets/..." exists, creating each missing segment.</summary>
        public static string EnsureFolder(string projectRelativePath)
        {
            projectRelativePath = projectRelativePath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(projectRelativePath))
                return projectRelativePath;

            string[] parts = projectRelativePath.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            return current;
        }

        /// <summary>Strips characters that are illegal in asset/file names.</summary>
        public static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Recording";
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' ' ? c : '_');
            return sb.ToString().Trim();
        }
    }
}
