using System;
using System.IO;
using System.Threading.Tasks;

namespace Windows.Storage
{
    public interface IStorageItem { }

    // Inert by default; gates/faults delay or fail primitive I/O, never registry decisions.
    public static class StorageTestHooks
    {
        public static Func<string, Task> BeforeCreate;
        public static Func<StorageFile, string, Task> AfterRead;
        public static Action<StorageFile, string> BeforeWrite;
        public static Action<StorageFile> BeforeReplace;
        public static Func<string, Task> BeforeGetFolder;
        public static Action<string> BeforeRemove;
        public static void Reset()
        {
            BeforeCreate = null; AfterRead = null; BeforeWrite = null;
            BeforeReplace = null; BeforeGetFolder = null; BeforeRemove = null;
        }
    }

    public sealed class StorageFile : IStorageItem
    {
        public string Path { get; private set; }
        public string Name => System.IO.Path.GetFileName(Path);

        internal StorageFile(string path) { Path = path; }

        public Task RenameAsync(string name, NameCollisionOption option)
        {
            StorageTestHooks.BeforeReplace?.Invoke(this);
            var next = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), name);
            if (File.Exists(next) && option == NameCollisionOption.ReplaceExisting) File.Delete(next);
            File.Move(Path, next);
            Path = next;
            return Task.CompletedTask;
        }

        public Task MoveAndReplaceAsync(StorageFile target)
        {
            StorageTestHooks.BeforeReplace?.Invoke(this);
            File.Delete(target.Path);
            File.Move(Path, target.Path);
            Path = target.Path;
            return Task.CompletedTask;
        }
    }

    public sealed class StorageFolder : IStorageItem
    {
        public string Path { get; private set; }
        internal StorageFolder(string path) { Path = path; }

        public Task<StorageFile> GetFileAsync(string name)
        {
            var path = System.IO.Path.Combine(Path, name.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            return Task.FromResult(new StorageFile(path));
        }

        public Task<StorageFolder> GetFolderAsync(string name)
        {
            var path = System.IO.Path.Combine(Path, name.Replace('/', System.IO.Path.DirectorySeparatorChar));
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
            return Task.FromResult(new StorageFolder(path));
        }

        public Task<IStorageItem> TryGetItemAsync(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            IStorageItem item = File.Exists(path) ? new StorageFile(path) : null;
            return Task.FromResult(item);
        }

        public async Task<StorageFile> CreateFileAsync(string name, CreationCollisionOption option)
        {
            if (StorageTestHooks.BeforeCreate != null) await StorageTestHooks.BeforeCreate(name);
            var path = System.IO.Path.Combine(Path, name);
            if (File.Exists(path) && option == CreationCollisionOption.ReplaceExisting) File.Delete(path);
            using (File.Create(path)) { }
            return new StorageFile(path);
        }
    }

    public static class FileIO
    {
        public static async Task<string> ReadTextAsync(StorageFile file)
        {
            var text = File.ReadAllText(file.Path);
            if (StorageTestHooks.AfterRead != null) await StorageTestHooks.AfterRead(file, text);
            return text;
        }
        public static Task WriteTextAsync(StorageFile file, string text)
        {
            StorageTestHooks.BeforeWrite?.Invoke(file, text);
            File.WriteAllText(file.Path, text);
            return Task.CompletedTask;
        }
    }

    public sealed class ApplicationData
    {
        public static ApplicationData Current { get; set; }
        public StorageFolder LocalFolder { get; set; }
    }

    public enum CreationCollisionOption { ReplaceExisting }
    public enum NameCollisionOption { ReplaceExisting }
}

namespace Windows.Storage.AccessCache
{
    using Windows.Storage;
    using System.Collections.Generic;

    public sealed class AccessList
    {
        private readonly Dictionary<string, StorageFolder> folders = new Dictionary<string, StorageFolder>();
        private readonly object sync = new object();
        public int Count { get { lock (sync) return folders.Count; } }
        public bool Contains(string token) { lock (sync) return folders.ContainsKey(token); }
        public void Clear() { lock (sync) folders.Clear(); }
        public void AddOrReplace(string token, StorageFolder folder, string metadata) { lock (sync) folders[token] = folder; }
        public async Task<StorageFolder> GetFolderAsync(string token)
        {
            if (StorageTestHooks.BeforeGetFolder != null) await StorageTestHooks.BeforeGetFolder(token);
            lock (sync)
            {
                if (!folders.TryGetValue(token, out var folder)) throw new FileNotFoundException(token);
                return folder;
            }
        }
        public void Remove(string token)
        {
            StorageTestHooks.BeforeRemove?.Invoke(token);
            lock (sync) folders.Remove(token);
        }
    }

    public static class StorageApplicationPermissions
    {
        public static AccessList FutureAccessList { get; } = new AccessList();
    }
}
