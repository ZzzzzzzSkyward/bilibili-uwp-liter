using Newtonsoft.Json;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// ± º«Õº∆¨
    /// </summary>
    public class NotePicture
    {
        /// <summary>
        /// Õº∆¨µÿ÷∑	
        /// </summary>
        [JsonProperty("img_src")]
        public string ImgSrc { get; set; }

        /// <summary>
        /// Õº∆¨øÌ∂»	
        /// </summary>
        [JsonProperty("img_width")]
        public float ImgWidth { get; set; }

        /// <summary>
        /// Õº∆¨∏ﬂ∂»	
        /// </summary>
        [JsonProperty("img_height")]
        public float ImgHeight { get; set; }

        /// <summary>
        /// Õº∆¨¥Û–°	
        /// </summary>
        [JsonProperty("img_size")]
        public float ImgSize { get; set; }
    }
}