using System.Collections.Generic;
using BiliLite.Models.LocalMedia;
using Windows.Storage;

namespace BiliLite.Modules.Player
{
    public sealed class SubtitleModel { public List<SubtitleItemModel> body { get; set; } }
    public sealed class SubtitleItemModel
    {
        public double from { get; set; }
        public double to { get; set; }
        public int location { get; set; }
        public string content { get; set; }
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

namespace NSDanmaku.Model
{
    public sealed class DanmakuModel { public int time_s { get; set; } }
}
