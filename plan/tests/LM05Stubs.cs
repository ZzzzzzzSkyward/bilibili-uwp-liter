using System.Collections.Generic;
using BiliLite.Models.LocalMedia;
using Windows.Storage;

namespace BiliLite.Modules
{
    public sealed class SubtitleModel
    {
        public double font_size { get; set; }
        public string font_color { get; set; }
        public double background_alpha { get; set; }
        public string background_color { get; set; }
        public string Stroke { get; set; }
        public List<SubtitleItemModel> body { get; set; }
    }
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
