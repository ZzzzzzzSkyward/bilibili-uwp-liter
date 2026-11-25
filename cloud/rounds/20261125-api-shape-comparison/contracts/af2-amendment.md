# AF2 amendment — validate current response before publication

Status: **READY FOR SAME REVIEWER RE-REVIEW**, 2026-11-25 UTC. The sole P1 in [af2-review.md](../af2-review.md) showed that the original failure-retention promise was false under unchanged legacy method ordering. [Root's ACTIVE correction](../af2-revision-dispatch.md) now authorizes a bounded proposal: retain the MyFavorite property repair and complete all currently required response decoding before publishing references/default inserts/appended rows. No implementation has been made. The authoritative amended [requirements](af2-requirements.md) contain exact decode/commit phases and the invocation/state outcome table.

## Demonstrated gap and bounded decision

The original full VM assigns created folders before extracting default detail, converting has_more and decoding collected folders. Its append method Adds rows before its has_more conversion. Later response errors therefore leave partial rows, and the same-Page retry duplicates an unaccepted row. A property-only repair would make those partial references visible; it cannot deliver the previously promised retention.

Root chooses current-response validation-before-publication, not rollback or a broader transaction. Stage only the exact existing branch-selected reads, helper decodes, null-created-list fallback, default item and explicit JToken bool conversion into locals. Then commit in the existing observable success order: created reference event, default Insert event, HasMore/Page success, collected reference assignment. For append, finish whole-list decoding and flag conversion before the first Add; preserve row order and current duplicates/null-row acceptance on valid responses. Validate collected data before any created commit, including when its failing decode occurred later in the old sequence.

No endpoint/DTO/strict-envelope/auth/tolerance migration is authorized. Current optional branch tests retain their CLR-null semantics; accepted null default items, null collected lists, created-list-null fallback and actual bool-cast values remain. Null append lists still fail. The CollectFavorite setter, DelFavorite, SortMyFavorite, FavoriteAPI and other product methods stay unchanged. Pre-request Loading/HasMore/Page and Refresh's prior null resets stay. No transaction/rollback guarantee covers arbitrary throwing/reentrant PropertyChanged or CollectionChanged subscribers during commit.

## Precise intentional behavior changes

M/C are the accepted created/collected references; response reads fail before the new commit point.

| Journey | Historical unsafe outcome | Amended desired-safe outcome |
| --- | --- | --- |
| Direct LoadFavorite: valid created list, missing default detail | Replaces M with created-only partial list, then error | Keep identical M/C/rows and bound M; Page1/HasMore=false/Loading=false; existing notice |
| Direct load: valid created/default list, invalid later has_more | Replaces M and inserts default before error | Same retained M/C/rows and reset state; no response property/collection event |
| Direct load: valid created/default/flag, invalid collected-list decode | Replaces M, inserts default, publishes success flag/Page before error | Same retained M/C/rows; Page1/HasMore=false/Loading=false; existing notice |
| Idle RefreshCommand with any of these late decode errors | Prior null resets followed by partial nonnull created replacement | Prior null reset remains observable; model/binding remain null, Page1/HasMore=false/Loading=false; existing notice |
| LoadCreateList: valid new row, invalid later flag | Adds new row, leaves Page2; same-page retry adds it again | Retain identical M/C and rows, no failed Add event; Page2/HasMore=false/Loading unchanged; retry requests pn2 and adds only its accepted row, Page3 |
| Valid existing responses | Current reference/default Insert/collected/append order and state | Same successful order/accepted shapes, plus corrected identity notifications |

The full table also distinguishes fresh initial failure, Loading-guard no-op, skipped branches, direct load versus Refresh reset and append's nonownership of Loading. Existing earlier accepted duplicate rows are not deduplicated. This is one rejected response leaving no row to duplicate, rather than a new dedup policy.

## Preserved raw history and new safe actual-source proof

The reviewer's seven unsafe-current-behavior assertions and raw log were copied byte-for-byte into private af2/revision-history/FailureBoundaryProbe.cs and failure-boundary.log. They remain discovery history showing the gap. They are **not repaired-green invariants** and are not rewritten or relabelled safe assertions. Original AF2Probe13 assertions, its11 positives/2 binding failures, original source snapshots, tool/package bindings and original-red.log remain byte-identical.

New private af2/revision-safe/ copies the same complete pre-AF2 source graph, full actual HttpResults and exact Task.Run DeserializeJson method, existing delivery/platform/signing primitives and original13-assertion fixture; it adds only AF2SafeBoundaryProbe and a private StartupObject. It runs actual awaited LoadFavorite/LoadCreateList, and actual RefreshCommand with operation/continuation counts plus captured faults and a5-second bound. No decoder/publication policy is implemented in an observer or stub. Read-only Page reflection asserts state without writing it.

Command: bash /workspace/work/api-shape-comparison/contracts/af2/revision-safe/run.sh

Outcome: **exit1,17 positives PASS and20 desired-safe failures/37 checks**. Three late-error families (missing default, invalid flag, collected decode) run for direct load and actual Refresh; safe retention/null-boundary/state expectations fail on the original, and valid retry state/metadata positives pass while stale binding assertions fail. Separate append invalid-flag/retry checks fail desired-safe no-Add/no-duplicate/event expectations while original Page/reset/error controls pass. This is compiled actual behavior red; no infrastructure failure or repaired-green result is claimed.

| Frozen input/result | SHA-256 |
| --- | --- |
| Complete original MyFollowVideoVM | 98a1ef21f38816cb020eb7512fdd4c05fbfb0abd168c7f9e729edf23f59bcabe |
| Original AF2Probe13 assertions, unchanged | dbe4ccfc7069a3bb0e70aef8ce6f31380abffe814913549848793990bece5600 |
| New AF2SafeBoundaryProbe37 checks | 1152ff012aee889035ca755028362d06a53f99dcf2627557e29eddb7d8c9cda7 |
| Safe original-red.log | 2842319599d5931e29b0db769b4fe9a743e4e0bc772bb87cf150ffc693d412b8 |
| revision-receipt.json (all compiled/runnable/tool/history inputs) | 023d4946c3548210b3f5b26130346e93ac69f6153e662edcc4f0d5986de3a39a |

Original receipt and every compiled/runnable/tool/dependency input were reverified unchanged before revision binding. Raw seven-control reviewer files equal their released originals. No prior log/snapshot/fixture or main-tree file was edited by this author.

## Final acceptance, paths and custody

The same final c2 fixture must preserve the original13 assertions, all11 active positives and the new37 safe boundary assertions unchanged (namespace/entrypoint adaptation only), then add the accepted reference/echo/null/optional/cast/Loading/failure/retry controls and retain every original/AS1–AS3/AF1 assertion. Run it on this complete pre-AF2 VM for genuine red, then the exact permitted property plus decode-before-publication repair for all green. Valid null/default/optional/cast controls prevent a hidden tolerance change. Helper extraction must still fail on missing/duplicate/incomplete boundaries and prove full emitted body equality. No handwritten parser belongs in a stub.

Only the existing same favorite executor may later receive MyFollowVideoVM's property/two fields and publication ordering inside LoadFavorite/LoadCreateList, plus the already named additive c2 fixture/project/partial-stub/extractor paths. Root grants those exact paths only after same-reviewer PASS and source-reader release. No other source/main-doc/index/ref/publication window is opened here.

Amended AF2 requirement/evidence/amendment and all own revision scratch input/result read custody are **RELEASED** for the same independent reviewer. Original and revision proofs are frozen. AS1–AS3/AF1, source caches and other owners' work remain untouched. Deadline/checkpoint instructions were read: actual clock at revision start was2026-11-25 03:49:30UTC, before07:41:29 checkpoint and08:51:29 hard deadline. No timer or automation was created or changed; root retains time-window orchestration. Native/service/account/session/liveU2 acceptance remains unverified.
