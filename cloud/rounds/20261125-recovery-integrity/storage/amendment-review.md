# Independent storage amendment review — FA-01

**PASS — requirements/design only.** The amendment closes the FA-01 contract gap with a minimal compatible guarded-read rule. No unresolved actionable P0/P1/P2 amendment finding. FA-01 remains an implementation finding until the original storage executor supplies actual repaired source and identical final-fixture red→green evidence under root's explicit grant. This review does not modify or supersede the original final-audit verdict.

Reviewer: fresh_audit, independent of the amendment author and source executor. Read amendment-review-dispatch, review-iteration-plan, the complete storage amendment/evidence, original storage requirements/review/final review, actual whole service, actual DownloadPage refresh/callers, LM02 and primitive storage/FAL stubs. Reused the fresh audit's actual-source reproduction and inspected the amendment's complete IdentityAudit/PageRefresh/project and final acceptance-red output. No redundant test or full-suite run was needed for an unresolved design question.

## Accepted contract and ownership

- **Validate the entire entry snapshot inside ReadRegistryUnderGateAsync's existing try/catch**, after its intentional null defaults and before returning to any caller. Reject a null entry or null/empty/whitespace Token, CollectionId or RootPath as typed `registry-read`, using existing `unknown` failure identity. A trailing bad row must fail reopen even when an earlier valid token matches. Merely guarding ToCollection or the selected reopen row would not meet this requirement.
- The three fields are precisely the current grant/reopen and replacement identities. No GUID requirement, trimming/canonicalization, duplicate policy, existence/FAL check, display-metadata requirement or schema migration is introduced. Missing file/null root/missing-null-empty Entries remain empty-library defaults. Complete identities with stale grants or broken assets remain saved rows with the existing reopen stages and repair/reimport behavior.
- Whole-read rejection must occur before import's provisional grant or staging, preserve exact committed/temp bytes and every prior grant, and release the existing gate through each caller's finally. Subsequent list/reopen/import uses the same service and static gate successfully after valid bytes are restored. Do not silently filter/rewrite corrupt entries or add a second gate/retry framework.
- The actual page refresh already awaits the service before clearing visible rows and catches LocalMediaLibraryException. Typed rejection therefore reaches its existing log/toast while retaining prior visible rows. No page source change or broad catch is needed; exact-body scratch composition is sufficient local evidence.
- Exact future product ownership is only LocalMediaLibraryService.cs, with additive LM02 acceptance and a necessary inert shared primitive observation hook if existing facilities are insufficient. History/playersess/page/XAML/project/runner/inventory/main-doc/index/ref scopes remain excluded. The original storage executor implements only after root's dispatch; this PASS itself grants no source mutation.

## Required implementation evidence

The amendment's acceptance is precise and executable without native prerequisites:

1. Null/empty entries and each Token/CollectionId/RootPath missing/null/empty/whitespace variant appear both before and after a valid entry. List, valid-token reopen and valid import all reject the whole snapshot as typed registry-read with no partial rows. In particular, valid-first/trailing-malformed reopen must fail rather than short-circuit successfully.
2. Preserve exact bytes, prior grant identities/count and temporary state after each rejection; observe no provisional access activity, staging, replacement or retirement. Final grant count alone cannot prove an added-then-removed provisional grant never existed. Existing hooks, including primitive removal observation, or one necessary inert addition can establish this without copying service decisions.
3. Restore valid bytes after each operation kind, keep the same static service/gate, then bound and await successful list/reopen/distinct import and prove both reachable rows/grants survive. Release any event gates in finally and drain all initiated work before fixture reset.
4. Keep intentional empty defaults, non-GUID usable tokens, absent Title/ItemCount, unknown/stale token stages and complete-identity broken-asset repair controls, plus every existing S1–S4/original assertion. The source binding and complete original assertion expressions must remain intact.
5. Execute the exact actual page refresh body against repaired bytes with seeded visible rows: Task completes through one typed failure log/toast, visible rows and bytes/grants remain. This is source-call composition, not rendered UI/native verification or another permanent physical stage.

Run the **final added LM02 test bytes against unchanged current service first**, require nonzero genuine red, then the same assertion bytes against the fix. The amendment's scratch42-red record supports the requirement but does not replace that owning-test receipt. Run LM02; run unchanged LM03 if shared stubs change, and retain LM05 in the required final aggregate. Root's common owner reruns the full51 logical/52 executable gate once storage/recovery stabilize. No new physical stage or unnecessary unchanged controls run is authorized; separate14-control and package15 contracts remain distinct.

## Independent bindings and evidence assessment

| Input/artifact | SHA256 |
| --- | --- |
| storage/amendment.md | ade35bb9186658ed6fff70ce40240563632cc540357c9c653d18a893c5c43925 |
| storage/amendment-evidence.md | 513c6538f641d0c16ce7e94c67d181f9b79e07f29d4ae931aa41e6bb102cc386 |
| Actual service | 335d35363f65fa03e57fc94193ce9e2c08aafd58cea83a40e24a0833e19a2dd0 |
| Actual DownloadPage source | c80f7267f7db8000c6f97f3dab1bfb37f631291c463a791a9f78db9e2423e56f |
| Existing LM02 | d558e088fc9f20d28712af97c0a6725ebc1e2bf948cba6fac9c3671d93ca6b6b |
| Existing primitive storage/FAL stubs | a1c38806369b348a089032cc559b165cd941b4fb46a8c242b58024456e2c9caf |
| IdentityAudit.cs | 3ee149dc7f431d6cade5fe6b651bf59dde1b430d5ce8738b4f80d37624d0e93e |
| PageRefresh.cs | 0b5239660ba5b4349b8eab2209878b3790c5621ddba20a221045f3f02964d7d8 |
| inputs.json | 2a77cf5debc2bab36206b07d9643efec9a4b68950cdc3a4694f23b8bc304795d |
| acceptance-red.log | d5c5fb0d05ef023b45ea25296437c3f80a2942256db6266a7c69500714a93513 |

Independently rehashed all10 declared repository inputs: zero mismatches. Extracted the actual refresh signature/body with the amendment's declared indentation boundary: SHA25649cbc84da98359c46d233d396e71bf8a079180642bf5540e1a324bd81c5d34f2, and verified the complete text occurs unchanged in the wrapper. The fixture compiles the whole real registry service/models/loader/validator with Newtonsoft13.0.3 and existing primitive stubs; no acceptance/identity algorithm is substituted.

Read final acceptance-red:14 malformed fixtures,42 unmet predicates, eight compatibility controls pass. A valid-first null trailing entry lets reopen succeed on old source; partial identities allow list/reopen and successful import, corroborating the need for whole-snapshot guarded validation. The exact page body faults with zero log/toast before repair. Fixture calls are awaited sequentially, no event/observer is left pending, and hooks/grants/temp tree are cleaned in finally. These are real source failures, not an implementation PASS.

The amendment retains coherent file/responsibility boundaries and uses existing UWP-compatible string/collection/exception primitives. No disk/schema/global framework or new API/stage is needed. Windows UWP compilation/rendering, native FAL/staged replacement/restart semantics and hosted CI remain separate unverified gates; they do not block this bounded source repair. Only this amendment-review Markdown was written this turn.
