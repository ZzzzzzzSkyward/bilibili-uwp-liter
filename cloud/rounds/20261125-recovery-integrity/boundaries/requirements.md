# Non-account parser and action boundaries

Status: **READY FOR INDEPENDENT REQUIREMENTS REVIEW**. Author: boundary_requirements. Snapshot `077ef35ae194d3ae85aa0621daeac07dc29e29ab`, dev, 2026-11-25 UTC. Evidence and exact scratch command: [evidence.md](evidence.md). Actual-source discovery run: **57 confirmed violations / 46 passing controls, process exit1**. This is intentional red evidence, not an aggregate suite result. Product implementation has not begun.

## Goal, scope and preserved contracts

Correct four reproduced boundary failures: malformed favorite action replies becoming success, unbounded payload integer conversions, null collection rows crashing otherwise usable pages, and late video action results mutating a replacement detail. All are local and executable without a service account; include supported P2 repairs in this round.

Keep builders, endpoints, form fields, exactly-once text encoding, cookies, saved identities, signing input, data-versus-result selection and business-code/error categories unchanged. Keep production HttpResults salvage, PB1 safeguards, existing assertions, ordinary scalar coercion/default policy, source-bound paging and season duplicate/source rules. Actions may succeed with `{"code":0}` and no `data`. No held account-parser work, protocol migration, global decoder tightening, schema migration, XAML redesign, player/session rewrite, live writes or service-availability claim.

## BND-A — favorite actions require an actual accepted envelope (P1)

**Defect:** actual FavoriteDetailVM Delete/Clean/DoCollect/DoCancelCollect/Sort deserialize an ApiDataModel whose absent code defaults to0; fractional/string codes can coerce to0. Four malformed bodies × five callers produced20 false-success effects. Sort also throws NullReferenceException for non-JSON/JSON-null; its real async-void drag-completion caller has no catch.

**Minimal change:** use the existing `Parsing/Video/WebVideoActionParser.ParseWebVideoAction` for these five action replies. It already owns the required code-only action shape, including favorites in video-detail actions. Keep API signatures and request construction untouched; do not change ApiDataModel globally or add another action decoder. Wrap Sort's whole request/parse path in an exception boundary, consistent with the neighboring task methods. No exception may escape Sort for null reply, failed delivery, undecodable body or request exception.

**Publication/error contract:**

- Only a typed successful action authorizes row removal, Clean's Refresh, collect/cancel flags, or 排序成功. A failure retains current rows/flags and does not start Clean's follow-up list request.
- Preserve existing ordinary success effects and business/HTTP rejection message when present; malformed envelope/payload gets a short local action failure message (`操作失败`; sorting may retain its `排序失败` prefix), missing response gets the existing network message. Do not show a favorite action failure as “视频详情读取失败”. A small private caller text helper is sufficient; no global message refactor.
- A transport exception uses the existing HandelError/logging path and a failure message. Do not expose raw response/credential content. Parser remains silent.
- Do not invent data/receipt requirements or undo/rollback a service mutation. Optimistic drag reordering and a fully successful Clean followed by a failed list reload are separate existing behaviors; this change makes the **action acceptance** truthful.

**Acceptance/red→green:** additive actual-VM tests run the four evidence bodies (`{}`, code absent with data/message, code0.4, code string"0") across all five methods; assert no row/flag mutation, no 排序成功, and no Clean refresh. Also cover null code, bool code, both Int32-overflow directions, both beyond-Int64 literals, non-JSON, JSON-null, null response, failed HTTP, thrown request, ordinary `code:-403` with message/msg and `code:0` without data. Sort never throws. Good code0 still performs each original success effect exactly once. Fake transport delivers fixtures to real parser/caller; actual HttpResults decoder body runs unchanged, including a production-compatible salvaged-object control.

## BND-B — bounded payload integers keep typed errors (P2)

**Defect:** four direct Int32 payload reads throw on overflow/BigInteger, despite Integer-token checks; legacy has_more reads similarly throw beyond Int64. These are not failures in the already-repaired business-code range check. VideoDetailVM catches at a wider level, so one malformed like/coin response can prevent later independent state reads.

**Minimal change and exact behavior:**

| Reader | Invalid integer result | Ordinary behavior to retain |
| --- | --- | --- |
| WebVideoStateParser like | Payload, stage `data` | Int32 range accepted unchanged; no new0/1-only restriction |
| WebVideoStateParser coin | Payload, `data.multiply` | Int32 range accepted unchanged; missing/wrong-type policy unchanged |
| WebRelationParser stat | Payload, `data.follower` | absent/null is successful0; Int32 extremes retained |
| WebRelationParser attribute | Payload, `data.attribute` | missing/wrong type still failure; Int32 values retained |
| LegacyDynamicFeedParser Parse / ParseReposts | Payload, `data.has_more` for an Integer outside Int64 | feed bool or in-range integer `!=0`; reposts in-range integer `==1`; missing/null/other-token false defaults retained |

Use a nonthrowing integer range check before conversion. The existing internal `ApiEnvelope.TryReadBusinessCode` is already a strict Int32 token/range reader and may be reused directly for the four Int32 payloads; its name alone does not justify a shared-source rename. No global helper/API rewrite or new product file is required. For legacy, keep public `ReadHasMore(JToken)` nonthrowing (out-of-range defaults false there); Parse/ParseReposts must distinguish the out-of-range integer and return the typed failure rather than silently publishing “end of feed”. A private legacy helper is enough. Do not stringify/round fractional numbers or numeric strings into newly accepted integers.

**Acceptance/red→green:** four literal fixtures from evidence × four Int32 readers return typed Payload at the exact stages without throwing. Preserve Int32.MinValue/MaxValue,0,1, absent follower0, and existing missing/null/type tests. Legacy feed/reposts reject the two beyond-Int64 literals without throwing; Int64.MinValue/MaxValue and bool/integer/default controls keep their current source-specific outcomes and exact large offset text. Exercise real VideoDetailVM with malformed like and valid coin/favorite responses: bad like leaves its previous value and independent coin/favorite still publish. Exercise owning real dynamic caller with failed append: known-good rows/cursor stay intact; the typed failure is recorded through the ordinary source failure path. PB1 business-code controls remain unchanged.

## BND-C — skip null wire rows without losing neighbors or paging meaning (P2)

**Defect:** a null season section escapes parser normalization; null season episode/area/season rows and null comment/top/subreply rows survive decode then crash mapping. Eight actual parser→mapper cases throw NullReferenceException.

**Minimal change:**

- Season parser filters null section entries before its section-normalization loop. Season mapper skips null main/section episode rows, optional area rows and optional season rows; keep this defensive at the actual dereference points. No request/VM/model schema change. Missing/null collections keep their established semantics (episodes/section empty; optional metadata remains absent).
- Comment mapper skips null entries in top_replies, replies and subreply loops before calling MapWebCommentReply; no need to alter the public single-row mapping API. **Do not remove entries from DTO arrays in the parser:** `WebCommentPage.ReplyCount` must remain original `page.Replies.Count`, including null slots, because it describes wire paging. Mapped Rows includes only usable rows. Existing pin priority, first-page rpid dedup, later-page duplicate behavior and source-bound NextOffset remain unchanged.
- Do not swallow invalid non-null elements (e.g. scalar where DTO object expected); they keep the existing typed Payload decode failure. Null row tolerance must not become a blanket catch returning empty success.

**Acceptance/red→green:** all eight evidence cases become nonthrowing parser→mapper success with empty mapped rows for null-only arrays. Mixed `[null, valid1, null, valid2]` cases retain valid values/order, section-first merge/duplicate winner and main/preview classification; optional metadata excludes only null entries. Comment first/later pages and subreplies retain usable rows and original raw ReplyCount; exact current cursor remains unchanged even when all mapped rows are filtered. Run actual SeasonDetailVM with valid playable neighbors plus each null season location: loaded, no generic exception panel, lists correct. Existing wrong-shape, empty/null-list, business rejection, refresh-failure and source/duplicate tests remain intact.

## BND-D — action publication belongs to its initiating detail (P2)

**Defect:** six real delayed actions for A mutate B after actual LoadVideoDetail(B) completes. A seventh case holds A's favorite-list refresh after its accepted action; B publishes its favorite list; A's late list replaces B's. Page InitializeVideo serializes detail loads, but does not own pending actions. This needs no server-toggle or native-rendering assumption.

**Minimal change:** in actual VideoDetailVM DoLike/DoDislike/DoTriple/DoCoin/DoFavorite/DoAttentionUP, capture initiating detail and request target **before the first await**, including login. If the detail retires during login, do not start a request for a different current detail. After action completion, only a still-current initiating detail may publish state/stat changes or initiate favorite-list refresh. Reference identity is sufficient because actual detail loading publishes a new model instance; no global generation/session framework is required. A same-ID reload is a new detail owner and retires the old continuation too. No remote cancellation/reversal is implied.

`LoadFavorite(avid)` must also bind its result to the initiating detail/avid and check before setting MyFavorite, so an already-started A refresh cannot replace B's list. Preserve request inputs and ordinary accepted action effects, count logic and messages; this slice does not redesign single-flight or server toggle policy.

**Compatibility trap:** public `AttentionUP(mid,mode)` is also used for staff rows and `new VideoDetailVM().AttentionUP(...)` in LiveDetailPage. Keep that standalone public signature/behavior valid without VideoInfo. Put detail-owner publication checks in DoAttentionUP, not an unconditional VideoInfo requirement in the shared standalone method. Existing standalone operation feedback may complete; it must not cause a replacement detail mutation. DoFavorite continues to send the supplied avid and exact favorite IDs; local publication also requires that supplied avid match the initiating detail’s aid. A request for a different supplied avid must not mutate the visible detail.

**Acceptance/red→green:** delay each of six real actions for A, run actual LoadVideoDetail(B), release success and failure separately, wait for actual async-void completion; B's req_user/stat/MyFavorite unchanged by A. A's response does not start a refresh against B. Repeat for same aid but replacement model. Delay favorite-list refresh A after accepted action, load B/listB, release listA; listB remains. Delay login then replace detail; no new request against B from A's initiating command. Current-owner ordinary success still performs original effects, valid failure retains prior state, and standalone AttentionUP with no VideoInfo remains usable. No timing sleep as an assertion; use deterministic task gates plus async-void completion tracking or equivalent.

## Exact implementation and test ownership proposal

These are nonoverlapping execution domains; root assigns named owners only after independent requirements PASS. Single owner per row; first two rows must not split their shared harness files between executors.

| Domain | Exclusive product files | Existing owning tests / harness seam |
| --- | --- | --- |
| BND-A favorite actions | `src/BiliLite.UWP/Modules/Detail/FavoriteDetailVM.cs` | `plan/tests/c2/{C2Probe.csproj,C2Stubs.cs,Program.cs,run.sh}` plus additive `.cs` under c2 if helpful. Keep current actual FavoriteAPI builder/signing assertions and MediaListService tests. Add actual full FavoriteDetailVM/IModules/RelayCommand/ReturnModel/ApiModel, existing FavoriteResourceListMapper and existing action parser/envelope links. Extend fake transport by request endpoint while preserving existing signed/unsigned queues. Extract exact HttpResults in run.sh as comments already does; no hand-written decoder. |
| BND-B video + BND-D | `src/BiliLite.UWP/Parsing/Video/{WebVideoStateParser.cs,WebRelationParser.cs}`; `src/BiliLite.UWP/Modules/Detail/VideoDetailVM.cs` | `plan/tests/c1/{C1Probe.csproj,C1Stubs.cs,Program.cs,run.sh}` plus additive checks/stubs under c1. Compile full actual VideoDetailVM rather than only its extracted model region; remove duplicate generated-model compile path, retain every existing assertion. Link actual IModules/RelayCommand/ReturnModel/ApiModel. Request delivery/platform primitive stubs only; extract real HttpResults. All existing Video parsers/DTOs already link here. |
| BND-B legacy | `src/BiliLite.UWP/Parsing/Dynamic/LegacyDynamicFeedParser.cs` | Add to existing `plan/tests/g4/Program.cs` (direct parser plus actual UserDynamicVM fallback/paging seam already present). Existing g3/g4/g5 probes link parser; no project/stage addition. |
| BND-C null rows | `src/BiliLite.UWP/Parsing/Season/{WebSeasonEpListParser.cs,WebSeasonEpListMapper.cs}`; `src/BiliLite.UWP/Parsing/Comment/WebCommentPageMapper.cs` | Additive `plan/tests/season1/Program.cs` and `plan/tests/comments/Program.cs` checks. Existing projects already compile real season VM/parser/mapper and comment parser/mapper; existing decoder/model seams suffice. No dialog source change. |

No new product source or executable stage is needed, so UWP explicit Compile entries, aggregate inventory and CI denominator stay **50 logical / 51 executable**. If an executor finds a genuine extra physical-stage necessity, stop that change and request a file-based ownership/inventory amendment; do not mask the denominator. All plan/ROADMAP/VALIDATION/REVIEW_LOG reconciliation belongs to the later single documentation owner, not these source executors.

## Validation, risks and completion

Each executor first adds failing behavioral checks to the existing owning batch, records genuine red on the current implementation, then fixes product source and records green with original assertion bodies preserved. All malformed inputs are local fixtures; no account/write endpoint is contacted. Use exact SDK8.0.427/runtime/ref8.0.33 at `/workspace/work/maintenance-foundation/quality`; no tool upgrade. Run affected c1/c2/comments/season1/g4 batches and required shared g3/g5 regressions according to touched dependencies; the final stable integrated50/51 gate is the later common owner’s responsibility.

Review the final diff for no endpoint/form/signing/credential change, no held account or root-doc mutation, no stale publication and no broad empty-success catch. Linux actual-source proof establishes local parsing/publication behavior only. Windows UWP compilation and native page interaction remain external gates; next safe native checks are favorite delete/clean/collect/sort rejection recovery, switching playlist/collection video while an action completes, and season/comment pages with partial optional data. Do not reopen historical device acceptance or claim new service/native PASS from console fixtures.
