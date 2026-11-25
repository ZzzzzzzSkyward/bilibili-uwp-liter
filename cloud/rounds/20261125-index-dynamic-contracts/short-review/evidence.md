# SR1 immutable discovery evidence

Owner `short_review_requirements`, 2026-11-25 UTC. **FROZEN / RELEASED**. Sole repository outputs are this file and [requirements.md](requirements.md). Exclusive private root **W**: `/workspace/work/index-dynamic-contracts/short-review-discovery/`. Raw source snapshots, fixtures, stdout, compiler outputs and tool bindings stay there. No product/test/main-doc/shared-project/Git-index/ref/cache/auth/dependency mutation, foreign execution or service request occurred.

## Immutable inputs and actual execution

Read AGENTS/CONTRACT/REPO, memory INDEX/current deadline, round plan/baseline/evidence-policy/discovery grant and accepted sibling inventory. Root's completed central startup remains authoritative; no fetch/checkout/pull over current owners. All local semantic discovery and private compiled inputs come from `git show 7fc3c524fde4139d01e7ecd362647ca771c64ea8:<path>`, not moving product working bytes.

`W/setup.py` copies **23 complete immutable inputs**, recording each Git blob, raw SHA256 and size in `W/source-manifest.json`. Fourteen complete real product sources compile: SeasonApi, ApiHelper (including the actual `BiliLite.Api.ApiModel`), AccountApi, SeasonReviewVM and its row models, IModules, RelayCommand, API data/result models, ReturnModel, CookieOrigins, CookieService, WebRefreshTransaction, PasswordLoginSession, RefreshCredentialPolicy and WbiKeyService. Counting is by file (fourteen), not types. The actual full HttpResults class is extracted unchanged from HttpHelper; the complete actual `Utils.ToInt32` method is extracted unchanged. Additional full sources inspected include Utils/HttpHelper and SeasonDetailPage code/XAML.

The private delivery hook captures actual ApiModel objects and returns synthetic HttpResults. Settings, logging/toast recording, platform cookie/storage delivery, timestamp and MD5 primitives are seams. There is no miniature builder, parser, cursor owner, encoding rule, signer or VM algorithm in stubs. Complete real response decoding, VM methods, RelayCommands and builder/ApiModel execute. Actual transport was inspected: Utils.cs:35–47 routes no-cookie GET to HttpHelper.Get(api.url, headers); HttpHelper.cs:116 constructs `new Uri(url)`. The fixture executes that same final URI boundary and `HttpUtility.ParseQueryString(uri.Query)` exactly once; it does not send the request.

| Immutable source | SHA256 |
| --- | --- |
| `Api/SeasonApi.cs` | `54dabff0be900eef7075850c8a08a05eee731db2e6054b92a192efa127b0ee80` |
| `Modules/Season/SeasonReviewVM.cs` | `c6e36bf65234c3dfc59471d4d3793891b4d4d033b2962e6e2e4912ca1d819abb` |
| `Api/ApiHelper.cs` | `dfb07f377db95023aebce4c68101c6d334444722cf79b07d7d39a6126ba302d9` |
| `Helpers/HttpHelper.cs` | `b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13` |
| `Helpers/Utils.cs` | `9db1e9c608e60ccd73e041ce3ebf5ba75cc205059714c9a22e35214c4c645952` |

No signing/credential value is printed or transferred into these repository artifacts. Real helper/signing source remains private; source hashes are sufficient here.

## Caller and sibling source facts

Immutable A `SeasonApi.cs:42–55` declares string next default empty, raw cursor interpolation, configured host, GET and optional logged-in access_key; no cookie flag or signer call. `SeasonReviewVM.cs:43–75` uses actual Next for the request and reads `data.next.ToString()` only while total exceeds accumulated rows; `:98–116` implements refresh/reset and LoadMore. `SeasonDetailPage.xaml.cs:31,49,186,689–694` constructs/assigns/calls the real VM; XAML `:289,354–357` binds Items, LoadMoreCommand and state. This establishes an active shipped browsing caller. Page/XAML were inspected as complete files; native page execution/rendering is not claimed.

S1 sources are pinned at `217fb54f51f6764176319bbdedab2a7522d75401`:

- [api.dart:854–866](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/http/api.dart#L854): exact short/long list and separate modern action/write route constants.
- [pgc.dart:113–132](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/http/pgc.dart#L113): nullable String next becomes named cursor in a query map with media_id, ps20, sort and web_location.
- [pgc_review_type.dart](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/models/common/pgc_review_type.dart): actual short enum maps to the short-list constant; default/latest sort0/1.
- [data.dart:3–16](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/models_new/pgc/pgc_review/data.dart#L3): String? next, count with total fallback.
- [child/controller.dart:14–60](https://github.com/bggRGjQaUbCoE/PiliPlus/blob/217fb54f51f6764176319bbdedab2a7522d75401/lib/pages/pgc_review/child/controller.dart#L14): response.next assigned to the same nullable String next and passed to the subsequent request. Refresh uses null; this different omission policy is not adopted.
- `http/constants.dart:3`, `http/init.dart:202–204,256–268`: api.bilibili.com base and named query map forwarded to Dio. No foreign library execution or inferred production-character distribution.

S3 pin `4789508a93f3a39b7d3c896273309db892a9cc8b`, `bilibili_api/bangumi.py:1172–1195`: actual short-comment method takes `Union[str,None]`, params media_id/ps20/sort and conditionally cursor=next. Its `data/api/bangumi.json:53–63` declares the same GET route but describes cursor as int; this inherited documentation discrepancy is not authority to convert local strings. S3 is narrowly maintained as qualified by the accepted inventory. S2 was checked only for frozen pin/clean custody; no S2-specific cursor claim is made.

`W/siblings-binding.json` binds nine inspected files. All three caches remain detached, clean and at accepted full pins; S1 raw files equal pinned blobs. S3's preexisting CRLF checkout is LF-equivalent to pinned LF blobs and raw/blob hashes are recorded separately. No cache was repinned or changed. GPL source observations are evidence only; no implementation was transferred.

## Exact regression and positive controls

New fixture `W/repro/Program.cs` is separate from original118/supplement34/G4179 and existing maintained assertions. **684 real requests** = 19 cursor cases × 2 hosts × 2 login states × 3 media IDs × 3 sort values, with five checks each, plus26 VM/default controls = **3446 assertions**. Every matrix check constructs the real request, then real ApiModel.url, `new Uri` and one query decode. Cases include null/empty/default, ordinary/numeric/leading-zero strings, plus, literal percent-looking text, delimiter/duplicate-key attempts, fragment, JSON-shaped text, Unicode, backslash/quote, CRLF and tab/NUL/control text.

| Frozen outcome | Checks | Failures | Interpretation |
| --- | ---: | ---: | --- |
| Active compatibility controls |1572|0| Ordinary/null/empty/numeric text, route/GET/cookie/signing/host and VM/error/reset behavior remain established. |
| Desired-safe boundary assertions |1874|488| 486 matrix failures and two actual VM append/retry failures. 1386 safe-boundary subchecks already pass. |
| Total |3446|488|2958 pass; process exit1 is expected old-source regression, not a green gate. |

The real VM fixtures first get a response with total3, one HTML-entity review and a String next, verify actual Next/content/row publication, then invoke actual LoadMoreCommand. A failed append keeps Next/rows, retry reuses it, terminal total2 preserves the old branch, and RefreshCommand resets cursor/rows. Ordinary and leading-zero cursor flows pass. The synthetic reserved-character token is stored intact in Next, but both subsequent final-URI cursor checks fail. MediaID0 remains the current no-request branch. No private VM flags or Next are assigned by the fixture to manufacture readiness.

## Commands, outputs and reproducible bindings

Tools: `/workspace/work/maintenance-foundation/quality/dotnet/dotnet`; SDK **8.0.427**, runtime **8.0.33**; Newtonsoft.Json **13.0.3**, existing local DLL. Environment: `DOTNET_ROOT` points to that dotnet directory; `DOTNET_CLI_HOME=W/dotnet-home`; `NUGET_PACKAGES=/workspace/work/api-shape-comparison/requirements-review/nuget` for the isolated link-only compilation.

1. `python3 W/setup.py` — exit0; immutable23 input extraction and private project setup.
2. `dotnet run --project W/repro/ShortReview.csproj` — builds actual full sources and records the same3446/488 outcome in `W/old-A.log`; the authoritative separately recorded executable exit is the replay below.
3. `dotnet run --no-build --project W/repro/ShortReview.csproj` — **exit1**,3446/488/1572positivePASS; `W/old-A-replay.log`.
4. `dotnet build W/api02-link-only/plan/tests/api02/API02.csproj --nologo --ignore-failed-sources` — **exit0**, zero errors/two existing lowercase-type warnings. Copies complete baseline API02 and all linked sources, with only a private additive link to actual SeasonApi; shared API01Stubs and original Program/checks stay byte-identical. This proves the prospective link compiles without shared stub or enum/type edits; existing API02 assertions were not rerun by this link-only check.
5. `python3 W/run-proof.py` and `python3 W/freeze.py` — exit0; exact commands/exits/counts, tool/source/fixture/output hashes, cache pin/clean checks and unchanged private source copies recorded.

| Private artifact relative to W | SHA256 |
| --- | --- |
| `repro/Program.cs` | `91a509e5ec041a9f179f308a8ef39d7f5061be48781123498a83ad177155ff9a` |
| `repro/ShortReview.csproj` | `81d345c8d661694d49c2bd6d8e1c469aefd34a0e8d14ef6cd3e8d4e86e4bc02a` |
| `repro/HttpResults.cs` | `46fbe440e03ff3d172322dbdd2ea7237665223b9ed073575a10d6fcc4aca6965` |
| `repro/ActualToInt32.cs` | `a5781d1d42b6685c659857cfb8e157bad8d0532d1cc13a1823bd0a5250a7c86c` |
| `repro/PlatformStubs.cs` | `864ecc5feb17f035048f2cf03d98dbf4319b7014d7260875972940c8da94bfca` |
| `source-manifest.json` | `7d60d1bf65e0be71323692731e36c231b76db10570959731a96110200d5f13d5` |
| `proof-binding.json` | `c3cc56659edc0efb098e90cf09bcd9a443d64632cb222e15f3d45d406f4050a2` |
| `siblings-binding.json` | `57e096149c7d6a204bd9a636d1cec6fd7afbd3793dff19161f1025982da029aa` |
| `old-A-replay.log` | `8c829b3709f6405cf1fdfa55b09878821fbc286791135c4d56f6bf3330925dac` |
| `api02-link-only.log` | `4967ea989b70f7426ea8a418776fed8fa6d4cc6b2bcce241e65002ac18fdb02d` |
| `release.json` | `ad1fbb96905673399100c679412f7c62234f002091ae12cc297cd61fabbc7f7d` |

`proof-binding.json` includes complete actual compiled-source hashes, dotnet/csc/CoreLib/Newtonsoft DLL hashes, exact command argument arrays and compiled ShortReview.dll hash. `release.json` additionally binds current eleven protected FI1/FD1/SeasonApi working paths without granting authority over them. No original118/supplement34/G4179 fixture or proof was written, and no full-suite/shared build ran. Future implementation must retain them and rebind exact current code separately.

All private processes ended and read/build custody is **released**. Discovery supplies one reviewed-ready proposal and real old-source failures; no repaired candidate, owning/full/native/hosted/live result or publication is asserted.
