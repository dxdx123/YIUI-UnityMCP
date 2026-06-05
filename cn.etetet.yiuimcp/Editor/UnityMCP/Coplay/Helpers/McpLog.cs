// ----------------------------------------------------------------------------
// Ported from MCP for Unity — https://github.com/CoplayDev/unity-mcp
// Copyright (c) 2025 CoplayDev. Licensed under the MIT License.
// Full license text: see THIRD-PARTY-NOTICES.md at the repository root.
// Vendored into YIUI-UnityMCP (cn.etetet.yiuimcp).
// CHANGE FROM UPSTREAM: dropped the MCPForUnity.Editor.Constants.EditorPrefKeys
//   dependency; the debug-logs EditorPrefs key is inlined below.
// ----------------------------------------------------------------------------
using UnityEditor;

namespace MCPForUnity.Editor.Helpers
{
    internal static class McpLog
    {
        private const string InfoPrefix = "<b><color=#2EA3FF>YIUIMCP-COPLAY</color></b>:";
        private const string DebugPrefix = "<b><color=#6AA84F>YIUIMCP-COPLAY</color></b>:";
        private const string WarnPrefix = "<b><color=#cc7a00>YIUIMCP-COPLAY</color></b>:";
        private const string ErrorPrefix = "<b><color=#cc3333>YIUIMCP-COPLAY</color></b>:";

        // Inlined replacement for MCPForUnity.Editor.Constants.EditorPrefKeys.DebugLogs
        private const string DebugLogsPrefKey = "YIUIMCP.Coplay.DebugLogs";

        private static volatile bool _debugEnabled = ReadDebugPreference();

        private static bool IsDebugEnabled() => _debugEnabled;

        private static bool ReadDebugPreference()
        {
            try { return EditorPrefs.GetBool(DebugLogsPrefKey, false); }
            catch { return false; }
        }

        public static void SetDebugLoggingEnabled(bool enabled)
        {
            _debugEnabled = enabled;
            try { EditorPrefs.SetBool(DebugLogsPrefKey, enabled); }
            catch { }
        }

        public static void Debug(string message)
        {
            if (!IsDebugEnabled()) return;
            UnityEngine.Debug.Log($"{DebugPrefix} {message}");
        }

        public static void Info(string message, bool always = true)
        {
            if (!always && !IsDebugEnabled()) return;
            UnityEngine.Debug.Log($"{InfoPrefix} {message}");
        }

        public static void Warn(string message)
        {
            UnityEngine.Debug.LogWarning($"{WarnPrefix} {message}");
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError($"{ErrorPrefix} {message}");
        }
    }
}
