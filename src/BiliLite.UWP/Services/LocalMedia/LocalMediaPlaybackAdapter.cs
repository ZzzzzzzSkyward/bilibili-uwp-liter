using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BiliLite.Controls;
using BiliLite.Models.LocalMedia;
using BiliLite.Modules.Player.Playurl;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace BiliLite.Services.LocalMedia
{
    public sealed class LocalMediaPlaybackException : Exception
    {
        public LocalMediaPlaybackException(string stage, string collectionId, string itemId, Exception innerException)
            : base("The selected local media collection could not be prepared for playback.", innerException)
        {
            Stage = stage;
            CollectionId = collectionId;
            ItemId = itemId;
        }

        public string Stage { get; private set; }
        public string CollectionId { get; private set; }
        public string ItemId { get; private set; }
    }

    public sealed class LocalMediaPlaybackTrack
    {
        public LocalMediaTrack Track { get; set; }
        public StorageFile File { get; set; }
    }

    public sealed class LocalMediaPlaybackPlaylist
    {
        public List<PlayInfo> PlayInfos { get; set; }
        public int Index { get; set; }
    }

    public static class LocalMediaPlaybackAdapter
    {
        public static async Task<LocalMediaPlaybackPlaylist> CreatePlaylistAsync(LocalMediaCollection collection, int selectedIndex)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            var stage = "collection-validation";
            string itemId = "unknown";
            try
            {
                if (collection.Manifest == null || collection.Manifest.Items == null || collection.Manifest.Items.Count == 0)
                    throw new InvalidOperationException("The collection has no validated playable items.");
                if (selectedIndex < 0 || selectedIndex >= collection.Manifest.Items.Count)
                    throw new ArgumentOutOfRangeException(nameof(selectedIndex));

                stage = "folder-access";
                var root = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(collection.Token);
                var playInfos = new List<PlayInfo>(collection.Manifest.Items.Count);
                for (var index = 0; index < collection.Manifest.Items.Count; index++)
                {
                    var item = collection.Manifest.Items[index];
                    itemId = item.Id;
                    stage = "track-resolution";
                    var selectedVideo = item.VideoTracks.FirstOrDefault(track => track.IsDefault) ?? item.VideoTracks.First();
                    var selectedAudio = item.AudioTracks.FirstOrDefault(track => track.IsDefault) ?? item.AudioTracks.FirstOrDefault();
                    var videoTracks = await ResolveTracksAsync(root, item.VideoTracks);
                    var audioTracks = await ResolveTracksAsync(root, item.AudioTracks);
                    var video = videoTracks.First(track => ReferenceEquals(track.Track, selectedVideo));
                    var audio = selectedAudio == null ? null : audioTracks.First(track => ReferenceEquals(track.Track, selectedAudio));

                    var info = new BiliPlayUrlInfo
                    {
                        HasPlayUrl = true,
                        PlayUrlType = BiliPlayUrlType.DASH,
                        QualityName = selectedVideo.Label ?? selectedVideo.Quality ?? selectedVideo.Id,
                        QualityID = -1,
                        DashInfo = new BiliDashPlayUrlInfo
                        {
                            Video = new BiliDashItem { IsVideo = true, Url = video.File.Path, file = video.File },
                            Audio = audio == null ? null : new BiliDashItem { IsVideo = false, Url = audio.File.Path, file = audio.File }
                        }
                    };
                    var localInfo = new LocalPlayInfo
                    {
                        Info = info,
                        Quality = info.QualityName,
                        ManifestItem = item,
                        VideoTracks = videoTracks,
                        AudioTracks = audioTracks,
                        SelectedVideoTrack = video,
                        SelectedAudioTrack = audio,
                        SubtitleTracks = item.SubtitleTracks.ToList(),
                        DanmakuTracks = item.DanmakuTracks.ToList(),
                        ManifestCollectionId = collection.Id,
                        ResumeId = CreateResumeId(collection.Id, item.Id)
                    };
                    playInfos.Add(new PlayInfo
                    {
                        play_mode = VideoPlayType.LoaclFile,
                        order = index,
                        avid = "0",
                        cid = localInfo.ResumeId,
                        title = item.Title,
                        LocalPlayInfo = localInfo
                    });
                }

                return new LocalMediaPlaybackPlaylist { PlayInfos = playInfos, Index = selectedIndex };
            }
            catch (LocalMediaPlaybackException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new LocalMediaPlaybackException(stage, collection.Id ?? "unknown", itemId, exception);
            }
        }

        private static async Task<List<LocalMediaPlaybackTrack>> ResolveTracksAsync(StorageFolder root, IEnumerable<LocalMediaTrack> tracks)
        {
            var resolved = new List<LocalMediaPlaybackTrack>();
            foreach (var track in tracks)
            {
                resolved.Add(new LocalMediaPlaybackTrack
                {
                    Track = track,
                    File = await ResolveFileAsync(root, track.Path)
                });
            }
            return resolved;
        }

        private static async Task<StorageFile> ResolveFileAsync(StorageFolder root, string relativePath)
        {
            var segments = relativePath.Split('/');
            var folder = root;
            for (var index = 0; index < segments.Length - 1; index++)
                folder = await folder.GetFolderAsync(segments[index]);
            return await folder.GetFileAsync(segments[segments.Length - 1]);
        }

        private static string CreateResumeId(string collectionId, string itemId)
        {
            return "local:" + Encode(collectionId) + ":" + Encode(itemId);
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
