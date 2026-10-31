using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Services.LocalMedia;
using Windows.Storage;
using Windows.Storage.AccessCache;

internal static class LM02
{
    private static async Task Main()
    {
        var scratch = Path.Combine(Path.GetTempPath(), "lm02-" + Guid.NewGuid().ToString("N"));
        var state = Path.Combine(scratch, "state");
        Directory.CreateDirectory(state);
        ApplicationData.Current = new ApplicationData { LocalFolder = new StorageFolderForTests(state).Folder };
        try
        {
            var validFolder = MakeCollection(scratch, "valid", "series-a", "video/one.mp4");
            var imported = await LocalMediaLibraryService.ImportFolderAsync(validFolder);
            Assert(imported.Id == "series-a" && imported.ItemCount == 1, "import returns collection metadata");
            Assert(StorageApplicationPermissions.FutureAccessList.Count == 1, "import stores one folder grant");

            var reopened = await LocalMediaLibraryService.ReopenCollectionAsync(imported.Token);
            Assert(reopened.Manifest.Items.Single().VideoTracks.Single().Path == "video/one.mp4", "saved token reopens and reloads manifest: " + reopened.Manifest.Items.Single().VideoTracks.Single().Path);

            var invalidPathFolder = MakeCollection(scratch, "invalid-path", "series-b", "../outside.mp4");
            await ExpectImportFailure(invalidPathFolder, "path traversal is rejected");
            var missingAssetFolder = MakeCollection(scratch, "missing", "series-c", "missing.mp4", createAsset: false);
            await ExpectImportFailure(missingAssetFolder, "missing referenced asset is rejected");
            Assert((await LocalMediaLibraryService.GetSavedCollectionsAsync()).Count == 1, "failed imports do not persist registry entries");
            Assert(StorageApplicationPermissions.FutureAccessList.Count == 1, "failed imports do not persist folder grants");

            File.Delete(Path.Combine(validFolder.Path, "video", "one.mp4"));
            await ExpectReopenFailure(imported.Token, "broken collection reopen reports an error");
            Assert((await LocalMediaLibraryService.GetSavedCollectionsAsync()).Count == 1, "failed reopen retains saved entry for repair");
            File.WriteAllText(Path.Combine(validFolder.Path, "video", "one.mp4"), "media");
            await LocalMediaLibraryService.ReopenCollectionAsync(imported.Token);

            await LocalMediaLibraryService.ImportFolderAsync(validFolder);
            Assert((await LocalMediaLibraryService.GetSavedCollectionsAsync()).Count == 1, "reimport replaces duplicate registry entry");
            Assert(StorageApplicationPermissions.FutureAccessList.Count == 1, "reimport removes superseded access token");
            Console.WriteLine("LM-02 checks passed: import/reopen, invalid paths, missing assets, retained broken entry, reimport deduplication.");
        }
        finally
        {
            Directory.Delete(scratch, true);
        }
    }

    private static StorageFolder MakeCollection(string parent, string name, string id, string videoPath, bool createAsset = true)
    {
        var root = Path.Combine(parent, name);
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "video"));
        if (createAsset && videoPath.IndexOf("..", StringComparison.Ordinal) < 0)
            File.WriteAllText(Path.Combine(root, videoPath.Replace('/', Path.DirectorySeparatorChar)), "media");
        File.WriteAllText(Path.Combine(root, "bili-media.json"),
            "{\"schemaVersion\":1,\"id\":\"" + id + "\",\"title\":\"Collection\",\"items\":[{" +
            "\"id\":\"ep1\",\"title\":\"Episode 1\",\"videoTracks\":[{\"id\":\"main\",\"path\":\"" + videoPath + "\"}]}]}");
        return new StorageFolderForTests(root).Folder;
    }

    private static async Task ExpectImportFailure(StorageFolder folder, string message)
    {
        try { await LocalMediaLibraryService.ImportFolderAsync(folder); }
        catch (LocalMediaLibraryException) { return; }
        throw new Exception(message);
    }

    private static async Task ExpectReopenFailure(string token, string message)
    {
        try { await LocalMediaLibraryService.ReopenCollectionAsync(token); }
        catch (LocalMediaLibraryException) { return; }
        throw new Exception(message);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class StorageFolderForTests
    {
        public StorageFolder Folder { get; private set; }
        public StorageFolderForTests(string path) { Folder = (StorageFolder)Activator.CreateInstance(typeof(StorageFolder), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new object[] { path }, null); }
    }
}
