# Logic/data/error source and malformed-input evidence

Date: 2026-11-24 UTC. No product, repository probe, build configuration, dependency or root documentation edits were made by this requirements owner. Scratch-only evidence compiles actual product parser files with a minimal HttpResults JSON transport seam. It does not exercise WinRT transport or app UI.

## Source command

```text
$ git rev-parse HEAD
exit=0
41dc562fba6e45896b0c4ba604f9ec38f4661e73
```

## Source command

```text
$ git status --short
exit=0
?? cloud/rounds/20261124-maintenance-foundation/
```

## Source command

```text
$ rg -n 'GetCSRF\(bool|GetCSRF\(true\)|body =' src/BiliLite.UWP/Api/ApiHelper.cs src/BiliLite.UWP/Api/CommentApi.cs
exit=0
src/BiliLite.UWP/Api/CommentApi.cs:71:            //string csrf = ApiHelper.GetCSRF(true);
src/BiliLite.UWP/Api/CommentApi.cs:85:            string csrf = ApiHelper.GetCSRF(true);
src/BiliLite.UWP/Api/CommentApi.cs:90:                body =  $"&oid={oid}&rpid={root}&action={action}&type={type}{csrf}",
src/BiliLite.UWP/Api/CommentApi.cs:99:            string csrf = ApiHelper.GetCSRF(true);
src/BiliLite.UWP/Api/CommentApi.cs:104:                body =  $"&oid={oid}&root={root}&parent={parent}&type={type}&message={message}{csrf}"
src/BiliLite.UWP/Api/CommentApi.cs:116:                body =  $"&oid={oid}&rpid={rpid}&type={type}&csrf={csrf}"
src/BiliLite.UWP/Api/CommentApi.cs:123:            string csrf = ApiHelper.GetCSRF(true);
src/BiliLite.UWP/Api/CommentApi.cs:128:                body = $"&oid={oid}&type={(int)type}&message={Uri.EscapeDataString(message)}{csrf}"
src/BiliLite.UWP/Api/ApiHelper.cs:70:        public static string GetCSRF(bool isparam = false) => Cookie.GetCSRF();
```

## Source command

```text
$ rg -n 'new LiveMessage|liveMessage.Connect' src/BiliLite.UWP/Modules/Detail/LiveRoomVM.cs
exit=0
625:                liveMessage = new LiveMessage();
730:                    liveMessage = new LiveMessage();
732:                await liveMessage.Connect(roomId, uid, cancelSource.Token);
```

## Source command

```text
$ rg -n 'new LiveDanmaku' src/BiliLite.UWP
exit=1
```

## Source command

```text
$ rg -n 'ServerUrl|roomid =|uid= uid|operation == 8|ConnectSuccess|ReceiveAsync|ParseData\(' src/BiliLite.UWP/Modules/Live/LiveMessage.cs
exit=0
31:        ConnectSuccess,
88:        public string ServerUrl { get; set; } = "wss://broadcastlv.chat.bilibili.com/sub";
109:                        await ws.ConnectAsync(new Uri(ServerUrl), cancellationToken);
122:                            WebSocketReceiveResult result = await ws.ReceiveAsync(buffer, cancellationToken);
127:                            ParseData(buffer.Take(result.Count).ToArray());
189:                    roomid = roomId,
190:                    uid= uid
221:        private void ParseData(byte[] data)
235:            if (operation == 8)
237:                NewMessage?.Invoke(MessageType.ConnectSuccess, "弹幕连接成功");
```

## Source command

```text
$ rg -n 'codeToken.Value<int>' src/BiliLite.UWP/Parsing
exit=0
src/BiliLite.UWP/Parsing/Video/VideoEnvelope.cs:40:            var code = codeToken.Value<int>();
src/BiliLite.UWP/Parsing/ApiEnvelope.cs:49:            var code = codeToken.Value<int>();
src/BiliLite.UWP/Parsing/Comment/WebCommentActionParser.cs:50:            var code = codeToken.Value<int>();
```

## Source command

```text
$ rg -n '2147483648|2147483649|9223372036854775808' plan/tests/comments/Program.cs plan/tests/c1/Program.cs plan/tests/season1/Program.cs
exit=1
```

## Interpretation and confidence

- U1 is proved by current source construction; the earlier research uses dummy token `abc`, producing `type=1abc` or a token-suffixed message without a `csrf` field. A source-linked builder regression is required before implementation; this evidence does not pretend those methods were compiled here.
- Active live chat is `LiveMessage`; constructor search found no active `new LiveDanmaku` caller. Search is limited to this repository, not an external plugin/reflection guarantee. The old class is still compiled and contains shared types; deleting it is outside scope.
- Existing comments/c1/season1 tests contain ordinary missing/noninteger/business-code tests but the searched out-of-range fixtures are absent. This is a coverage observation, not proof of the entire suite passing.
- The dedicated scratch experiment below makes the business-code issue a reproduced malformed-input defect, not a source-only hypothesis.

## Actual-source parser boundary experiment

The verification stream installed SDK 8.0.427/runtime 8.0.33 into scratch. This owner reused that executable and Newtonsoft.Json 13.0.3 assembly without installation or repository changes. The program links the actual `ApiEnvelope`, `VideoEnvelope`, `WebCommentActionParser`, `ApiParseResult` and action DTO source files. Only HttpResults transport/JSON decode is stubbed; each fixture is a complete, valid JSON object, so the stub does not exercise or replace range conversion.

Command environment:

```text
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet
DOTNET_CLI_HOME=/workspace/work/maintenance-foundation/logic/dotnet-cli
DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET_NOLOGO=1
$ /workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/maintenance-foundation/logic/parser-boundary/ParserBoundaryProbe.csproj -c Debug
exit=1
ApiEnvelope.data code=2147483648 THREW OverflowException
ApiEnvelope.data code=-2147483649 THREW OverflowException
ApiEnvelope.data code=9223372036854775808 THREW InvalidCastException
ApiEnvelope.data code=-9223372036854775809 THREW InvalidCastException
ApiEnvelope.data in-range code=2147483647 preserved rejection=True
ApiEnvelope.data in-range code=-2147483648 preserved rejection=True
ApiEnvelope.data in-range code=-404 preserved rejection=True
ApiEnvelope.data code=0 empty objects success=True
ApiEnvelope.result code=2147483648 THREW OverflowException
ApiEnvelope.result code=-2147483649 THREW OverflowException
ApiEnvelope.result code=9223372036854775808 THREW InvalidCastException
ApiEnvelope.result code=-9223372036854775809 THREW InvalidCastException
ApiEnvelope.result in-range code=2147483647 preserved rejection=True
ApiEnvelope.result in-range code=-2147483648 preserved rejection=True
ApiEnvelope.result in-range code=-404 preserved rejection=True
ApiEnvelope.result code=0 empty objects success=True
VideoEnvelope code=2147483648 THREW OverflowException
VideoEnvelope code=-2147483649 THREW OverflowException
VideoEnvelope code=9223372036854775808 THREW InvalidCastException
VideoEnvelope code=-9223372036854775809 THREW InvalidCastException
VideoEnvelope in-range code=2147483647 preserved rejection=True
VideoEnvelope in-range code=-2147483648 preserved rejection=True
VideoEnvelope in-range code=-404 preserved rejection=True
VideoEnvelope code=0 empty objects success=True
CommentAction code=2147483648 THREW OverflowException
CommentAction code=-2147483649 THREW OverflowException
CommentAction code=9223372036854775808 THREW InvalidCastException
CommentAction code=-9223372036854775809 THREW InvalidCastException
CommentAction in-range code=2147483647 preserved rejection=True
CommentAction in-range code=-2147483648 preserved rejection=True
CommentAction in-range code=-404 preserved rejection=True
CommentAction code=0 empty objects success=True
CommentAction code=0 no-data success=True
CONTRACT_VIOLATIONS=16
```

The nonzero exit is intentional: malformed input violated the required typed-failure contract 16 times. In-range codes and success controls passed. The first shell invocation printed this output with `cat`, which masked its process status; the subprocess run recorded above captures the **actual dotnet process exit 1**.

### Linked-source hashes

```text
50605d79f59cfca01341b223b0612893f5e6f73215455543e072db1203a35d5f  src/BiliLite.UWP/Parsing/ApiEnvelope.cs
f708c0a8cf8cddad1aa2f3b0abc1346cde0a10cf37313c4fee9aecd3c0c0e3ab  src/BiliLite.UWP/Parsing/ApiParseResult.cs
a0628961af607af485b7faf8bbe15eb05051b7efb1e69198b6296b65624e0590  src/BiliLite.UWP/Parsing/Video/VideoEnvelope.cs
e5d322818aa1563f1decbbe62cb2931666a31a72ed9255d69eaab0ad771877a5  src/BiliLite.UWP/Parsing/Comment/WebCommentActionParser.cs
af2a2f3c2ca5f76390d5d21fa1976ed91fea64baa254bb3561fbc77acbd10dc1  src/BiliLite.UWP/Models/Api/Comment/WebCommentActionResultDto.cs
```

### Scratch project (exact)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><Nullable>disable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup>
    <Reference Include="Newtonsoft.Json"><HintPath>/workspace/work/maintenance-foundation/quality/cache/nuget/newtonsoft.json/13.0.3/lib/net6.0/Newtonsoft.Json.dll</HintPath></Reference>
    <Compile Include="/workspace/bilibili-uwp-liter/src/BiliLite.UWP/Parsing/ApiParseResult.cs" Link="ApiParseResult.cs" />
    <Compile Include="/workspace/bilibili-uwp-liter/src/BiliLite.UWP/Parsing/ApiEnvelope.cs" Link="ApiEnvelope.cs" />
    <Compile Include="/workspace/bilibili-uwp-liter/src/BiliLite.UWP/Parsing/Video/VideoEnvelope.cs" Link="VideoEnvelope.cs" />
    <Compile Include="/workspace/bilibili-uwp-liter/src/BiliLite.UWP/Parsing/Comment/WebCommentActionParser.cs" Link="WebCommentActionParser.cs" />
    <Compile Include="/workspace/bilibili-uwp-liter/src/BiliLite.UWP/Models/Api/Comment/WebCommentActionResultDto.cs" Link="WebCommentActionResultDto.cs" />
  </ItemGroup>
</Project>
```

### Scratch program (exact)

```csharp
using BiliLite.Helpers;
using BiliLite.Parsing;
using BiliLite.Parsing.Comment;
using BiliLite.Parsing.Video;
using Newtonsoft.Json.Linq;

int violations = 0;
string[] invalidCodes = { "2147483648", "-2147483649", "9223372036854775808", "-9223372036854775809" };
var readers = new (string Name, Func<HttpResults, (bool Success, ApiParseFailure Failure, string Stage, int Code, string Message)> Read)[] {
  ("ApiEnvelope.data", response => View(ApiEnvelope.Read(response))),
  ("ApiEnvelope.result", response => View(ApiEnvelope.ReadResult(response))),
  ("VideoEnvelope", response => View(VideoEnvelope.ReadDataToken(response))),
  ("CommentAction", response => View(WebCommentActionParser.Parse(response)))
};
foreach (var (name, read) in readers) {
  foreach (string code in invalidCodes) {
    try {
      var result = read(Ok("{\"code\":" + code + ",\"data\":{},\"result\":{}}"));
      bool valid = !result.Success && result.Failure == ApiParseFailure.Envelope && result.Stage == "envelope.code";
      Console.WriteLine($"{name} code={code} returned success={result.Success} failure={result.Failure} stage={result.Stage}; expected typed-envelope={valid}");
      if (!valid) violations++;
    } catch (Exception error) {
      Console.WriteLine($"{name} code={code} THREW {error.GetType().Name}");
      violations++;
    }
  }
  foreach (string code in new[] { "2147483647", "-2147483648", "-404" }) {
    var result = read(Ok("{\"code\":" + code + ",\"message\":\"fixture rejection\",\"data\":{},\"result\":{}}"));
    bool valid = !result.Success && result.Failure == ApiParseFailure.Rejected && result.Code.ToString() == code && result.Message == "fixture rejection";
    Console.WriteLine($"{name} in-range code={code} preserved rejection={valid}");
    if (!valid) violations++;
  }
  var zero = read(Ok("{\"code\":0,\"data\":{},\"result\":{}}"));
  Console.WriteLine($"{name} code=0 empty objects success={zero.Success}");
  if (!zero.Success) violations++;
}
var actionNoData = WebCommentActionParser.Parse(Ok("{\"code\":0}"));
Console.WriteLine($"CommentAction code=0 no-data success={actionNoData.IsSuccess}");
if (!actionNoData.IsSuccess) violations++;
Console.WriteLine($"CONTRACT_VIOLATIONS={violations}");
return violations == 0 ? 0 : 1;

static HttpResults Ok(string json) => new HttpResults { status = true, code = 200, results = json };
static (bool Success, ApiParseFailure Failure, string Stage, int Code, string Message) View<T>(ApiParseResult<T> result) =>
  (result.IsSuccess, result.Failure, result.Stage, result.BusinessCode, result.Message);

namespace BiliLite.Helpers {
  public sealed class HttpResults {
    public bool status; public int code; public string message; public string results;
    public bool TryGetJObject(out JObject body) {
      try { body = JObject.Parse(results); return true; }
      catch { body = null; return false; }
    }
  }
}
```
