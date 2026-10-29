using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.LocalMedia
{
    public class LocalMediaItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("videoTracks")]
        public List<LocalMediaTrack> VideoTracks { get; set; } = new List<LocalMediaTrack>();

        [JsonProperty("audioTracks")]
        public List<LocalMediaTrack> AudioTracks { get; set; } = new List<LocalMediaTrack>();

        [JsonProperty("subtitleTracks")]
        public List<LocalMediaTrack> SubtitleTracks { get; set; } = new List<LocalMediaTrack>();

        [JsonProperty("danmakuTracks")]
        public List<LocalMediaTrack> DanmakuTracks { get; set; } = new List<LocalMediaTrack>();
    }
}
