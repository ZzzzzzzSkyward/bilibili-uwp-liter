using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Models.LocalMedia;
using BiliLite.Modules.Player.Playurl;
using BiliLite.Services.LocalMedia;
using Windows.Storage;
using Windows.Storage.AccessCache;

internal static class LM03
{
    private static async Task Main()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "lm03-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(rootPath, "video"));
        Directory.CreateDirectory(Path.Combine(rootPath, "audio"));
        Directory.CreateDirectory(Path.Combine(rootPath, "subs"));
        File.WriteAllText(Path.Combine(rootPath, "video", "main.mkv"), "video");
        File.WriteAllText(Path.Combine(rootPath, "video", "alternate.mp4"), "video");
        File.WriteAllText(Path.Combine(rootPath, "audio", "main.flac"), "audio");
        File.WriteAllText(Path.Combine(rootPath, "subs", "en.json"), "{}");
        File.WriteAllText(Path.Combine(rootPath, "danmaku.xml"), "<i/>");
        try
        {
            var root = CreateFolder(rootPath);
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("saved-token", root, "Collection");
            var videoDefault = new LocalMediaTrack { Id = "v-main", Path = "video/main.mkv", Label = "Main", IsDefault = true };
            var videoAlternate = new LocalMediaTrack { Id = "v-alt", Path = "video/alternate.mp4", Label = "Alternate" };
            var audioDefault = new LocalMediaTrack { Id = "a-main", Path = "audio/main.flac", Label = "Japanese", IsDefault = true };
            var item = new LocalMediaItem
            {
                Id = "ep:01",
                Title = "Episode 1",
                VideoTracks = { videoDefault, videoAlternate },
                AudioTracks = { audioDefault },
                SubtitleTracks = { new LocalMediaTrack { Id = "en", Path = "subs/en.json", IsDefault = false } },
                DanmakuTracks = { new LocalMediaTrack { Id = "bili", Path = "danmaku.xml", EnabledByDefault = true } }
            };
            var collection = new LocalMediaCollection
            {
                Id = "series:a",
                Token = "saved-token",
                Manifest = new LocalMediaManifest { Id = "series:a", Items = { item, new LocalMediaItem
                {
                    Id = "ep:02", Title = "Episode 2",
                    VideoTracks = { new LocalMediaTrack { Id = "v2", Path = "video/main.mkv" } }
                } } }
            };

            var playlist = await LocalMediaPlaybackAdapter.CreatePlaylistAsync(collection, 1);
            Assert(playlist.PlayInfos.Count == 2 && playlist.Index == 1, "playlist preserves all items and selected index");
            var playInfo = playlist.PlayInfos[0];
            Assert(playInfo.title == "Episode 1" && playInfo.play_mode == BiliLite.Controls.VideoPlayType.LoaclFile, "manifest item maps into existing local playback mode");
            Assert(playInfo.LocalPlayInfo.Info.PlayUrlType == BiliPlayUrlType.DASH, "manifest item uses the existing DASH player model");
            Assert(playInfo.LocalPlayInfo.Info.DashInfo.Video.file.Path.EndsWith("video/main.mkv", StringComparison.Ordinal), "default video resolves to a StorageFile handle");
            Assert(playInfo.LocalPlayInfo.Info.DashInfo.Audio.file.Path.EndsWith("audio/main.flac", StringComparison.Ordinal), "default external audio resolves to a StorageFile handle");
            Assert(playInfo.LocalPlayInfo.VideoTracks.Count == 2 && playInfo.LocalPlayInfo.SelectedVideoTrack.Track == videoDefault, "alternate video tracks remain typed for LM-04");
            Assert(playInfo.LocalPlayInfo.SelectedAudioTrack.Track == audioDefault, "selected audio track remains typed for LM-04");
            Assert(playInfo.LocalPlayInfo.SubtitleTracks.Count == 1 && !playInfo.LocalPlayInfo.SubtitleTracks[0].IsDefault, "subtitle metadata is retained without implicit activation");
            Assert(playInfo.LocalPlayInfo.DanmakuTracks.Single().EnabledByDefault, "danmaku metadata is retained for LM-05");
            Assert(playInfo.LocalPlayInfo.SubtitleFiles.Single().File.Path.EndsWith("subs/en.json", StringComparison.Ordinal), "subtitle file resolves through saved collection grant");
            Assert(playInfo.LocalPlayInfo.DanmakuFiles.Single().File.Path.EndsWith("danmaku.xml", StringComparison.Ordinal), "danmaku file resolves through saved collection grant");

            var repeated = await LocalMediaPlaybackAdapter.CreatePlaylistAsync(collection, 0);
            var sameIdentity = await LocalMediaPlaybackAdapter.CreatePlaylistAsync(collection, 0);
            Assert(repeated.PlayInfos[0].cid == sameIdentity.PlayInfos[0].cid, "resume identity is stable between adapter runs");
            var pairA = MakeCollection("a:b", "c", root);
            var pairB = MakeCollection("a", "b:c", root);
            var idA = (await LocalMediaPlaybackAdapter.CreatePlaylistAsync(pairA, 0)).PlayInfos[0].cid;
            var idB = (await LocalMediaPlaybackAdapter.CreatePlaylistAsync(pairB, 0)).PlayInfos[0].cid;
            Assert(idA != idB, "resume identities remain distinct when component boundaries differ");

            var broken = new LocalMediaCollection
            {
                Id = "broken",
                Token = "saved-token",
                Manifest = new LocalMediaManifest { Id = "broken", Items = { new LocalMediaItem
                {
                    Id = "bad-item", Title = "Missing video",
                    VideoTracks = { new LocalMediaTrack { Id = "missing", Path = "missing.mp4" } }
                } } }
            };
            try
            {
                await LocalMediaPlaybackAdapter.CreatePlaylistAsync(broken, 0);
                throw new Exception("missing track resolution should fail");
            }
            catch (LocalMediaPlaybackException exception)
            {
                Assert(exception.Stage == "track-resolution" && exception.CollectionId == "broken" && exception.ItemId == "bad-item", "adapter failures include stage and collection/item identifiers");
                var trackFailure = exception.InnerException as LocalMediaTrackLoadException;
                Assert(trackFailure?.TrackId == "missing" && trackFailure.InnerException is FileNotFoundException, "adapter failure retains track id and full underlying storage exception");
            }

            Console.WriteLine("LM-03 checks passed: playlist mapping, default StorageFile tracks, saved-grant subtitle/danmaku handles, retained metadata, explicit subtitle-off behavior, stable resume IDs, and contextual failures.");
        }
        finally
        {
            Directory.Delete(rootPath, true);
        }
    }

    private static LocalMediaCollection MakeCollection(string collectionId, string itemId, StorageFolder folder)
    {
        return new LocalMediaCollection
        {
            Id = collectionId,
            Token = "saved-token",
            Manifest = new LocalMediaManifest { Id = collectionId, Items = { new LocalMediaItem
            {
                Id = itemId, Title = itemId,
                VideoTracks = { new LocalMediaTrack { Id = "v", Path = "video/main.mkv" } }
            } } }
        };
    }

    private static StorageFolder CreateFolder(string path)
    {
        return (StorageFolder)Activator.CreateInstance(typeof(StorageFolder), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new object[] { path }, null);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
