# Non-account boundary discovery evidence

Status: **CONFIRMED RED; requirements READY FOR INDEPENDENT REVIEW**. Snapshot `077ef35ae194d3ae85aa0621daeac07dc29e29ab`, dev, 2026-11-25 UTC. Only this stream's Markdown and `/workspace/work/recovery-integrity/boundaries/` were written. No product/test/build/main-document/index/history mutation, service request, account/session access or live write.

Read: AGENTS, cloud README/REPO/CONTRACT, memory INDEX plus relevant decisions/pitfalls/API-maintenance record, current round plan/intake-observations/discovery-coordination, prior PB1 requirements/evidence, ROADMAP and owning probe projects/programs/scripts. Root completed startup synchronization; this shared working copy has only authorized cloud work in progress.

## Actual-source scratch experiment

Scratch: `/workspace/work/recovery-integrity/boundaries/{setup.py,BoundaryProbe.csproj,Program.cs,Stubs.cs,CommentModels.stubs.cs,HttpResults.actual.cs,source-hashes.json,run.log}`. Project links current product parsers/DTOs, full `FavoriteDetailVM`, `VideoDetailVM`, `SeasonDetailVM`, actual IModules/RelayCommand/ApiModel/ReturnModel/VideoCollections and common models. The complete **actual HttpResults class body** is extracted from `Helpers/HttpHelper.cs`; its production JSON salvage and legacy GetData/GetJson behavior run unchanged. Only API request delivery, account-presence primitive, logging/toast capture, platform enum and comment display-model properties are stand-ins. No signing or transport conclusion is drawn from the fake delivery. `AsyncVoidTracker` waits for actual async-void operation completion, rather than timing a UI callback.

```bash
python3 /workspace/work/recovery-integrity/boundaries/setup.py
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/boundaries/dotnet-cli \
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/boundaries/BoundaryProbe.csproj -c Debug
```

Actual process **exit 1**, deliberately red; `CONFIRMED_VIOLATIONS=57 CONTROLS_PASSED=46`. This is a discovery harness, not the aggregate50/51 gate or Windows/UWP verification. Initial harness compile gaps were corrected only in scratch before this run. No existing assertion was altered.

| ID / priority | Executed input / actual result | Consequence / boundary |
| --- | --- | --- |
| BND-A / P1 | Four HTTP200 bodies `{}`, `{"data":{},"message":"not a success envelope"}`, `{"code":0.4}`, `{"code":"0"}` × actual FavoriteDetailVM Delete/Clean/DoCollect/DoCancelCollect/Sort = **20 false-success outcomes**. Production legacy decoder returns `success=true`; existing typed video action parser rejects every body. Delete removes its row, collect/cancel toggle flags, Sort says 排序成功, Clean clears rows before its forced follow-up list failure. | Missing/coerced business code is mistaken for an accepted server mutation. Does **not** prove any mutation was accepted remotely. |
| BND-A / P1 | Actual Sort with `not JSON` and JSON `null` = **2 NullReferenceExceptions**. | Sort has no exception boundary; real caller `Pages/User/FavoriteDetailPage.xaml.cs:187–198` is async void and awaits it without a catch. |
| BND-B / P2 | Like scalar `data`, coin `data.multiply`, follower `data.follower`, relation `data.attribute` × `2147483648`, `-2147483649`, `9223372036854775808`, `-9223372036854775809` = **16 exceptions** (OverflowException for Int32 overflow, InvalidCastException for Newtonsoft BigInteger). | Integer token check alone does not bound narrowing. VideoDetailVM's outer catch limits the runtime impact, but a bad like/coin read can skip independent later optional reads. No app-crash claim for these four parser paths. |
| BND-B / P2 | Legacy feed Parse and ParseReposts with `has_more` `9223372036854775808` and `-9223372036854775809` = **4 InvalidCastExceptions**. | Uncaught conversion escapes the typed parse result; source-selection/diagnostic boundary is bypassed. |
| BND-C / P2 | Season parser→mapper: `section:[null]`, `episodes:[null]`, `section:[{"episodes":[null]}]`, `areas:[null]`, `seasons:[null]` = **5 NullReferenceExceptions**. | First case escapes parser's post-decode normalization; four cases parse successfully then fail mapping. An otherwise usable episode list can fail as a whole. |
| BND-C / P2 | Comment parser→mapper: `replies:[null,{"rpid":1,...}]`, `top_replies:[null,{"rpid":1,...}]`, reply-list `replies:[null,{"rpid":1}]` = **3 NullReferenceExceptions**. | Null rows pass DTO decode then fail mapping. Wire ReplyCount remains a paging contract; simply deleting rows in parser would change it. |
| BND-D / P2 | For each actual DoLike/DoDislike/DoTriple/DoCoin/DoFavorite/DoAttentionUP: hold action response for video-A/owner-A, run **actual LoadVideoDetail("video-B")** to completion with ordinary fake replies, release `{"code":0}`, wait for actual async-void completion = **6 late mutations of video-B**. | Request target is A, current detail is B. The accepted continuation writes current VideoInfo, not its initiating detail. This is local identity corruption, with no inference about server toggle semantics. |
| BND-D / P2 | Hold A favorite-list refresh after accepted action; actual B load publishes B-folder; release A-folder = **1 stale replacement of MyFavorite**. | Current video stays B but its action list now belongs to A. Standalone `new VideoDetailVM().AttentionUP(...)` without VideoInfo passed a compatibility control; it has real staff/live callers and must remain supported. |

## Source and ownership observations

- BND-A: `Modules/Detail/FavoriteDetailVM.cs:184–257,304–395`; `Models/ApiModel.cs:3–30`; `Helpers/HttpHelper.cs:553–578,619–628`. Source builders `Api/User/FavoriteAPI.cs:80–106,195–210,258–267,289–302` already specify the existing endpoints/form/signing; no request change is needed. The existing `WebVideoActionParser` accepts valid code-zero **without data**, and reports Transport/Envelope/Rejected rather than default success.
- BND-B: `Parsing/Video/WebVideoStateParser.cs:48,71`, `WebRelationParser.cs:47,70`, `Parsing/Dynamic/LegacyDynamicFeedParser.cs:67,97,130`. `ApiEnvelope.TryReadBusinessCode` already demonstrates safe Int32 integer reading; PB1 business-code coverage stays closed/preserved. Legacy feed accepts bool or any Int64 nonzero; reposts only integer1 means more. Existing tests do not require broadening or narrowing those ordinary contracts.
- BND-C: `Parsing/Season/WebSeasonEpListParser.cs:80–94`; mapper `:38–60,75,131–137,166–174`; `Parsing/Comment/WebCommentPageMapper.cs:58–74,89–96,101–106`. Neighboring video/favorite/watch-later/media-list mappers already skip null rows. Comment `ReplyCount` is explicitly raw list length (`WebCommentPageMapper.cs:85`), and source-bound next offset is independent of mapped rows.
- BND-D: `Modules/Detail/VideoDetailVM.cs:374–621`; page reuses one VM (`VideoDetailPage.xaml.cs:75`), and sequential playlist/collection selection calls InitializeVideo. Its `flag` serializes **detail loads**, not an outstanding action. XAML action buttons have no shared in-flight ownership gate (`VideoDetailPage.xaml:94,101,161–162,308–309`). The scratch replacement uses the real load path, not direct property substitution.

## Ruled out / preserved / bounded follow-ups

1. **Large ID precision:** actual video parser/mapper preserves JSON integers `9007199254740993` (aid), `9007199254740995` (owner.mid), `9007199254740997` (cid) exactly as strings. Legacy `offset:9007199254740993` remains exact across bool/integer controls. There is no evidence for changing all IDs to a new schema or number type.
2. **DTO overflow already caught:** actual video `duration:9223372036854775808` and favorite nested `cnt_info.play` of that value return typed Payload failures. The hypothesis that every numeric DTO property throws past its parser is false. Do not replace all DTO deserialization.
3. **Ordinary scalar compatibility:** all four direct Int32 payload readers preserve 0,1,Int32.MinValue,Int32.MaxValue; absent follower stays successful0; favoured bool stays accepted. Like values other than0/1 and coin counts are currently folded by caller; this audit does not authorize new semantic range restrictions.
4. **Legacy compatibility:** bool true/false and integer0/1/2/-1/Int64.MaxValue feed pages stay successful; exact cursor preserved. String/other-token defaults are explicit existing behavior, not newly diagnosed defects. Reposts' `==1` behavior must stay independent from feed's `!=0` behavior.
5. **Null collection versus null element:** season null episodes/section collections already map to a valid empty list; actual video pages/staff null elements are skipped. Comment source declarations deliberately retain wire row count; the fix belongs in mapping skip logic, not count rewriting.
6. **Actions do not require data:** `{"code":0}` is a successful action, and actual favorite Delete correctly removes its row; `{"code":-403,"message":"denied"}` preserves it. Do not route actions through object-required ApiEnvelope.Read or demand a receipt object.
7. **Load concurrency false positive:** page InitializeVideo has a real `flag`/transition-ticket guard, so concurrent LoadVideoDetail alone is not promoted as a defect. BND-D is a different, reproduced action-versus-replacement path. No global player/session rewrite is justified.
8. **Decoder compatibility:** production longest-object salvage is executed, not replaced by stricter JObject-only fake. Neither the malformed-success findings nor their fix authorize changing HttpResults globally, held account parsing, authentication or protocol policy.
9. **Other inspected surfaces:** media-list/favorite/watch-later and video tag/relates DTO catches/null filtering, comment envelope/shape checks, dynamic mapper Obj guards/repost depth limit, wire string ID handling and season source/duplicate rules were inspected. Unknown dynamic card behavior and permissive cursor/string metadata policies stay as documented. This bounded audit is not an exhaustive input-space claim.

No current Windows/UWP runtime exists here. Native rendering, actual cookie/session behavior, signed requests against the service and real mutation confirmation remain unverified; the next safe step is independent requirements review, then owned local fixes and additive regressions in existing batches.

## Reproducer binding

Selected product SHA-256 values at the red run (full closure in scratch `source-hashes.json`):

```text
b12e457a75a133e9298fb77ad982b33986c2f95da2562b4f031b1fd692424d13  src/BiliLite.UWP/Helpers/HttpHelper.cs
5c0484ae131de6846fdab778ecade7c180e51b1fe55a4f53809fe90f737bdd99  src/BiliLite.UWP/Modules/Detail/FavoriteDetailVM.cs
66de7eb0ef0d1ed2fd0e87dca4558f2784ac78853448d7430b2f454068efd50f  src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs
469b46e1e03b5620947c7355808f4200229e8327134a14774ad1f233362cb6cc  src/BiliLite.UWP/Parsing/Video/WebVideoStateParser.cs
fa7d4ff8b2a7b1d66ff14267c7c8cb51f0230a49f34edff0d62b5c17ad404a54  src/BiliLite.UWP/Parsing/Video/WebRelationParser.cs
ca41be3cfe13dfb1f4ef7423ae7de7b3359bfeefe9c23fb9f4481e94bd602e0b  src/BiliLite.UWP/Parsing/Dynamic/LegacyDynamicFeedParser.cs
239a221228518294df53318d0c6a20c4184eaf4c5f230d4d4e79017af15fce72  src/BiliLite.UWP/Parsing/Season/WebSeasonEpListParser.cs
5ab4b114904d875b2a9115c6fb401fe12acccfbf842fc6004eb7c941c14e86ca  src/BiliLite.UWP/Parsing/Season/WebSeasonEpListMapper.cs
78196b8402677eb0080616d6d4d3e8ec132fc39ab2ff67a85d01c2f2c6fcf714  src/BiliLite.UWP/Parsing/Comment/WebCommentPageMapper.cs
```

Scratch Program.cs SHA-256: `42e7dea1b2e1cd70276bbed0ddb7b4b54c8ebeb432114b2c518ab807ea45d064`. The final run.log ends `CONFIRMED_VIOLATIONS=57 CONTROLS_PASSED=46`; every named control is true.
