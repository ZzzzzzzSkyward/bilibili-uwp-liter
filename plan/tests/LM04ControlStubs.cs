using System.Collections.Generic;
using BiliLite.Models.LocalMedia;
using BiliLite.Modules.Player.Playurl;
using Windows.Storage;

namespace Windows.Storage
{
    public sealed class StorageFile
    {
        public string Path { get; set; }
        public string Name { get; set; }
    }
}

namespace BiliLite.Services.LocalMedia
{
    public sealed class LocalMediaPlaybackTrack
    {
        public LocalMediaTrack Track { get; set; }
        public StorageFile File { get; set; }
    }
}

namespace BiliLite.Controls
{
    using BiliLite.Services.LocalMedia;

    public sealed class LocalPlayInfo
    {
        public BiliPlayUrlInfo Info { get; set; }
        public string Quality { get; set; }
        public IList<LocalMediaPlaybackTrack> VideoTracks { get; set; }
        public IList<LocalMediaPlaybackTrack> AudioTracks { get; set; }
        public LocalMediaPlaybackTrack SelectedVideoTrack { get; set; }
        public LocalMediaPlaybackTrack SelectedAudioTrack { get; set; }
    }
}
