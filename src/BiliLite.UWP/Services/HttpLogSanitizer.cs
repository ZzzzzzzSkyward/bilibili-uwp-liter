using System;

namespace BiliLite.Services
{
    public static class HttpLogSanitizer
    {
        public static string DescribeEndpoint(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return "unparsed-endpoint";
            var path = uri.GetLeftPart(UriPartial.Path);
            const string correspondMarker = "/correspond/1/";
            var marker = path.IndexOf(correspondMarker, StringComparison.OrdinalIgnoreCase);
            if (marker >= 0) return path.Substring(0, marker + correspondMarker.Length) + "<redacted>";
            return path;
        }

        public static bool ShouldLogApiMessage(string endpoint)
        {
            return endpoint == null ||
                (endpoint.IndexOf("passport.bilibili.com", StringComparison.OrdinalIgnoreCase) < 0 &&
                 endpoint.IndexOf("/x/safecenter/", StringComparison.OrdinalIgnoreCase) < 0);
        }
    }
}
