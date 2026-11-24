# Boundary requirements review

**PASS — requirements/design only.** Reviewer: boundary_review, independent of the requirements author. Reviewed baseline `077ef35ae194d3ae85aa0621daeac07dc29e29ab` on dev. No unresolved actionable P0/P1/P2 requirement finding. This is not an implementation, integrated-suite, Windows, device or service PASS; root still owns execution dispatch.

## Inputs and independent evidence

Read AGENTS/CONTRACT, memory INDEX and relevant decisions/current-run record, this round's plan/intake/coordination/quality assessment, review dispatch, inventory decision, and the complete released boundary requirements/evidence. Inspected actual touched parsers/mappers/VMs, envelope/decoder/error helpers, request builders and page callers, plus the c1/c2/g4/comments/season1 source closures, scripts and existing assertions. Read the other streams' ownership proposals to check shared-file conflicts.

Reviewed artifact SHA-256:

```text
3c29d998caaf7ae86a4a431294bf253ea9fe9ea4ccb2fa437d68ad3203abfef9  boundaries/requirements.md
f2ba52f0fb22bfa90c3ba60fafe00c413ebd239e76e9bf771c026768f7e1587e  boundaries/evidence.md
ee667a2ebc9f25c32dea21a65ab4578a973b36b0fa78680b833313ef75fc0c8d  ../inventory-decision.md
```

Independently compared every entry in the scratch `source-hashes.json` with current product bytes: **47 entries, zero mismatches**. Its SHA-256 is `7cb6ed81f4a6492c6d83e58f1061cc93a0b7f7b6c9cf3bbfd4abe7874e97ef11`. Reconstructed the expected HttpResults extraction from current HttpHelper and compared it with `HttpResults.actual.cs`: exact full-body match. Scratch Program SHA-256 remains the released `42e7dea1b2e1cd70276bbed0ddb7b4b54c8ebeb432114b2c518ab807ea45d064`.

Re-executed, without changing the reproducer or any assertion:

```bash
DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet \
DOTNET_CLI_HOME=/workspace/work/recovery-integrity/boundaries/dotnet-cli \
DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run \
  --project /workspace/work/recovery-integrity/boundaries/BoundaryProbe.csproj -c Debug
```

Observed exit **1**, `CONFIRMED_VIOLATIONS=57 CONTROLS_PASSED=46`, every named control true. These are genuine local defects in actual sources: 20 favorite false-success effects; two uncaught Sort exceptions; 16 Int32 narrowing exceptions; four legacy BigInteger exceptions; eight null-row exceptions; six stale action publications; one stale favorite-list publication. The scratch response delivery is fake; the decoder, parser/mapper and VM decision/publication paths are real. No live mutation or signing/session conclusion follows from this experiment. A separate `git diff --exit-code -- src plan/tests .github docs README.md AGENTS.md` exited0.

## Contract decisions

| Area | Review conclusion and required implementation acceptance |
| --- | --- |
| BND-A: action acknowledgement | PASS. Real FavoriteDetailVM uses default/coerced ApiDataModel.code and Sort dereferences a null decode result. Reusing existing WebVideoActionParser is the smallest compatible correction: strict bounded integer business code, valid code0 with absent/null data accepted, rejection/transport preserved. All five publication effects require typed success; malformed replies must not remove rows/toggle flags/refresh/toast success. Whole Sort request/parse path needs the stated exception boundary. Use local action failure text rather than VideoFailureText's detail-read wording. Preserve production salvage and real FavoriteAPI construction/signing checks. |
| BND-B: payload integers | PASS. Integer token type does not establish Int32/Int64 range. Existing ApiEnvelope.TryReadBusinessCode can safely serve the four Int32 payload conversions without a new helper file or global rename. Preserve each payload stage/default and all ordinary integer values. Legacy Parse/ParseReposts must turn out-of-Int64 integers into Payload at data.has_more, while public ReadHasMore stays nonthrowing. Feed bool/nonzero and repost integer-equals1 are distinct compatible rules. A failed like parse must leave later valid coin/favorite reads usable; a failed legacy append must keep known-good rows and its own cursor and record the ordinary source failure. |
| BND-C: null rows | PASS. Null section normalization and the listed mapper dereferences reproduce the exceptions. Filtering null sections before normalization and skipping null row dereferences are bounded repairs. Keep non-null malformed DTO elements as typed Payload failures. Comment parser arrays retain their raw slots: WebCommentPage.ReplyCount remains page.Replies.Count, while mapped Rows filters nulls. Preserve source-owned NextOffset, pin priority, first-page dedup and later-page duplicates. Season neighbor order, section-first winner, main/preview classification and absent optional metadata remain unchanged. |
| BND-D: detail ownership | PASS. Actual page serializes loads but does not retire pending actions; actual LoadVideoDetail publishes a new model, so reference identity is sufficient for this bounded publication contract, including same-aid replacement. Capture owner and request inputs before login/first await; never re-read B's aid/mid to execute A's command. Recheck current owner before state/stat writes or initiating a favorite refresh. LoadFavorite independently binds owner plus avid before its await and checks before assigning MyFavorite; checking only the action continuation is insufficient. Preserve ordinary current-owner effects and supplied favorite IDs/avid, with no visible-detail mutation for a different supplied avid. Delay success and failure, login, same-aid replacement and an already-started list refresh using task gates and observed async-void completion. |
| Standalone attention compatibility | PASS. VideoDetailPage staff button calls public AttentionUP(mid,mode) against its captured staff row; LiveDetailPage calls it on a new VM with no VideoInfo. Keep that public operation usable. Detail-owned request/publication handling belongs in DoAttentionUP or its private detail-aware path; an unconditional VideoInfo requirement inside standalone AttentionUP would violate the reviewed contract. |

The ruled-out hypotheses are supported and remain exclusions: exact large aid/mid/cid/cursor strings survive; DTO overflow is already caught in the inspected paths; missing follower0 and ordinary scalar/default behavior are intentional; code0 actions require no data receipt; null collection compatibility already works; concurrent detail loads alone are not the reproduced fault. Do not reopen held account parsing, tighten HttpResults globally, change protocols or infer server toggle behavior from these fixtures.

## Source seams, preservation and ownership

The proposed scope is nonoverlapping with recovery/storage. Root may allocate fewer executors, but each shared harness row must have exactly one writer. Approved boundary files are:

- FavoriteDetailVM and c2's project/stubs/Program/run script, with additive c2 checks if useful. Keep the real FavoriteAPI linked, its existing builder/signing assertions and signed/unsigned MediaListService queues. Add the full real favorite VM, mapper and action parser/envelopes; replace the handwritten response decoder with exact current HttpResults extraction. `ApiModel` response-wrapper linkage here means `Models/ApiModel.cs`; the fake delivery request descriptor does not authorize a product API edit.
- WebVideoStateParser, WebRelationParser, VideoDetailVM and c1's project/stubs/Program/run script, with additive c1 checks if useful. Compile the full actual VM and dependencies. Exclude the old generated DetailModels compile path, including a pre-existing build/DetailModels.cs, so repeated/dirty-artifact runs cannot compile duplicate models. Preserve every original c1 assertion body and source-wiring check; add action tests after/reset separately from the existing silent-parser checks. Use exact HttpResults extraction and primitive request/login/log/toast seams, not a copied VM or action algorithm.
- LegacyDynamicFeedParser and additive g4 Program checks. The **existing executable caller seam is `BiliLite.Modules.DynamicVM` from `Modules/Home/DynamicVM.cs`**, linked by G4Probe.csproj; the proposal's “UserDynamicVM” label is read in light of its controlling requirement to exercise an owning real dynamic caller. User-space DynamicVM is currently source-wiring coverage, not executed by g4. Use the existing Home VM fallback/append path for the new behavioral failure assertions; this grants no new user-VM source, project, stub or extraction scope. Preserve the original user-space source checks and gRPC/polymer/legacy assertions.
- WebSeasonEpListParser/WebSeasonEpListMapper/WebCommentPageMapper and additive season1/comments Program checks. The existing projects already link the actual season VM/parser/mapper and comment parser/mapper. No dialog edit or copied mapper is needed. Preserve all existing response-shape, rejection, source, duplicate, cursor and refresh-failure checks.

All new checks must first fail for the actual defect on unchanged baseline, then pass against the repair with assertion bodies intact. Freeze executed-input hashes for both phases. Primitive platform/API delivery stubs may capture request targets and gate completion; they may not decide parser acceptance, action ownership, paging or mapping on behalf of product source. Run c1/c2/comments/season1/g4 plus shared g3/g5 regressions as required by the touched dependencies. No new boundary executable/program is approved and no hidden second invocation is allowed.

## Global inventory and evidence limits

`inventory-decision.md` controls over the boundary proposal's old global 50/51 wording: boundaries adds **zero** physical stages; recovery adds exactly batch:recovery. Final current inventory is **44 directory batches + seven legacy logical/eight legacy executable = 51 logical/52 executable**. All original stages/assertions remain required. Full completion requires 51/51 logical, 52/52 executable, every required row PASS and complete_required=true; selected runs remain PARTIAL_PASS. Historical 50/51 results remain unchanged provenance. Recovery executor alone owns run-all.py, verify-suite-runner.py and any new-parser UWP Compile entry; a later single documentation owner reconciles reusable current counts. Boundary executors do not edit the UWP project, aggregate runners, workflow, main documents, identity, protected data, indexes or history.

No platform prerequisite blocks these local repairs. Linux proves the reviewed parsing/publication behavior, not UWP/XAML compilation, native page rendering, actual cookies/signing or server mutation. Retain Windows compile and touched-flow checks for favorite rejection/sorting, video replacement during actions, and partial season/comment data as explicit external evidence gates. No fresh Windows/device/live PASS is claimed.

Reviewer mutated only this review.md; product/test/build/main-document/index/history files remain untouched by this review.
