using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Local / debug level analytics (GDD Phase 4): one JSON line per event, written to the console and to
    /// <c>persistentDataPath/analytics.jsonl</c> in development builds and the editor. Release builds do nothing until a
    /// real analytics backend is plugged in behind this same API.
    /// </summary>
    public static class GameEvents
    {
        const string FileName = "analytics.jsonl";
        static readonly StringBuilder sb = new StringBuilder(256);
        static string path;

        static bool Active { get { return Debug.isDebugBuild; } }

        public static void Track(string name, params object[] keyValues)
        {
            if (!Active) return;
            sb.Length = 0;
            sb.Append("{\"event\":\"").Append(name).Append("\",\"t\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            for (int i = 0; i + 1 < keyValues.Length; i += 2)
            {
                sb.Append(",\"").Append(keyValues[i]).Append("\":");
                object v = keyValues[i + 1];
                if (v is string || v is Enum) sb.Append('"').Append(v).Append('"');
                else if (v is bool) sb.Append((bool)v ? "true" : "false");
                else if (v is float) sb.Append(((float)v).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                else sb.Append(v);
            }
            sb.Append('}');
            string line = sb.ToString();
            Debug.Log("[SkyQueue][event] " + line);
            try
            {
                if (path == null) path = Path.Combine(Application.persistentDataPath, FileName);
                File.AppendAllText(path, line + "\n");
            }
            catch (Exception) { /* analytics must never break the game */ }
        }
    }
}
