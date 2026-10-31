using System.Collections.Generic;
using BiliLite.Models.LocalMedia;
using BiliLite.Services.LocalMedia;

namespace BiliLite.Controls
{
    public enum VideoPlayType { LoaclFile }

    public sealed class PlayInfo
    {
        public VideoPlayType play_mode { get; set; }
        public int order { get; set; }
        public string avid { get; set; }
        public string cid { get; set; }
        public string title { get; set; }
        public LocalPlayInfo LocalPlayInfo { get; set; }
    }

    public sealed class LocalPlayInfo
    {
        public BiliLite.Modules.Player.Playurl.BiliPlayUrlInfo Info { get; set; }
        public string Quality { get; set; }
        public LocalMediaItem ManifestItem { get; set; }
        public IList<LocalMediaPlaybackTrack> VideoTracks { get; set; }
        public IList<LocalMediaPlaybackTrack> AudioTracks { get; set; }
        public LocalMediaPlaybackTrack SelectedVideoTrack { get; set; }
        public LocalMediaPlaybackTrack SelectedAudioTrack { get; set; }
        public IList<LocalMediaTrack> SubtitleTracks { get; set; }
        public IList<LocalMediaTrack> DanmakuTracks { get; set; }
        public IList<LocalMediaPlaybackTrack> SubtitleFiles { get; set; }
        public IList<LocalMediaPlaybackTrack> DanmakuFiles { get; set; }
        public string ManifestCollectionId { get; set; }
        public string ResumeId { get; set; }
    }
}

namespace BiliLite.Services.LocalMedia
{
    using BiliLite.Models.LocalMedia;

    public sealed class LocalMediaCollection
    {
        public string Token { get; set; }
        public string Id { get; set; }
        public LocalMediaManifest Manifest { get; set; }
    }
}
