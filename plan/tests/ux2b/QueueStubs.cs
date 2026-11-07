namespace BiliLite.Services
{
    /// <summary>
    /// Models/VideoQueue.cs 只用到这个常量,服务本身依赖 UWP 的传输层,probe 不引入。
    /// 值必须和 src/BiliLite.UWP/Services/MediaListService.cs 保持一致。
    /// </summary>
    public class MediaListService
    {
        public const int DefaultMediaListType = 1;
    }
}
