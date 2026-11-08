using System;
using System.IO;
using UnityEngine;

namespace SkyQueue
{
    /// <summary>
    /// Local save file with crash-safe writes: the new data goes to a temp file, the previous good file is kept as a
    /// backup, then the temp file is moved into place. Loading falls back to the backup when the main file is missing or
    /// fails validation, and to a fresh save when both are bad.
    /// </summary>
    public sealed class SaveStore
    {
        readonly string path, tempPath, backupPath;

        public SaveStore(string fileName)
        {
            path = Path.Combine(Application.persistentDataPath, fileName);
            tempPath = path + ".tmp";
            backupPath = path + ".bak";
        }

        public SaveData Load()
        {
            var d = Read(path);
            if (d != null) return d;
            d = Read(backupPath);
            DevLog.Info("save", d != null ? "main save unreadable, restored from backup" : "no valid save, starting fresh (" + path + ")");
            return d;
        }

        static SaveData Read(string file)
        {
            try { return File.Exists(file) ? SaveCodec.Decode(File.ReadAllText(file)) : null; }
            catch (Exception e) { Debug.LogWarning("[SkyQueue][save] could not read " + file + ": " + e.Message); return null; }
        }

        public bool Save(SaveData data)
        {
            using (Markers.Save.Auto())
            try
            {
                long t = DevLog.Stamp();
                File.WriteAllText(tempPath, SaveCodec.Encode(data));
                if (File.Exists(path))
                {
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Move(path, backupPath);
                }
                File.Move(tempPath, path);
                DevLog.Took("save", "write", t, 8f);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SkyQueue][save] write failed: " + e.Message);
                return false;
            }
        }
    }
}
