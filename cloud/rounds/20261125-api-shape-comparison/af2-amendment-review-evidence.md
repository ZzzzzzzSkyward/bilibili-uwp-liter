# AF2 staging amendment — independent re-review evidence

2026-11-25, independent `api_requirements_review`. Supports [af2-amendment-review.md](af2-amendment-review.md). Scope is the sole P1 failure-retention contradiction and root's explicit limited decode-before-publication extension. No source/test/project/main-doc/index/ref/cache mutation or full suite. Actual clock at review was03:55:41UTC, before the recorded07:41:29 checkpoint/08:51:29 hard deadline; no timer/automation change occurred.

## Reviewed requirements and actual-method feasibility

Read ACTIVE re-review/revision dispatches, corrected AF2 requirement/evidence/amendment, original CHANGES/evidence and the deadline file. The original complete MyFollowVideoVM remains hash98a1ef21…; its actual reads/publication sequence was established in the first review and remains byte-identical.

The corrected requirements now authorize precisely the missing phase separation:

| Operation | Decode before commit | Observable success commit retained |
| --- | --- | --- |
| LoadFavorite created branch | Existing selected list decode, existing null-created-list fallback, default item decode and explicit JToken bool cast | MyFavorite reference notification, default Insert0 collection event, staged HasMore and Page advance |
| LoadFavorite collected branch | Existing selected collected list decode, before either branch publishes | CollectFavorite assignment after the created commit when selected; its unconditional setter stays unchanged |
| LoadCreateList | Entire existing list decode/non-null iteration prerequisite plus existing bool conversion before Add | Same collection, all decoded rows in original order, staged HasMore, Page+1 |

No product helper/cast/envelope or route changes are required to achieve this order. Complete Task.Run DeserializeJson and actual HttpResults remain the decoder boundary. Created/collected optional branch tests keep exact CLR-null semantics; a present JSON-null token does not newly skip a branch. No extra required branch or nonnull default policy is added. Existing accepted created-list-null fallback, null default item, null collected result, valid duplicates/null rows and bool-cast accepted variants remain positive compatibility controls. Null append list must still fail before commit; it cannot silently become empty success. Valid empty append still follows the existing append success/Page rule, distinct from AS3 favorite-resource paging.

The failure state table is now consistent with the expanded scope:

- Direct LoadFavorite preserves accepted M/C/reference/row identity on any selected precommit response failure while retaining pre-request Page1/HasMore=false and finally Loading=false plus existing notice.
- Refresh first applies its existing explicit null resets. Decode failure then publishes no response reference, so model/binding stay null. Initial failure starts/stays null. No rollback to pre-Refresh rows is promised.
- Loading=true Refresh stays a no-op. Other current command/single-flight rules are not strengthened.
- Append preserves M/C/rows and requested Page on metadata failure, clears HasMore as today and leaves Loading's incoming value untouched. Successful retry requests that same Page and only its accepted rows are added.
- Skipped branches retain the state after the invocation's existing reset phase, not a restored pre-Refresh collection. Created skipped means Page1/HasMore=false; selected collected-only publication stays separate.
- Exceptions/reentrancy from arbitrary subscriber code during commit remain with the existing exception owner; no general transaction, rollback, cancellation or dedup is claimed.

This intentionally repairs the seven unsafe observations rather than treating their historical PASS label as desired behavior. It does not silently relax earlier binding or success controls. Property reference notification, pure getter, bounded same-reference echo and removal of only the two obsolete flags remain the accepted AF1-style contract. CollectFavorite setter and API/action/sort methods stay byte-identical.

## Binding and preserved history

Private reviewer directory: `/workspace/work/api-shape-comparison/requirements-review/af2-amendment-review/`.

Verified author revision receipt SHA `023d4946c3548210b3f5b26130346e93ac69f6153e662edcc4f0d5986de3a39a`; all33 new compiled/runnable records, five tool/dependency records, two unsafe history records and the safe-result hash match actual files. Original AF2 receipt's33 runnable/five tool inputs and original-red.log remain exact. All32 original complete source/read-evidence inputs remain exact. Current main pre-AF2 VM still equals the original; no repair or AS1–AS3/AF1 regression was used to obtain red.

Key immutable bindings:

| Input/result | SHA-256 |
| --- | --- |
| Current/frozen pre-AF2 complete MyFollowVideoVM | `98a1ef21f38816cb020eb7512fdd4c05fbfb0abd168c7f9e729edf23f59bcabe` |
| Original AF2Probe13 assertions | `dbe4ccfc7069a3bb0e70aef8ce6f31380abffe814913549848793990bece5600` |
| Original AF2 binding log (11PASS/2FAIL) | `70e2f715173bb72ffb93480095833a81aa9b15d8899bf1930f37d120c4b5a7ad` |
| New AF2SafeBoundaryProbe37 assertions | `1152ff012aee889035ca755028362d06a53f99dcf2627557e29eddb7d8c9cda7` |
| Author safe original-red.log | `2842319599d5931e29b0db769b4fe9a743e4e0bc772bb87cf150ffc693d412b8` |
| Actual full DeserializeJson wrapper | `6e731a090e6537fb03635bc5360ad50ba35936a870c12698fc4ff696fb0136da` |
| Complete actual HttpResults extraction | `6df7b868c05bfa188a13c15601c17215f5c81437ccae7214aaa7e0d70a22f4d2` |

Author `revision-history/FailureBoundaryProbe.cs` and failure-boundary.log independently equal this reviewer's own released original files byte-for-byte. Those seven unsafe original observations and both initial AF2 CHANGES files were not edited or rerun. They remain history explaining the expanded contract, not repaired-green requirements. Original13 binding proof is reused by exact binding, not rerun for a new label.

Copied the exact new runnable snapshot graph into exclusive reviewer scratch, with original complete source/read evidence and exact helper/decoder/stubs. Snapshot project changes only private StartupObject for the new safe fixture; no algorithm moves into stubs. Existing AF1-fixed FavoriteDetailVM/fixtures and AS3 paging fixtures remain untouched. `input-check.json` records all receipts and historical review hashes.

## Independent new-boundary replay

Inspected AF2SafeBoundaryProbe before running it. It observes real inherited property signals, retains its own binding reference and subscribes to collection events. Page reflection reads only; no private fields are reset. Actual Task-returning load/append calls are awaited. Actual RefreshCommand uses an operation-counting SynchronizationContext, pending continuation counts, captured escaped faults, Done task and5-second bound. No sleep-based quiescence, fake command, parser or notification algorithm substitutes for production work.

With existing SDK8.0.427/runtime8.0.33/Newtonsoft13.0.3 and a private CLI home:

```bash
export DOTNET_ROOT=/workspace/work/maintenance-foundation/quality/dotnet
export DOTNET_CLI_HOME=/workspace/work/api-shape-comparison/requirements-review/af2-amendment-review/cli-home
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$DOTNET_ROOT/dotnet" run --project /workspace/work/api-shape-comparison/requirements-review/af2-amendment-review/snapshot/plan/tests/c2/C2Probe.csproj
```

**Exit1, SAFE BOUNDARY passed=17 failed=20 (37 total)**. Private `independent-safe-red.log` contains the compiled result. No infrastructure failure or repaired-source PASS is claimed.

Observed breakdown:

| Error family / journey | Original actual failure and active controls |
| --- | --- |
| Missing default, direct | Safe no-partial-created assertion fails; bound old reference/reset Page1/notice and valid retry model/state controls pass; retry binding assertion fails. |
| Missing default, Refresh | Async-void settlement/no-escaped-fault control passes; safe no-partial/null-bound assertions fail; reset/error completion and valid retry model/state pass; retry binding fails. |
| Invalid later flag, direct/Refresh | Same corresponding source-state and stale-binding pattern as missing default. |
| Invalid collected decode, direct/Refresh | Created rows/default and success flag/Page already publish before failure; safe no-partial and Page1/HasMore=false controls fail, with the binding failures; valid retry model/state and settled Refresh controls pass. |
| Invalid append flag then same-Page retry | Safe no-Add/event and single accepted copy/event assertions fail; old Page2/HasMore/error completion control passes. |

Several20 failures are the already-supported reference-publication defect; others are the newly explicit response-stage safety boundary. They are not all attributed to one mechanism. The final combined AF2 fixture must close both while preserving all17 active positives and original13 binding assertions/11 positives.

## Final implementation acceptance retained

Same frozen final fixture/stub/tool/dependency bytes must execute complete pre-AF2 original and repaired VM, with genuine old behavior red then all green. Preserve original13 and new37 assertion bodies/outcomes (entrypoint/namespace adaptation only) and every original c2/action/AS3/AF1 assertion. Add the full reference/null/equal-content/getter/echo table, pending/Loading guards, early and late failures, exact M/C/row/Page/HasMore/Loading/notices and retry state, accepted optional/null/cast variants, default/created/collected order, reorder/sorting request order and exact helper extraction negative/fidelity controls.

The precise product scope is MyFollowVideoVM property/two flags plus response decode/publication ordering inside LoadFavorite/LoadCreateList. Proposed C2Probe.csproj, partial Utils declaration, additive FavoriteFolderPublicationProbe/dispatch and run.sh generated-helper extraction remain justified. No extra product file, shared notifier, parser/DTO/auth/API/action/sort policy change or test-stage denominator is allowed. A bounded failed response adding no row is not a new general dedup algorithm.

Owning c2+c3, implementation/fresh review and one later combined stable51/52 gate remain required. Native FavoritePage/render/reorder/dialogs/UWP, live schema/service/account/write/cookie/session/U2 and Windows memory remain unverified. Root deadline/checkpoint rules and exact future source/publication grants are unaffected by this requirements verdict.

## Release receipt

All author original/revision source/fixture/helper/decoder/tool/result/history bytes and reviewer historical reviews are unchanged at completion. Sole repository writes are af2-amendment-review.md and af2-amendment-review-evidence.md; private writes remain exclusive. No product/test/project/stub/runner/main-doc/index/ref/cache mutation, full suite, old proof-label rerun, foreign execution or service request occurred.

All amendment artifact/source/snapshot/fixture read custody is **RELEASED**; reviewer scratch is frozen. Root must issue the exact same-owner grant after binding the still-current original; PASS itself is not executable or publication authority.
