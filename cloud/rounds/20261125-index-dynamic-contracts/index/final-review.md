# FI1 independent owning implementation review — PASS

**PASS for the frozen FI1 implementation. No unresolved in-scope P0/P1/P2 finding.** Reviewer `index_contract_review`, 2026-11-25 UTC. This is selected **PARTIAL_PASS**, not combined/full/native/hosted acceptance. Baseline A is `7fc3c524fde4139d01e7ecd362647ca771c64ea8`. [Binding](review-evidence/binding.json), [commands/exits](review-evidence/commands.json), [counts](review-evidence/summary.json) and [custody](review-evidence/release.json) bind the exact reviewed bytes.

The five owned paths match the current corrected executor release: `SeasonIndexVM.cs` SHA256 `ca599d3d7f56f86d015b6a0a110e0887ab6d0ec449ce71428156e714c684b0ff`; season1 project `8eb31a41d340d0b159b20f3ef950961dc0c0fcb16e5a4f69b3bdef5054a44e52`, Program `9e0d0af8d9299965de65d6903ac30f9ddb5f2fff79d21630fb52ff2aeb7c64c4`, Stubs `a27b570ad6be0a060606fd6f1099776c24bcb4cffc9c27e32dfbcb3facab4248`, and new index probe `b383ee1f3f5757525bc3948fd591ad8252658b9708527c4aeddef0b4ccc4f1a4`.

## Implementation and fixture assessment

At VM lines 45–60, both owners start idle. Lines 84–97 and 167–172 reject null Parameter, active owners and structurally unusable metadata before any request, result mutation, observable result/loading write or owner finally. Readiness preserves nonempty unknown fields and empty keywords; it rejects empty/null fields, null entries/current/keyword, and missing/empty Conditions. No selection fallback or decoding policy was introduced. Conditions stays owned through its synchronous setter/publication. Refresh/LoadMore/direct conflicts remain quiet, and an idle Refresh retries unusable metadata once before a guarded result attempt.

Result ownership precedes Loading=true subscribers and Result=null subscribers. Only an acquiring invocation reaches release; duplicate/third calls cannot clear the active owner. Existing result row publication and Page advancement precede Loading=false, whose field write occurs before inline callbacks. No later old-owner write follows that notification, so the complete actual grid callback acquires and retains a genuine page2 owner. The full actual page navigation and selection handler execute in the maintained fixture over generic platform primitives; there is no copied owner/paging/selection rule in the delivery facade.

The independent product diff exactly matches the author's frozen diff. Existing request construction, data.filter/data.order/data.list reads, scalar casts, selection/order/defaults, filter escaping, sort=0, has_next treatment, first-page replacement, ordered append, empty stop, notices and retry-page retention remain unchanged. The unchanged full SeasonIndexAPI/ApiHelper and actual complete HttpResults class supplement the maintained delivery facade, retaining route/client/GET/no-cookie/page/pagesize/type/filter and real GetSign controls. These managed fixtures do not establish Windows UI or real transport behavior.

The maintained project adds only complete real VM/page/grid links. Program retains all original assertions literally, adding only the new invocation and combined exit. Lifetime497 sources, extractor, run.sh and SeasonDetailVM/page/player inputs remain exact. Stub additions are two scalar/display signatures; existing parser/delivery policy is untouched. The new file owns request recording/delivery, generic platform primitives and active assertions, is compiled and called, and adds no product abstraction, dead path or orphan file. Responsibilities and file structure remain appropriate within this scope.

## Independent private replay

All runs used SDK8.0.427/runtime8.0.33 and bound Newtonsoft13.0.3, private output/cache/home directories, and preserved fixture programs. No shared repository project or full suite was run by this reviewer.

| Proof | Exit | Observed denominator/outcome |
| --- | --- | --- |
| Same final maintained fixture, complete old-A VM | 1 | Original141 + lifetime497 PASS; **60 reached** new checks:17 PASS/43 FAIL, including3 failed scenario checks (2 timeouts,1 exception) |
| Same final maintained fixture, exact candidate VM | 0 | Original141 + lifetime497 + **75/75** new checks PASS |
| Unchanged original118, candidate VM + immutable old-A FD1 | 1 | **94 PASS/24 foreign FD1 failures**; all89 historical positives retained and all5 FI1 failures closed |
| Unchanged supplemental34, candidate full VM/page/grid | 0 | **34/34 PASS**; all22 historical positives retained and all12 FI1 failures closed |

Old/candidate maintained input manifests differ only in the complete VM. Fresh navigation starts at the real constructor without flag assignment or a warm-up. The old-A Case wrapper counts exceptions/timeouts as failures and exits nonzero; it does not count unreached assertions as PASS. Held-call checks observe returned Tasks before gate completion and drain them afterwards, retaining rejection assertions without deadlocking the unsafe baseline. Candidate executes every intended75 ordinary check. Original/supplement candidate receipts are byte-identical to the author's receipts, with identical assertion names/order to historical fixtures.

All18 original and16 supplemental bound historical artifacts, G4's historical179-control proof, actual helper/builder/signature inputs, seven relocated raw files,17 initial metadata files and16 current executor metadata files verified exact. G4 was not rerun; metadata relocation caused no test rerun. Complete raw outputs, source diff, manifests, receipts and compiled evidence remain privately under `/workspace/work/index-dynamic-contracts/index-review/`; repository review evidence is concise Markdown/JSON only.

## Freeze, release and limits

The five owned code/test hashes remained stable after replay. All reviewer outputs/private snapshots are now frozen and released for root integration review; no source/test/main-document/Git/shared-suite mutation occurred. Further same-owner source amendments require a separate exact grant and fresh binding. Combined118/118, integrated full51logical/52executable, Windows UWP/XAML/native/device/server and observed pushed-SHA hosted results remain separate gates. The fixed08:29:30UTC/16:29:30AsiaShanghai deadline is unchanged.
