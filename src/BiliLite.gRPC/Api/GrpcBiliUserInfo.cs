namespace BiliLite.gRPC.Api
{
    public class GrpcBiliUserInfo
    {
        public GrpcBiliUserInfo(string accessKey, long userMid, string appKey)
        {
            AccessKey = accessKey;
            UserMid = userMid;
            AppKey = appKey;
        }

        public string AccessKey { get; set; }

        public long UserMid { get; set; }

        public string AppKey { get; set; }
    }
}
