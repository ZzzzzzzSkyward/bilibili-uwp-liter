using System;
using System.Text;
using Newtonsoft.Json.Linq;

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

        private static readonly string[] SecretNames =
        {
            "token", "cookie", "csrf", "key", "sign", "secret", "sessdata", "bili_jct", "w_rid", "wts",
        };

        /// <summary>
        /// 正文样本：丢掉名字像凭据的字段，再按字节上限截断。宁可有损，也不把凭据写进日志。
        /// </summary>
        public static string RedactBody(string body, int maxBytes)
        {
            if (string.IsNullOrEmpty(body))
            {
                return "";
            }
            var text = body;
            try
            {
                var token = JToken.Parse(body);
                DropSecrets(token);
                text = token.ToString(Newtonsoft.Json.Formatting.None);
            }
            catch (Exception)
            {
                // 不是 JSON 就照原文截断，正文内容由调用方负责选择要不要采
            }
            if (maxBytes > 0 && Encoding.UTF8.GetByteCount(text) > maxBytes)
            {
                var builder = new StringBuilder();
                var bytes = 0;
                foreach (var c in text)
                {
                    var size = Encoding.UTF8.GetByteCount(c.ToString());
                    if (bytes + size > maxBytes)
                    {
                        break;
                    }
                    builder.Append(c);
                    bytes += size;
                }
                text = builder.Append("…").ToString();
            }
            return text;
        }

        private static void DropSecrets(JToken token)
        {
            var obj = token as JObject;
            if (obj != null)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (var property in obj.Properties())
                {
                    if (IsSecretName(property.Name))
                    {
                        names.Add(property.Name);
                    }
                    else
                    {
                        DropSecrets(property.Value);
                    }
                }
                foreach (var name in names)
                {
                    obj[name] = "<redacted>";
                }
                return;
            }
            var array = token as JArray;
            if (array != null)
            {
                foreach (var element in array)
                {
                    DropSecrets(element);
                }
            }
        }

        private static bool IsSecretName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            var lower = name.ToLowerInvariant();
            foreach (var secret in SecretNames)
            {
                if (lower.Contains(secret))
                {
                    return true;
                }
            }
            return false;
        }    }
}
