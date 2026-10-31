using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BiliLite.gRPC;
using BiliLite.gRPC.Api;
using Bilibili.App.Playurl.V1;
using Proto.Header;

internal static class Program
{
    private const string ANDROID_KEY = "1d8b6e7d45233436";
    private const string IOS_KEY = "27eb53fc9058f8c3";
    private const string LOGIN_KEY = "4409e2ce8ffd12b8";

    private static int _assertions;
    private static readonly Assembly Lib = typeof(GrpcRequest).Assembly;
    private static readonly Type FactoryType = Lib.GetType("BiliLite.gRPC.Api.GrpcRequestHeaderFactory", true);
    private static readonly Type HeaderInterfaceType = Lib.GetType("BiliLite.gRPC.Api.IGrpcRequestHeader", true);
    private static readonly MethodInfo FactoryMethod = FactoryType.GetMethod("GetGrpcRequestHeader", BindingFlags.Public | BindingFlags.Static);

    private static int Main()
    {
        try
        {
            FactoryResolvesKnownKeys();
            ResolveHeaderNeverNull();
            AndroidHeaders();
            AndroidMetadataBin();
            IosHeaders();
            IosAuroraEid();
            TraceIdInvariants();
            FactoryStability();
            SharedClientHasNoProfileHeaders();
            HeadersReachTheWire();
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL " + ex);
            return 1;
        }

        Console.WriteLine($"PASS {_assertions} assertions");
        return 0;
    }

    private static void Check(bool condition, string what)
    {
        if (!condition) throw new Exception("assertion failed: " + what);
        _assertions++;
    }

    private static void CheckEqual(object expected, object actual, string what)
    {
        var e = expected == null ? "<null>" : expected.ToString();
        var a = actual == null ? "<null>" : actual.ToString();
        if (!string.Equals(e, a, StringComparison.Ordinal))
            throw new Exception($"assertion failed: {what} (expected '{e}', got '{a}')");
        _assertions++;
    }

    private static object HeaderFor(string appKey)
    {
        return FactoryMethod.Invoke(null, new object[] { appKey });
    }

    private static string TypeName(object o)
    {
        return o == null ? "<null>" : o.GetType().Name;
    }

    private static Dictionary<string, string> GetHeaders(object header, GrpcBiliUserInfo userInfo)
    {
        var method = HeaderInterfaceType.GetMethod("GetHeaders");
        return (Dictionary<string, string>)method.Invoke(header, new object[] { userInfo });
    }

    private static object ResolveHeader(GrpcBiliUserInfo userInfo)
    {
        var method = typeof(GrpcRequest).GetMethod("ResolveHeader", BindingFlags.NonPublic | BindingFlags.Static);
        return method.Invoke(null, new object[] { userInfo });
    }

    private static string Unbase64(string value)
    {
        var padded = value + new string('=', (4 - value.Length % 4) % 4);
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    private static void FactoryResolvesKnownKeys()
    {
        CheckEqual("AndroidGrpcRequestHeader", TypeName(HeaderFor(ANDROID_KEY)), "factory android key");
        CheckEqual("IosGrpcRequestHeader", TypeName(HeaderFor(IOS_KEY)), "factory ios key");
        Check(HeaderFor(null) == null, "factory null key -> null");
        Check(HeaderFor("") == null, "factory empty key -> null");
        Check(HeaderFor(LOGIN_KEY) == null, "factory login key -> null");
        Check(HeaderFor("custom") == null, "factory custom key -> null");
    }

    private static void ResolveHeaderNeverNull()
    {
        CheckEqual("IosGrpcRequestHeader", TypeName(ResolveHeader(new GrpcBiliUserInfo("ak", 123, IOS_KEY))), "resolve ios key");
        CheckEqual("AndroidGrpcRequestHeader", TypeName(ResolveHeader(new GrpcBiliUserInfo("ak", 123, ANDROID_KEY))), "resolve android key");
        CheckEqual("AndroidGrpcRequestHeader", TypeName(ResolveHeader(new GrpcBiliUserInfo("ak", 0, LOGIN_KEY))), "resolve login key");
        CheckEqual("AndroidGrpcRequestHeader", TypeName(ResolveHeader(new GrpcBiliUserInfo("ak", 0, ""))), "resolve empty key");
        CheckEqual("AndroidGrpcRequestHeader", TypeName(ResolveHeader(new GrpcBiliUserInfo("ak", 0, null))), "resolve null key");
        CheckEqual("AndroidGrpcRequestHeader", TypeName(ResolveHeader(new GrpcBiliUserInfo("ak", 0, "custom"))), "resolve custom key");
        CheckEqual("AndroidGrpcRequestHeader", TypeName(ResolveHeader(null)), "resolve null user info");
    }

    private static Dictionary<string, string> AndroidHeaders()
    {
        var userInfo = new GrpcBiliUserInfo("ak-123", 0, ANDROID_KEY);
        var headers = GetHeaders(HeaderFor(ANDROID_KEY), userInfo);

        var expectedKeys = new[]
        {
            "User-Agent", "APP-KEY", "x-bili-metadata-bin", "authorization", "x-bili-device-bin",
            "x-bili-network-bi", "x-bili-restriction-bin", "x-bili-locale-bin", "x-bili-fawkes-req-bin",
            "grpc-accept-encoding", "grpc-timeout", "env", "Transfer-Encoding", "TE",
        };
        CheckEqual(
            string.Join("|", expectedKeys.OrderBy(x => x, StringComparer.Ordinal)),
            string.Join("|", headers.Keys.OrderBy(x => x, StringComparer.Ordinal)),
            "android key set");

        CheckEqual("Dalvik/2.1.0 (Linux; U; Android 9; Xiaomi MIUI) 6.7.0 os/android model/MIUI mobi_app/android build/6070600 channel/bilibili140 innerVer/6070600 osVer/9 network/1",
            headers["User-Agent"], "android user agent");
        CheckEqual("android", headers["APP-KEY"], "android APP-KEY");
        CheckEqual("identify_v1 ak-123", headers["authorization"], "android authorization");
        Check(headers.ContainsKey("x-bili-network-bi"), "android x-bili-network-bi present");
        Check(!headers.ContainsKey("x-bili-network-bin"), "android x-bili-network-bin absent");
        CheckEqual("", headers["x-bili-restriction-bin"], "android restriction bin");
        CheckEqual("17985446u", headers["grpc-timeout"], "android grpc-timeout");
        CheckEqual("prod", headers["env"], "android env");
        CheckEqual("trailers", headers["TE"], "android TE");
        CheckEqual("chunked", headers["Transfer-Encoding"], "android transfer encoding");
        CheckEqual("identity", headers["grpc-accept-encoding"], "android grpc-accept-encoding");
        Check(!headers.ContainsKey("x-bili-mid"), "android has no x-bili-mid");
        Check(headers.Values.All(x => x != null), "android header values non-null");
        return headers;
    }

    private static void AndroidMetadataBin()
    {
        var userInfo = new GrpcBiliUserInfo("ak-123", 0, ANDROID_KEY);
        var headers = GetHeaders(HeaderFor(ANDROID_KEY), userInfo);

        foreach (var name in new[] { "x-bili-metadata-bin", "x-bili-device-bin", "x-bili-network-bi", "x-bili-locale-bin", "x-bili-fawkes-req-bin" })
            Check(!headers[name].Contains("="), "android " + name + " has no base64 padding");

        var metadata = Metadata.Parser.ParseFrom(
            Convert.FromBase64String(headers["x-bili-metadata-bin"] + new string('=', (4 - headers["x-bili-metadata-bin"].Length % 4) % 4)));
        CheckEqual("ak-123", metadata.AccessKey, "metadata access key");
        CheckEqual("android", metadata.MobiApp, "metadata mobi app");
        CheckEqual(6070600, metadata.Build, "metadata build");
        CheckEqual("bilibili140", metadata.Channel, "metadata channel");
        CheckEqual("XZFD48CFF1E68E637D0DF11A562468A8DC314", metadata.Buvid, "metadata buvid");
        CheckEqual("android", metadata.Platform, "metadata platform");

        var device = Device.Parser.ParseFrom(
            Convert.FromBase64String(headers["x-bili-device-bin"] + new string('=', (4 - headers["x-bili-device-bin"].Length % 4) % 4)));
        CheckEqual(1, device.AppId, "device app id");
        CheckEqual("MIUI", device.Model, "device model");
        CheckEqual("Xiaomi", device.Brand, "device brand");

        var network = Network.Parser.ParseFrom(
            Convert.FromBase64String(headers["x-bili-network-bi"] + new string('=', (4 - headers["x-bili-network-bi"].Length % 4) % 4)));
        CheckEqual(Network.Types.TYPE.Wifi, network.Type, "network type");
        CheckEqual("46007", network.Oid, "network oid");
    }

    private static void IosHeaders()
    {
        var userInfo = new GrpcBiliUserInfo("ak-ios", 123456789, IOS_KEY);
        var headers = GetHeaders(HeaderFor(IOS_KEY), userInfo);

        CheckEqual("123456789", headers["x-bili-mid"], "ios mid");
        CheckEqual("Y3436F391696935149CCA03C1537118D2080", headers["buvid"], "ios buvid");
        Check(headers.ContainsKey("x-bili-trace-id"), "ios trace id present");
        Check(headers.ContainsKey("x-bili-aurora-eid"), "ios aurora eid present");
        CheckEqual("", headers["x-bili-exps-bin"], "ios exps bin");
        Check(headers.ContainsKey("x-bili-network-bin"), "ios network bin present");
        Check(!headers.ContainsKey("x-bili-network-bi"), "ios network-bi absent");
        CheckEqual("trailers", headers["te"], "ios te");
        Check(!headers.ContainsKey("TE"), "ios no upper-case TE");
        CheckEqual("identify_v1 ak-ios", headers["authorization"], "ios authorization");
        Check(headers["User-Agent"].StartsWith("bili-universal/76700100 os/ios model/iPad 9G mobi_app/iphone osVer/15.5 network/2", StringComparison.Ordinal),
            "ios user agent");

        foreach (var name in new[] { "x-bili-metadata-bin", "x-bili-device-bin", "x-bili-network-bin", "x-bili-locale-bin", "x-bili-fawkes-req-bin" })
            Check(!headers[name].Contains("="), "ios " + name + " has no base64 padding");

        var guest = GetHeaders(HeaderFor(IOS_KEY), new GrpcBiliUserInfo("", 0, IOS_KEY));
        CheckEqual("", guest["authorization"], "ios empty access key authorization");
        CheckEqual("", guest["x-bili-aurora-eid"], "ios guest aurora eid");
        CheckEqual("0", guest["x-bili-mid"], "ios guest mid");
    }

    private static void IosAuroraEid()
    {
        var header = HeaderFor(IOS_KEY);
        var configType = Lib.GetType("BiliLite.gRPC.Api.IosGrpcRequestHeaderConfig", true);
        var config = Activator.CreateInstance(configType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new object[] { new GrpcBiliUserInfo("ak", 123456789, IOS_KEY) }, null);
        var method = configType.GetMethod("GetAuroraEid", BindingFlags.Public | BindingFlags.Instance);
        var actual = (string)method.Invoke(config, null);

        // 独立算一遍:mid 的 UTF-8 字节按 12 字节密钥循环异或,再做去掉 '=' 的 base64
        var key = new byte[] { 97, 100, 49, 118, 97, 52, 54, 97, 55, 108, 122, 97 };
        var midBytes = Encoding.UTF8.GetBytes("123456789");
        var expected = Convert.ToBase64String(
            midBytes.Select((b, i) => (byte)(b ^ key[i % 12])).ToArray()).TrimEnd('=');
        CheckEqual(expected, actual, "aurora eid xor");

        var guestConfig = Activator.CreateInstance(configType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new object[] { new GrpcBiliUserInfo("ak", 0, IOS_KEY) }, null);
        CheckEqual("", method.Invoke(guestConfig, null), "aurora eid for mid 0");

        // 头部里的 eid 与 config 计算一致
        var headers = GetHeaders(header, new GrpcBiliUserInfo("ak", 123456789, IOS_KEY));
        CheckEqual(actual, headers["x-bili-aurora-eid"], "ios header aurora eid matches config");
    }

    private static void TraceIdInvariants()
    {
        var configType = Lib.GetType("BiliLite.gRPC.Api.IosGrpcRequestHeaderConfig", true);
        var config = Activator.CreateInstance(configType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new object[] { new GrpcBiliUserInfo("ak", 1, IOS_KEY) }, null);
        var method = configType.GetMethod("GenTraceId", BindingFlags.Public | BindingFlags.Instance);

        var first = (string)method.Invoke(config, null);
        var second = (string)method.Invoke(config, null);

        CheckEqual(53, first.Length, "trace id length");
        CheckEqual(':', first[32], "trace id colon 32");
        CheckEqual(':', first[49], "trace id colon 49");
        CheckEqual(':', first[51], "trace id colon 51");
        Check(first.EndsWith(":0:0", StringComparison.Ordinal), "trace id suffix :0:0");
        Check(first.Substring(0, 32).All(c => "0123456789abcdefghijklmnopqrstuvwxyz".IndexOf(c) >= 0), "trace id prefix charset");
        // 第二段是随机 id 的第 16-31 位原样拷贝,第 24-29 位是时间戳的 6 位小写十六进制
        CheckEqual(first.Substring(16, 16), first.Substring(33, 16), "trace id second segment copies chars 16-31");
        Check(first.Substring(24, 6).All(c => "0123456789abcdef".IndexOf(c) >= 0), "trace id timestamp is lowercase hex");
        Check(first != second, "trace id differs per call");
    }

    private static void FactoryStability()
    {
        Check(ReferenceEquals(HeaderFor(ANDROID_KEY), HeaderFor(ANDROID_KEY)), "android header singleton");
        Check(ReferenceEquals(HeaderFor(IOS_KEY), HeaderFor(IOS_KEY)), "ios header singleton");
        Check(ReferenceEquals(ResolveHeader(new GrpcBiliUserInfo("a", 0, "custom")), ResolveHeader(new GrpcBiliUserInfo("b", 0, null))),
            "fallback instance shared");
    }

    private static void SharedClientHasNoProfileHeaders()
    {
        var field = typeof(GrpcRequest).GetField("SharedHttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        var client = (System.Net.Http.HttpClient)field.GetValue(null);
        CheckEqual(TimeSpan.FromSeconds(100), client.Timeout, "shared client timeout");
        Check(!client.DefaultRequestHeaders.Contains("APP-KEY"), "shared client has no APP-KEY");
        Check(!client.DefaultRequestHeaders.Contains("authorization"), "shared client has no authorization");
        Check(!client.DefaultRequestHeaders.Contains("x-bili-metadata-bin"), "shared client has no metadata bin");
        Check(!client.DefaultRequestHeaders.Contains("TE"), "shared client has no TE");
        Check(client.DefaultRequestHeaders.Count() == 0, "shared client has no default headers");
    }

    private static string CaptureWireHead(GrpcBiliUserInfo userInfo, out GrpcResult result)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        string requestHead = null;

        var server = Task.Run(() =>
        {
            using (var client = listener.AcceptTcpClient())
            using (var stream = client.GetStream())
            {
                var buffer = new byte[4096];
                var sb = new StringBuilder();
                while (sb.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal) < 0)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;
                    sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }
                requestHead = sb.ToString();
                // 6 字节响应体:5 字节 gRPC 前缀 + 1 字节数据
                var response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 6\r\nConnection: close\r\n\r\n\0\0\0\0\0\0");
                stream.Write(response, 0, response.Length);
                stream.Flush();
            }
        });

        result = GrpcRequest.Instance
            .SendMessage("http://127.0.0.1:" + port + "/probe", new PlayViewReq(), userInfo)
            .GetAwaiter().GetResult();
        server.Wait(5000);
        listener.Stop();
        return requestHead;
    }

    private static void HeadersReachTheWire()
    {
        GrpcResult result;
        var requestHead = CaptureWireHead(new GrpcBiliUserInfo("wire-ak", 0, ANDROID_KEY), out result);

        Check(requestHead != null, "wire request captured");
        Check(result.status, "wire request succeeded: " + result.message);
        var head = requestHead.ToLowerInvariant();
        Check(head.Contains("\r\napp-key: android"), "wire APP-KEY");
        Check(head.Contains("\r\nauthorization: identify_v1 wire-ak"), "wire authorization");
        Check(head.Contains("\r\ntransfer-encoding: chunked"), "wire transfer-encoding chunked");
        Check(head.Contains("\r\nte: trailers"), "wire TE trailers");
        Check(head.Contains("\r\nx-bili-network-bi: "), "wire network-bi");
        Check(!head.Contains("\r\nx-bili-network-bin:"), "wire has no network-bin for android");
        Check(head.Contains("\r\nx-bili-metadata-bin: "), "wire metadata bin");
        Check(requestHead.StartsWith("POST /probe HTTP/1.1", StringComparison.Ordinal), "wire request line");

        // iOS 档必须真的自解析头走到线上,而不是只靠工厂映射
        var iosHead = CaptureWireHead(new GrpcBiliUserInfo("wire-ios", 123, IOS_KEY), out result);
        Check(iosHead != null, "ios wire request captured");
        Check(result.status, "ios wire request succeeded: " + result.message);
        var iosLower = iosHead.ToLowerInvariant();
        Check(iosLower.Contains("\r\nx-bili-mid: 123"), "ios wire mid");
        Check(iosLower.Contains("\r\nx-bili-network-bin: "), "ios wire network-bin");
        Check(!iosLower.Contains("app-key"), "ios wire has no app-key");

        // null 用户信息归一化后应是一次真正的匿名 Android 请求,而不是异常兜底结果
        var anonHead = CaptureWireHead(null, out result);
        Check(result.status, "anonymous wire request succeeded: " + result.message);
        Check(anonHead != null, "anonymous wire request captured");
        var anonLower = anonHead.ToLowerInvariant();
        Check(anonLower.Contains("\r\napp-key: android"), "anonymous wire APP-KEY");
        Check(anonLower.Contains("\r\nauthorization: identify_v1 \r\n"), "anonymous wire authorization");
    }
}
