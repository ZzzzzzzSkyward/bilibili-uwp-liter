using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Models.LocalMedia;
using Newtonsoft.Json;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace BiliLite.Services.LocalMedia
{
    public sealed class LocalMediaLibraryException : Exception
    {
        public LocalMediaLibraryException(string stage, string collectionId, string message, Exception innerException = null)
            : base(message, innerException)
        {
            Stage = stage;
            CollectionId = collectionId;
        }

        public string Stage { get; private set; }
        public string CollectionId { get; private set; }
    }

    public sealed class LocalMediaCollection
    {
        public string Token { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public int ItemCount { get; set; }
        public LocalMediaManifest Manifest { get; set; }
    }

    public static class LocalMediaLibraryService
    {
        private const string RegistryFileName = "local-media-library.json";

        public static async Task<LocalMediaCollection> ImportFolderAsync(StorageFolder folder)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));
            LocalMediaManifest manifest = null;
            string stage = "manifest-read";
            string collectionId = "unknown";
            string token = null;
            var registrySaved = false;
            try
            {
                var manifestFile = await folder.GetFileAsync(LocalMediaManifestLoader.ManifestFileName);
                var json = await FileIO.ReadTextAsync(manifestFile);
                manifest = LocalMediaManifestLoader.Load(json, folder.Path, path => true);
                collectionId = manifest.Id;

                stage = "asset-resolution";
                var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                LocalMediaValidator.Validate(manifest, folder.Path, path => { references.Add(path); return true; });
                var resolvedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in references)
                {
                    var relativePath = MakeRelativePath(folder.Path, path);
                    try { await ResolveFileAsync(folder, relativePath); }
                    catch (Exception exception) { throw new LocalMediaManifestException("Referenced asset could not be opened: '" + relativePath + "'.", exception); }
                    resolvedPaths.Add(path);
                }

                stage = "manifest-validation";
                LocalMediaValidator.Validate(manifest, folder.Path, resolvedPaths.Contains);

                stage = "registry-read";
                var registry = await ReadRegistryAsync();
                var priorEntries = registry.Entries.Where(entry =>
                    string.Equals(entry.CollectionId, manifest.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(entry.RootPath, folder.Path, StringComparison.OrdinalIgnoreCase)).ToList();

                stage = "access-grant";
                token = Guid.NewGuid().ToString("N");
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(token, folder, manifest.Title);
                var savedEntry = new LibraryRegistryEntry
                {
                    Token = token,
                    CollectionId = manifest.Id,
                    Title = manifest.Title,
                    ItemCount = manifest.Items.Count,
                    RootPath = folder.Path
                };
                registry.Entries.RemoveAll(entry => priorEntries.Contains(entry));
                registry.Entries.Add(savedEntry);

                stage = "registry-write";
                await WriteRegistryAsync(registry);
                registrySaved = true;
                foreach (var prior in priorEntries)
                {
                    if (!string.Equals(prior.Token, token, StringComparison.Ordinal))
                        StorageApplicationPermissions.FutureAccessList.Remove(prior.Token);
                }

                return ToCollection(savedEntry, manifest);
            }
            catch (LocalMediaLibraryException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (token != null && !registrySaved)
                    StorageApplicationPermissions.FutureAccessList.Remove(token);
                throw CreateException(stage, collectionId, exception);
            }
        }

        public static async Task<IReadOnlyList<LocalMediaCollection>> GetSavedCollectionsAsync()
        {
            var registry = await ReadRegistryAsync();
            return registry.Entries.Select(entry => ToCollection(entry, null)).ToList();
        }

        public static async Task<LocalMediaCollection> ReopenCollectionAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("A collection token is required.", nameof(token));
            var registry = await ReadRegistryAsync();
            var entry = registry.Entries.FirstOrDefault(item => string.Equals(item.Token, token, StringComparison.Ordinal));
            if (entry == null) throw new LocalMediaLibraryException("access-restore", "unknown", "This saved collection is no longer available. Import its folder again.");

            string stage = "access-restore";
            try
            {
                var folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                stage = "manifest-read";
                var manifestFile = await folder.GetFileAsync(LocalMediaManifestLoader.ManifestFileName);
                var json = await FileIO.ReadTextAsync(manifestFile);
                var manifest = LocalMediaManifestLoader.Load(json, folder.Path, path => true);

                stage = "asset-resolution";
                var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                LocalMediaValidator.Validate(manifest, folder.Path, path => { references.Add(path); return true; });
                var resolvedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in references)
                {
                    var relativePath = MakeRelativePath(folder.Path, path);
                    try { await ResolveFileAsync(folder, relativePath); }
                    catch (Exception exception) { throw new LocalMediaManifestException("Referenced asset could not be opened: '" + relativePath + "'.", exception); }
                    resolvedPaths.Add(path);
                }

                stage = "manifest-validation";
                LocalMediaValidator.Validate(manifest, folder.Path, resolvedPaths.Contains);
                return ToCollection(entry, manifest);
            }
            catch (Exception exception)
            {
                throw CreateException(stage, entry.CollectionId, exception);
            }
        }

        private static async Task<StorageFile> ResolveFileAsync(StorageFolder root, string relativePath)
        {
            var segments = relativePath.Split('/');
            var current = root;
            for (var index = 0; index < segments.Length - 1; index++)
                current = await current.GetFolderAsync(segments[index]);
            return await current.GetFileAsync(segments[segments.Length - 1]);
        }

        private static string MakeRelativePath(string root, string fullPath)
        {
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new LocalMediaManifestException("An asset path resolved outside the collection folder.");
            return fullPath.Substring(prefix.Length).Replace('\\', '/');
        }

        private static async Task<LibraryRegistry> ReadRegistryAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.TryGetItemAsync(RegistryFileName) as StorageFile;
                if (file == null) return new LibraryRegistry();
                var registry = JsonConvert.DeserializeObject<LibraryRegistry>(await FileIO.ReadTextAsync(file));
                registry = registry ?? new LibraryRegistry();
                registry.Entries = registry.Entries ?? new List<LibraryRegistryEntry>();
                return registry;
            }
            catch (Exception exception)
            {
                if (exception is LocalMediaLibraryException) throw;
                throw CreateException("registry-read", "unknown", exception);
            }
        }

        private static async Task WriteRegistryAsync(LibraryRegistry registry)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var temporary = await localFolder.CreateFileAsync(RegistryFileName + ".tmp", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(temporary, JsonConvert.SerializeObject(registry));
            var current = await localFolder.TryGetItemAsync(RegistryFileName) as StorageFile;
            if (current == null)
                await temporary.RenameAsync(RegistryFileName, NameCollisionOption.ReplaceExisting);
            else
                await temporary.MoveAndReplaceAsync(current);
        }

        private static LocalMediaCollection ToCollection(LibraryRegistryEntry entry, LocalMediaManifest manifest)
        {
            return new LocalMediaCollection
            {
                Token = entry.Token,
                Id = manifest == null ? entry.CollectionId : manifest.Id,
                Title = manifest == null ? entry.Title : manifest.Title,
                ItemCount = manifest == null ? entry.ItemCount : manifest.Items.Count,
                Manifest = manifest
            };
        }

        private static LocalMediaLibraryException CreateException(string stage, string collectionId, Exception exception)
        {
            var validation = exception as LocalMediaManifestException;
            var message = validation == null ? "The collection could not be opened. Check that its manifest and referenced files are available." : validation.Message;
            return new LocalMediaLibraryException(stage, collectionId, message, exception);
        }

        private sealed class LibraryRegistry
        {
            public List<LibraryRegistryEntry> Entries { get; set; } = new List<LibraryRegistryEntry>();
        }

        private sealed class LibraryRegistryEntry
        {
            public string Token { get; set; }
            public string CollectionId { get; set; }
            public string Title { get; set; }
            public int ItemCount { get; set; }
            public string RootPath { get; set; }
        }
    }
}
