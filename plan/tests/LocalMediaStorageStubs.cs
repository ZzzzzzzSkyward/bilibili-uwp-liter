using System;
using System.IO;
using System.Threading.Tasks;

namespace Windows.Storage
{
    public interface IStorageItem { }

    public sealed class StorageFile : IStorageItem
    {
        public string Path { get; private set; }
        public string Name => System.IO.Path.GetFileName(Path);

        internal StorageFile(string path) { Path = path; }

        public Task RenameAsync(string name, NameCollisionOption option)
        {
            var next = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path), name);
            if (File.Exists(next) && option == NameCollisionOption.ReplaceExisting) File.Delete(next);
            File.Move(Path, next);
            Path = next;
            return Task.CompletedTask;
        }

        public Task MoveAndReplaceAsync(StorageFile target)
        {
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

        public Task<StorageFile> CreateFileAsync(string name, CreationCollisionOption option)
        {
            var path = System.IO.Path.Combine(Path, name);
            if (File.Exists(path) && option == CreationCollisionOption.ReplaceExisting) File.Delete(path);
            using (File.Create(path)) { }
            return Task.FromResult(new StorageFile(path));
        }
    }

    public static class FileIO
    {
        public static Task<string> ReadTextAsync(StorageFile file) => Task.FromResult(File.ReadAllText(file.Path));
        public static Task WriteTextAsync(StorageFile file, string text)
        {
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
        public int Count => folders.Count;
        public void AddOrReplace(string token, StorageFolder folder, string metadata) => folders[token] = folder;
        public Task<StorageFolder> GetFolderAsync(string token)
        {
            if (!folders.TryGetValue(token, out var folder)) throw new FileNotFoundException(token);
            return Task.FromResult(folder);
        }
        public void Remove(string token) => folders.Remove(token);
    }

    public static class StorageApplicationPermissions
    {
        public static AccessList FutureAccessList { get; } = new AccessList();
    }
}
