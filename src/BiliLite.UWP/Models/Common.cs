using Newtonsoft.Json;

namespace BiliLite.Models.Common
{
    /// <summary>
    /// �ʼ�ͼƬ
    /// </summary>
    public class NotePicture
    {
        /// <summary>
        /// ͼƬ��ַ	
        /// </summary>
        [JsonProperty("img_src")]
        public string ImgSrc { get; set; }

        /// <summary>
        /// ͼƬ���	
        /// </summary>
        [JsonProperty("img_width")]
        public float ImgWidth { get; set; }

        /// <summary>
        /// ͼƬ�߶�	
        /// </summary>
        [JsonProperty("img_height")]
        public float ImgHeight { get; set; }

        /// <summary>
        /// ͼƬ��С	
        /// </summary>
        [JsonProperty("img_size")]
        public float ImgSize { get; set; }
    }
}