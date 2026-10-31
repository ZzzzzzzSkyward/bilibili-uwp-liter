using Newtonsoft.Json;

namespace BiliLite.Models.LocalMedia
{
    public class LocalMediaTrack
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("quality")]
        public string Quality { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("default")]
        public bool IsDefault { get; set; }

        [JsonProperty("enabledByDefault")]
        public bool EnabledByDefault { get; set; }
    }
}
