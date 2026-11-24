# Exact BND-A red and green evidence

Baseline dev/origin/dev: `077ef35ae194d3ae85aa0621daeac07dc29e29ab`. SDK8.0.427, runtime/ref8.0.33 reused from `/workspace/work/maintenance-foundation/quality/dotnet`. Scratch `/workspace/work/recovery-integrity/favorite-execution/`; `red/` and `green/` contain immutable copies of all28 compile/runner/decoder inputs, manifests, run.log and exit-code. The only differing input is the owned FavoriteDetailVM. Red source equals baseline SHA-256 `5c0484ae131de6846fdab778ecade7c180e51b1fe55a4f53809fe90f737bdd99`; green `2a0e667cd6d6bb066df581e7ae27a2277ab4c529c3a65db388940b8e06af7e1b`.

Commands, from repository root, before and after the bounded source repair:

```bash
python3 /workspace/work/recovery-integrity/favorite-execution/snapshot.py red
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/favorite-execution/dotnet-cli \
bash plan/tests/c2/run.sh > /workspace/work/recovery-integrity/favorite-execution/red/run.log 2>&1
# exit134; Favorite action checks: passed=462 failed=101
python3 /workspace/work/recovery-integrity/favorite-execution/snapshot.py green
UPSTREAM_UNIT_TOOLS=/workspace/work/maintenance-foundation/quality \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/favorite-execution/dotnet-cli \
bash plan/tests/c2/run.sh > /workspace/work/recovery-integrity/favorite-execution/green/run.log 2>&1
# exit0; Favorite action checks: passed=563 failed=0; PASS451 assertions
python3 /workspace/work/recovery-integrity/favorite-execution/verify-preservation.py
# exit0: original Program/assertions preserved; exact closure differs only in VM;
# current executed inputs match green snapshot
```

Initial harness preparation caught an incorrect **new** Clean success expectation: the existing VM keeps Page1 when returned rows equal media_count. That new expectation was corrected before the recorded red phase; it is1 in both frozen red/green suites. No existing assertion was removed or modified. The first development run and helper scripts are retained in scratch, separate from the final bound evidence.

Every action tests12 malformed envelopes: missing code `{}`, missing code with data/message, fractional0.4, string"0", null, bool, both Int32-overflow literals and both beyond-Int64 literals, non-JSON and JSON-null. Each also receives null response, failed HTTP503 with supplied message, asynchronously thrown request, message/msg business rejection, code-only success and production-compatible prefixed/suffixed JSON-object salvage success. Primitive asynchronous delivery forces real VM continuations. SynchronizationContext operation and posted-callback counters wait for actual async-void completion, capture escaping exceptions and drain nested Clean refresh; no sleeps/timing assumptions. Tests assert unchanged rows/info/page/flags and no refresh on failure, exact failure text, one request except successful Clean's one follow-up list request, actual row removal once, collect flag publication once, refreshed rows and Sort success exactly once. Request exceptions traverse actual IModules logging.

The recorded red101 failed checks include the20 originally observed false-success actions and10 Sort exception escapes (null/wrong/overflow code, non-JSON/JSON-null, missing response and request exception). Ordinary code-only and salvage success controls passed during red. Green retains all original451 assertions, including signed/unsigned MediaListService queues and FavoriteAPI form/signing controls. The generated actual decoder emits expected CS1998 and linked RelayCommand unused-event warnings; there are no compile errors. All95 action cases and asynchronous fixture callbacks completed within the one c2 process; it performs no network request, creates no service process and mutates no account/session.

| Artifact | SHA-256 |
| --- | --- |
| red/hashes.json | `8aa7c61977a8647e4b8ede34854a2c7e48d70391a9c9e259d680beafe128f709` |
| green/hashes.json | `fd91dea7ecbfb9980db9d579eaf960c80c96ac8c7c33056e93d19cb5fe8a450c` |
| red/run.log | `4c3963fe0211b69c41e5bab8ff877ac45c150d0e20e567431fbd5213b05f1a79` |
| green/run.log | `954d084862318380bf5401b9ae2f7cf261ee7dd8c12cbba4c86b50be7830bf14` |

Complete green input binding follows; red is identical except the VM hash recorded above.

| Exact tested path | SHA-256 |
| --- | --- |
| `plan/tests/c2/C2Probe.csproj` | `242c9c855cd7543e9af2923d4bffcdf5a7d510bcc90f77578b6922163ecb399c` |
| `plan/tests/c2/C2Stubs.cs` | `ccccf197a344e6fbe7d5d597a769d4cc1fcf19d976d68f34e3f6b856b66fc8b9` |
| `plan/tests/c2/FavoriteActionChecks.cs` | `032422e6dbd784dc7507d0250ac2b0cfc3e67d5ce825b4623a66e01fbc5346be` |
| `plan/tests/c2/Program.cs` | `04b62fe038a3888a57b8828e4ab29a272f4511cf7027f6c3a219405adeed2d4b` |
| `plan/tests/c2/obj/HttpResults.actual.cs` | `6df7b868c05bfa188a13c15601c17215f5c81437ccae7214aaa7e0d70a22f4d2` |
| `plan/tests/c2/run.sh` | `808681b957b83a14054b3fe2dc080a0eeb64a52e9dfda5494dbe3dd2d8c7357a` |
| `src/BiliLite.UWP/Api/User/FavoriteAPI.cs` | `1e1d56bdeb5b9716e49c6160e92a3a31bcd17362121db0a80bbd7717bb81105d` |
| `src/BiliLite.UWP/Helpers/HttpHelper.cs` | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| `src/BiliLite.UWP/Models/Api/MediaList/FavoriteResourceListDto.cs` | `ae05ea7b1d958a2cb1c2b0bf9df19ca8d44133b4cc8f57471765c02623992403` |
| `src/BiliLite.UWP/Models/Api/MediaList/MediaListResourceListDto.cs` | `dd0e851b024d3019eb0dc9efa2b09a70e4615d33420ad016d522a4b37648f75d` |
| `src/BiliLite.UWP/Models/Api/MediaList/WatchLaterListDto.cs` | `8a9d2461c8b95cb5ae719df9087fe77572cfc0e53dd17deb0660767d817ba5ae` |
| `src/BiliLite.UWP/Models/ApiModel.cs` | `6493168767c4c9a92637b3ac82b9ec7d67e92ac2840cf0680655887996611671` |
| `src/BiliLite.UWP/Models/MediaListResources.cs` | `28d8575924e1629c70c9967dd9384c2f71bead2a49844d4089ba9735df05a7fc` |
| `src/BiliLite.UWP/Models/ReturnModel.cs` | `0c5192f37d4447954937a8b226b5667dba77ed09f3d5a345b226e1e6c8994a1c` |
| `src/BiliLite.UWP/Modules/Detail/FavoriteDetailVM.cs` | `2a0e667cd6d6bb066df581e7ae27a2277ab4c529c3a65db388940b8e06af7e1b` |
| `src/BiliLite.UWP/Modules/IModules.cs` | `cef5334b540d7e74513d59b513e3d7671fce98e9be62b7d50bad471df14ed3c6` |
| `src/BiliLite.UWP/Modules/RelayCommand.cs` | `2941ad18ba1a0aed08d0803cb6ddead223d20e750d4541f4284ecb828a1c74e8` |
| `src/BiliLite.UWP/Parsing/ApiEnvelope.cs` | `050317cddde277c46c6169f3878cb6c7accef3cec0dbbf018006cbc411948421` |
| `src/BiliLite.UWP/Parsing/ApiParseResult.cs` | `f708c0a8cf8cddad1aa2f3b0abc1346cde0a10cf37313c4fee9aecd3c0c0e3ab` |
| `src/BiliLite.UWP/Parsing/MediaList/FavoriteResourceListMapper.cs` | `d8edcbf20b2c16e10b359c9e81ff05be8f92d2f4b585ed49b242664afb512c59` |
| `src/BiliLite.UWP/Parsing/MediaList/FavoriteResourceListParser.cs` | `44282186da546e910455c3e76fc9be1cf5eeb59a293e6e9f9cd258d03b962f06` |
| `src/BiliLite.UWP/Parsing/MediaList/MediaListFailureText.cs` | `100ad1d115b1830467d75fcd151d950e30f68f0ac8e3a2655ad20e57f0ad2122` |
| `src/BiliLite.UWP/Parsing/MediaList/MediaListResourceListMapper.cs` | `321ecca555c437294781519f3ad3edf87380e13e7aa7bf5aea49ae63e5454215` |
| `src/BiliLite.UWP/Parsing/MediaList/MediaListResourceListParser.cs` | `3465f247ad6da58afa510a6b6bd4143647368189d88ec332625a3e4dca9c20b7` |
| `src/BiliLite.UWP/Parsing/MediaList/WatchLaterListParser.cs` | `aa353652aff0efa4c6d979acff40e5ee906263e6f0baea9d86502d51b0949cc9` |
| `src/BiliLite.UWP/Parsing/Video/VideoEnvelope.cs` | `286b4cd4cba70378cf1bdb0e57f0b850d221cd40d3d228a9e05f70ab14ede82d` |
| `src/BiliLite.UWP/Parsing/Video/WebVideoActionParser.cs` | `ba0d9f2f482539b6327053ed0601af50a72907889a37f3148b5516fbac5500cf` |
| `src/BiliLite.UWP/Services/MediaListService.cs` | `836ad612b8740600ad6a5c1e33a2d0f5913ec726877cd134b9b6f10593e00f6d` |
