using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Submodules.Utility.Persistence
{
    /// <summary>
    /// One file per key in an injected directory: <c>key.sav</c>, with the value it replaced in
    /// <c>key.sav.bak</c>. A write goes to <c>key.sav.tmp</c> first and then replaces the save in one
    /// step, so a crash or a failed write leaves the previous save readable; a leftover temp file is
    /// never read as a save. <see cref="SetAside"/> renames a damaged save to <c>key.sav.corrupt</c>
    /// and keeps it; <see cref="AppendSideFile"/> writes other files beside the saves. The directory
    /// is created on the first write. The platform's persistent
    /// data path is chosen by the caller, never here, so tests point this at a temp directory.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        private const string SaveExtension = ".sav";
        private const string BackupExtension = ".bak";
        private const string TempExtension = ".tmp";
        private const string AsideExtension = ".corrupt";

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        private readonly string directory;

        public FileSaveStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("A file store needs a directory.", nameof(directory));

            this.directory = directory;
        }

        public bool TryRead(string key, out string content) => TryReadFile(SavePath(key), out content);

        public bool TryReadBackup(string key, out string content) => TryReadFile(SavePath(key) + BackupExtension, out content);

        public void Write(string key, string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var path = SavePath(key);
            var temp = path + TempExtension;

            Directory.CreateDirectory(directory);
            File.WriteAllText(temp, content, Utf8);

            if (File.Exists(path))
                File.Replace(temp, path, path + BackupExtension);
            else
                File.Move(temp, path);
        }

        public bool Delete(string key)
        {
            var path = SavePath(key);
            var deleted = false;

            foreach (var file in new[] { path, path + BackupExtension, path + TempExtension })
            {
                if (!File.Exists(file))
                    continue;

                File.Delete(file);
                deleted = true;
            }

            return deleted;
        }

        public IReadOnlyCollection<string> Keys()
        {
            var keys = new List<string>();

            if (!Directory.Exists(directory))
                return keys;

            foreach (var file in Directory.GetFiles(directory, "*" + SaveExtension))
            {
                if (file.EndsWith(SaveExtension, StringComparison.Ordinal))
                    keys.Add(Path.GetFileNameWithoutExtension(file));
            }

            return keys;
        }

        public bool SetAside(string key)
        {
            var path = SavePath(key);

            if (!File.Exists(path))
                return false;

            // key.sav.corrupt, then key.sav.corrupt2, ...: an earlier set-aside file is never overwritten,
            // and neither target of the two moves exists, so the pair is never left half moved.
            var aside = path + AsideExtension;
            for (var attempt = 2; File.Exists(aside) || File.Exists(aside + BackupExtension); attempt++)
                aside = path + AsideExtension + attempt;

            File.Move(path, aside);

            var backup = path + BackupExtension;
            if (File.Exists(backup))
                File.Move(backup, aside + BackupExtension);

            return true;
        }

        public void AppendSideFile(string fileName, string text)
        {
            if (string.IsNullOrEmpty(fileName)
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || fileName.EndsWith(SaveExtension, StringComparison.Ordinal))
                throw new ArgumentException($"'{fileName}' is not a usable side file name.", nameof(fileName));

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, fileName), text, Utf8);
        }

        private string SavePath(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"'{key}' is not a usable save key.", nameof(key));

            return Path.Combine(directory, key + SaveExtension);
        }

        private static bool TryReadFile(string path, out string content)
        {
            if (!File.Exists(path))
            {
                content = null;
                return false;
            }

            content = File.ReadAllText(path, Utf8);
            return true;
        }
    }
}
