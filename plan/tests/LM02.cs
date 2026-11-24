using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
            await StorageIntegrityCases(scratch);
            await RegistryIdentityCases(scratch);
            Console.WriteLine("LM-02 checks passed: import/reopen, invalid paths, missing assets, retained broken entry, reimport deduplication.");
        }
        finally
        {
            StorageTestHooks.Reset();
            StorageApplicationPermissions.FutureAccessList.Clear();
            Directory.Delete(scratch, true);
        }
    }

    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(10);
    private static readonly List<string> StorageFailures = new List<string>();
    private static TaskCompletionSource<bool> Signal() => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    private static string RegistryPath => Path.Combine(ApplicationData.Current.LocalFolder.Path, "local-media-library.json");
    private static void StorageCheck(bool condition, string message)
    {
        Console.WriteLine((condition ? "[ok] " : "[FAIL] ") + message);
        if (!condition) StorageFailures.Add(message);
    }
    private static void ResetStorage(string scratch, string name)
    {
        StorageTestHooks.Reset();
        StorageApplicationPermissions.FutureAccessList.Clear();
        var state = Path.Combine(scratch, "integrity-" + name);
        Directory.CreateDirectory(state);
        ApplicationData.Current = new ApplicationData { LocalFolder = new StorageFolderForTests(state).Folder };
    }
    private static async Task<LocalMediaLibraryException> TypedFailure(Task operation, string stage)
    {
        try { await operation.WaitAsync(GateTimeout); }
        catch (LocalMediaLibraryException exception)
        {
            StorageCheck(exception.Stage == stage, "failure retains typed " + stage + " stage");
            return exception;
        }
        StorageCheck(false, "expected typed " + stage + " failure");
        return null;
    }
    private static async Task CanReopen(string token, string label)
    {
        try { await LocalMediaLibraryService.ReopenCollectionAsync(token).WaitAsync(GateTimeout); StorageCheck(true, label); }
        catch (LocalMediaLibraryException exception) { StorageCheck(false, label + ": " + exception.Stage); }
    }

    private static async Task StorageIntegrityCases(string scratch)
    {
        var aFolder = MakeCollection(scratch, "concurrent-a", "a", "video/one.mp4");
        var bFolder = MakeCollection(scratch, "concurrent-b", "b", "video/one.mp4");
        ResetStorage(scratch, "imports");
        var entered = Signal(); var release = Signal(); var creates = 0;
        StorageTestHooks.BeforeCreate = async name =>
        {
            if (Interlocked.Increment(ref creates) == 1)
            { entered.TrySetResult(true); await release.Task.WaitAsync(GateTimeout); }
        };
        var aTask = LocalMediaLibraryService.ImportFolderAsync(aFolder);
        Task<LocalMediaCollection> bTask = null;
        Task<IReadOnlyList<LocalMediaCollection>> listTask = null;
        try
        {
            await entered.Task.WaitAsync(GateTimeout);
            bTask = LocalMediaLibraryService.ImportFolderAsync(bFolder);
            listTask = LocalMediaLibraryService.GetSavedCollectionsAsync();
            StorageCheck(!bTask.IsCompleted && creates == 1, "concurrent import waits before reading/staging a second transaction");
            StorageCheck(!listTask.IsCompleted, "saved collection reader waits for admitted registry transaction");
        }
        finally
        {
            release.TrySetResult(true);
            await Task.WhenAll(new Task[] { aTask, bTask ?? Task.CompletedTask, listTask ?? Task.CompletedTask }).WaitAsync(GateTimeout);
            StorageTestHooks.Reset();
        }
        var rows = await LocalMediaLibraryService.GetSavedCollectionsAsync();
        StorageCheck(rows.Count == 2 && rows.Select(row => row.Id).OrderBy(id => id).SequenceEqual(new[] { "a", "b" }), "both successful distinct imports survive latest-registry merge");
        StorageCheck(StorageApplicationPermissions.FutureAccessList.Count == rows.Count && rows.All(row => StorageApplicationPermissions.FutureAccessList.Contains(row.Token)), "successful imports have exactly their reachable grants");
        await CanReopen(aTask.Result.Token, "concurrent import A reopens");
        await CanReopen(bTask.Result.Token, "concurrent import B reopens");
        rows[0].Title = "detached caller mutation";
        StorageCheck((await LocalMediaLibraryService.GetSavedCollectionsAsync()).All(row => row.Title == "Collection"), "saved collection rows remain detached from registry");

        ResetStorage(scratch, "same-root");
        var original = await LocalMediaLibraryService.ImportFolderAsync(aFolder);
        entered = Signal(); release = Signal(); creates = 0;
        StorageTestHooks.BeforeCreate = async name =>
        {
            if (Interlocked.Increment(ref creates) == 1)
            { entered.TrySetResult(true); await release.Task.WaitAsync(GateTimeout); }
        };
        var retireAfterCommit = true;
        StorageTestHooks.BeforeRemove = token => retireAfterCommit &= !File.ReadAllText(RegistryPath).Contains(token);
        aTask = LocalMediaLibraryService.ImportFolderAsync(aFolder); bTask = null;
        try
        {
            await entered.Task.WaitAsync(GateTimeout);
            bTask = LocalMediaLibraryService.ImportFolderAsync(aFolder);
            StorageCheck(!bTask.IsCompleted && creates == 1, "same-root reimports share registry and grant ownership");
        }
        finally
        {
            release.TrySetResult(true);
            await Task.WhenAll(new Task[] { aTask, bTask ?? Task.CompletedTask }).WaitAsync(GateTimeout);
            StorageTestHooks.Reset();
        }
        rows = await LocalMediaLibraryService.GetSavedCollectionsAsync();
        StorageCheck(rows.Count == 1 && rows[0].Token == bTask.Result.Token && StorageApplicationPermissions.FutureAccessList.Count == 1, "concurrent same-root reimports retain only latest entry and grant");
        StorageCheck(retireAfterCommit && !StorageApplicationPermissions.FutureAccessList.Contains(original.Token) && !StorageApplicationPermissions.FutureAccessList.Contains(aTask.Result.Token), "each superseded grant retires only after replacement commit");
        await CanReopen(bTask.Result.Token, "latest replacement reopens");

        // Case-insensitive same-id/same-root dedup; either different component remains distinct.
        MakeCollection(scratch, "concurrent-a", "A", "video/one.mp4");
        await LocalMediaLibraryService.ImportFolderAsync(aFolder);
        StorageCheck((await LocalMediaLibraryService.GetSavedCollectionsAsync()).Count == 1 && StorageApplicationPermissions.FutureAccessList.Count == 1, "same-root collection id comparison stays case insensitive");
        var otherRoot = MakeCollection(scratch, "other-root", "A", "video/one.mp4");
        await LocalMediaLibraryService.ImportFolderAsync(otherRoot);
        MakeCollection(scratch, "concurrent-a", "other-id", "video/one.mp4");
        await LocalMediaLibraryService.ImportFolderAsync(aFolder);
        StorageCheck((await LocalMediaLibraryService.GetSavedCollectionsAsync()).Count == 3 && StorageApplicationPermissions.FutureAccessList.Count == 3, "different-root same-id and different-id same-root remain distinct");
        MakeCollection(scratch, "concurrent-a", "a", "video/one.mp4");

        foreach (var gate in new[] { "read", "folder", "manifest" })
            await ReopenOwnershipCase(scratch, aFolder, gate);

        ResetStorage(scratch, "faults");
        original = await LocalMediaLibraryService.ImportFolderAsync(aFolder);
        foreach (var fault in new[] { "write", "replace", "read" })
        {
            var prior = File.ReadAllText(RegistryPath);
            if (fault == "write") StorageTestHooks.BeforeWrite = (file, text) => { File.WriteAllText(file.Path, "{partial"); throw new IOException("injected partial staging write"); };
            if (fault == "replace") StorageTestHooks.BeforeReplace = file => throw new IOException("injected pre-replace failure");
            if (fault == "read") StorageTestHooks.AfterRead = (file, text) => file.Name == "local-media-library.json" ? Task.FromException(new IOException("injected registry read")) : Task.CompletedTask;
            try { await TypedFailure(LocalMediaLibraryService.ImportFolderAsync(aFolder), fault == "read" ? "registry-read" : "registry-write"); }
            finally { StorageTestHooks.Reset(); }
            StorageCheck(File.ReadAllText(RegistryPath) == prior && StorageApplicationPermissions.FutureAccessList.Count == 1 && StorageApplicationPermissions.FutureAccessList.Contains(original.Token), fault + " failure preserves committed bytes and old grant");
            await CanReopen(original.Token, fault + " failure preserves prior reopen");
            var retry = await LocalMediaLibraryService.ImportFolderAsync(aFolder).WaitAsync(GateTimeout);
            StorageCheck(StorageApplicationPermissions.FutureAccessList.Count == 1 && !StorageApplicationPermissions.FutureAccessList.Contains(original.Token), fault + " failure releases gate and retry retires superseded grant");
            original = retry;
        }
        var goodBytes = File.ReadAllText(RegistryPath);
        File.WriteAllText(RegistryPath, "{corrupt");
        await TypedFailure(LocalMediaLibraryService.ImportFolderAsync(bFolder), "registry-read");
        await TypedFailure(LocalMediaLibraryService.GetSavedCollectionsAsync(), "registry-read");
        await TypedFailure(LocalMediaLibraryService.ReopenCollectionAsync(original.Token), "registry-read");
        StorageCheck(File.ReadAllText(RegistryPath) == "{corrupt" && StorageApplicationPermissions.FutureAccessList.Count == 1 && StorageApplicationPermissions.FutureAccessList.Contains(original.Token), "corrupt registry preserves bytes and grants without new provisional access");
        File.WriteAllText(RegistryPath, goodBytes);
        await CanReopen(original.Token, "repair after corrupt read proves gate release");
        await TypedFailure(LocalMediaLibraryService.ReopenCollectionAsync("unknown-token"), "access-restore");
        StorageApplicationPermissions.FutureAccessList.Remove(original.Token);
        await TypedFailure(LocalMediaLibraryService.ReopenCollectionAsync(original.Token), "access-restore");
        StorageCheck((await LocalMediaLibraryService.GetSavedCollectionsAsync()).Count == 1, "truly stale input token retains saved entry for repair");
        if (StorageFailures.Count != 0) throw new Exception("Storage integrity failed: " + string.Join("; ", StorageFailures));
    }

    private static async Task ReopenOwnershipCase(string scratch, StorageFolder folder, string gate)
    {
        ResetStorage(scratch, "reopen-" + gate);
        var original = await LocalMediaLibraryService.ImportFolderAsync(folder);
        var entered = Signal(); var release = Signal(); var held = 0;
        StorageTestHooks.AfterRead = async (file, text) =>
        {
            if ((gate == "read" && file.Name == "local-media-library.json" || gate == "manifest" && file.Name == "bili-media.json") && Interlocked.Exchange(ref held, 1) == 0)
            { entered.TrySetResult(true); await release.Task.WaitAsync(GateTimeout); }
        };
        StorageTestHooks.BeforeGetFolder = async token =>
        {
            if (gate == "folder" && Interlocked.Exchange(ref held, 1) == 0)
            { entered.TrySetResult(true); await release.Task.WaitAsync(GateTimeout); }
        };
        var reopen = LocalMediaLibraryService.ReopenCollectionAsync(original.Token);
        Task<LocalMediaCollection> replacement = null;
        try
        {
            await entered.Task.WaitAsync(GateTimeout);
            replacement = LocalMediaLibraryService.ImportFolderAsync(folder);
            StorageCheck(gate == "manifest" ? replacement.IsCompletedSuccessfully : !replacement.IsCompleted,
                gate == "manifest" ? "reopen releases registry gate before manifest validation" : "reimport waits for admitted reopen through " + gate + " acquisition");
            if (gate != "manifest") StorageCheck(StorageApplicationPermissions.FutureAccessList.Contains(original.Token), "reopen's grant remains live during " + gate + " acquisition");
        }
        finally
        {
            release.TrySetResult(true);
            // Drain both even when the original source fails admitted reopen at access-restore.
            try { await reopen.WaitAsync(GateTimeout); StorageCheck(true, "admitted reopen succeeds after held " + gate); }
            catch (LocalMediaLibraryException exception) { StorageCheck(false, "admitted reopen succeeds after held " + gate + ": " + exception.Stage); }
            if (replacement != null) await replacement.WaitAsync(GateTimeout);
            StorageTestHooks.Reset();
        }
        StorageCheck(!StorageApplicationPermissions.FutureAccessList.Contains(original.Token) && StorageApplicationPermissions.FutureAccessList.Count == 1, "replacement after " + gate + " retires only old grant");
        await TypedFailure(LocalMediaLibraryService.ReopenCollectionAsync(original.Token), "access-restore");
        await CanReopen(replacement.Result.Token, "replacement after " + gate + " reopens");
    }

    private static async Task RegistryIdentityCases(string scratch)
    {
        var folder = MakeCollection(scratch, "identity-valid", "identity-a", "video/one.mp4");
        var importedFolder = MakeCollection(scratch, "identity-import", "identity-b", "video/one.mp4");
        var retryFolder = MakeCollection(scratch, "identity-retry", "identity-c", "video/one.mp4");
        var fixtures = new List<KeyValuePair<string, JToken>>
        {
            new KeyValuePair<string, JToken>("null-entry", JValue.CreateNull()),
            new KeyValuePair<string, JToken>("empty-entry", new JObject())
        };
        foreach (var field in new[] { "Token", "CollectionId", "RootPath" })
        foreach (var invalid in new[] { "missing", "null", "empty", "whitespace" })
        {
            var entry = new JObject { ["Token"] = "other-token", ["CollectionId"] = "other-id", ["RootPath"] = folder.Path };
            if (invalid == "missing") entry.Remove(field);
            else entry[field] = invalid == "null" ? JValue.CreateNull() : new JValue(invalid == "empty" ? "" : " \t\r\n");
            fixtures.Add(new KeyValuePair<string, JToken>(field + "-" + invalid, entry));
        }
        var caseNumber = 0;
        foreach (var fixture in fixtures)
        foreach (var beforeValid in new[] { true, false })
        foreach (var operation in new[] { "list", "reopen", "import" })
        {
            var label = fixture.Key + "/" + (beforeValid ? "before" : "after") + "/" + operation;
            ResetStorage(scratch, "identity-" + caseNumber++);
            var original = await LocalMediaLibraryService.ImportFolderAsync(folder).WaitAsync(GateTimeout);
            var validBytes = File.ReadAllText(RegistryPath);
            var validEntry = JObject.Parse(validBytes)["Entries"][0].DeepClone();
            var entries = beforeValid ? new JArray(fixture.Value.DeepClone(), validEntry) : new JArray(validEntry, fixture.Value.DeepClone());
            var malformedBytes = new JObject { ["Entries"] = entries }.ToString();
            File.WriteAllText(RegistryPath, malformedBytes);
            var temporaryPath = RegistryPath + ".tmp";
            const string temporaryBytes = "existing incomplete temporary bytes";
            File.WriteAllText(temporaryPath, temporaryBytes);
            var creates = 0; var writes = 0; var replacements = 0; var removals = 0;
            StorageTestHooks.BeforeCreate = name => { creates++; return Task.CompletedTask; };
            StorageTestHooks.BeforeWrite = (file, text) => writes++;
            StorageTestHooks.BeforeReplace = file => replacements++;
            StorageTestHooks.BeforeRemove = token => removals++;
            string observed = "success";
            try
            {
                if (operation == "list") await LocalMediaLibraryService.GetSavedCollectionsAsync().WaitAsync(GateTimeout);
                else if (operation == "reopen") await LocalMediaLibraryService.ReopenCollectionAsync(original.Token).WaitAsync(GateTimeout);
                else await LocalMediaLibraryService.ImportFolderAsync(importedFolder).WaitAsync(GateTimeout);
            }
            catch (LocalMediaLibraryException exception) { observed = exception.Stage + "/" + exception.CollectionId; }
            catch (Exception exception) { observed = exception.GetType().Name; }
            finally { StorageTestHooks.Reset(); }
            StorageCheck(observed == "registry-read/unknown", label + " rejects whole snapshot through typed registry-read/unknown: " + observed);
            StorageCheck(File.ReadAllText(RegistryPath) == malformedBytes, label + " preserves exact registry bytes");
            StorageCheck(File.Exists(temporaryPath) && File.ReadAllText(temporaryPath) == temporaryBytes, label + " preserves existing temporary bytes");
            StorageCheck(StorageApplicationPermissions.FutureAccessList.Count == 1 && StorageApplicationPermissions.FutureAccessList.Contains(original.Token), label + " preserves exact grant identity/count");
            // Any provisional grant still live changes Count; any removed one reaches BeforeRemove.
            StorageCheck(creates == 0 && writes == 0 && replacements == 0 && removals == 0, label + " has no provisional-grant/staging/replacement/retirement activity");

            // Restore only bytes; the same process-wide service and semaphore must recover after every operation kind.
            File.WriteAllText(RegistryPath, validBytes);
            var restored = await LocalMediaLibraryService.GetSavedCollectionsAsync().WaitAsync(GateTimeout);
            var reopened = await LocalMediaLibraryService.ReopenCollectionAsync(original.Token).WaitAsync(GateTimeout);
            StorageCheck(restored.Count == 1 && restored[0].Token == original.Token && reopened.Id == "identity-a", label + " releases gate for restored list and reopen");
            var retry = await LocalMediaLibraryService.ImportFolderAsync(retryFolder).WaitAsync(GateTimeout);
            var rows = await LocalMediaLibraryService.GetSavedCollectionsAsync().WaitAsync(GateTimeout);
            StorageCheck(rows.Count == 2 && rows.Any(row => row.Token == original.Token) && rows.Any(row => row.Token == retry.Token) && StorageApplicationPermissions.FutureAccessList.Count == 2 && rows.All(row => StorageApplicationPermissions.FutureAccessList.Contains(row.Token)), label + " retry preserves old/new rows and exactly reachable grants");
            await LocalMediaLibraryService.ReopenCollectionAsync(retry.Token).WaitAsync(GateTimeout);
        }

        var invalidCalls = caseNumber;
        foreach (var empty in new[] { "absent", "null", "{}", "{\"Entries\":null}", "{\"Entries\":[]}" })
        {
            ResetStorage(scratch, "empty-identity-" + caseNumber++);
            if (empty != "absent") File.WriteAllText(RegistryPath, empty);
            StorageCheck((await LocalMediaLibraryService.GetSavedCollectionsAsync().WaitAsync(GateTimeout)).Count == 0, "identity validation preserves empty default: " + empty);
        }
        ResetStorage(scratch, "identity-compatible");
        const string compatibleToken = "non-GUID-token";
        StorageApplicationPermissions.FutureAccessList.AddOrReplace(compatibleToken, folder, "Collection");
        var compatibleBytes = new JObject { ["Entries"] = new JArray(new JObject { ["Token"] = compatibleToken, ["CollectionId"] = "identity-a", ["RootPath"] = folder.Path }) }.ToString();
        File.WriteAllText(RegistryPath, compatibleBytes);
        var compatibleRows = await LocalMediaLibraryService.GetSavedCollectionsAsync().WaitAsync(GateTimeout);
        StorageCheck(compatibleRows.Count == 1 && compatibleRows[0].Token == compatibleToken && compatibleRows[0].Title == null && compatibleRows[0].ItemCount == 0, "identity-complete non-GUID token permits missing display metadata");
        await LocalMediaLibraryService.ReopenCollectionAsync(compatibleToken).WaitAsync(GateTimeout);
        StorageCheck(File.ReadAllText(RegistryPath) == compatibleBytes, "compatible reopen does not rewrite identity or metadata");
        File.Delete(Path.Combine(folder.Path, "video", "one.mp4"));
        StorageCheck((await LocalMediaLibraryService.GetSavedCollectionsAsync().WaitAsync(GateTimeout)).Count == 1, "identity-complete broken asset remains listed for repair");
        await TypedFailure(LocalMediaLibraryService.ReopenCollectionAsync(compatibleToken), "asset-resolution");
        StorageCheck(File.ReadAllText(RegistryPath) == compatibleBytes && StorageApplicationPermissions.FutureAccessList.Contains(compatibleToken), "broken asset keeps registry bytes and grant");
        File.WriteAllText(Path.Combine(folder.Path, "video", "one.mp4"), "media");
        await LocalMediaLibraryService.ReopenCollectionAsync(compatibleToken).WaitAsync(GateTimeout);
        StorageCheck(File.ReadAllText(RegistryPath) == compatibleBytes, "asset repair reopens complete identity without registry rewrite");
        await TypedFailure(LocalMediaLibraryService.ReopenCollectionAsync("unknown-valid-input"), "access-restore");
        StorageApplicationPermissions.FutureAccessList.Remove(compatibleToken);
        StorageCheck((await LocalMediaLibraryService.GetSavedCollectionsAsync().WaitAsync(GateTimeout)).Count == 1, "complete identity with stale grant remains listed without FAL lookup");
        await TypedFailure(LocalMediaLibraryService.ReopenCollectionAsync(compatibleToken), "access-restore");
        var argumentRejected = false;
        try { await LocalMediaLibraryService.ReopenCollectionAsync(" ").WaitAsync(GateTimeout); }
        catch (ArgumentException) { argumentRejected = true; }
        StorageCheck(argumentRejected, "blank public reopen token retains argument precondition");
        Console.WriteLine("FA-01 identity fixtures=" + fixtures.Count + "; placements=2; operations=3; invalid calls=" + invalidCalls);
        if (StorageFailures.Count != 0) throw new Exception("Storage identity integrity failed: " + string.Join("; ", StorageFailures));
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
