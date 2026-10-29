using System.Collections.Generic;
using Newtonsoft.Json;

namespace BiliLite.Models.LocalMedia
{
    public class LocalMediaManifest
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("poster")]
        public string Poster { get; set; }

        [JsonProperty("items")]
        public List<LocalMediaItem> Items { get; set; } = new List<LocalMediaItem>();
    }
}
