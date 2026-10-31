namespace BiliLite.Api
{
    public enum EmoteBusiness
    {
        /// <summary>
        /// 评论区
        /// </summary>
        reply,
        /// <summary>
        /// 动态
        /// </summary>
        dynamic
    }

    public class EmoteApi : BiliLite.Models.Requests.Api.EmoteApi { }
}
