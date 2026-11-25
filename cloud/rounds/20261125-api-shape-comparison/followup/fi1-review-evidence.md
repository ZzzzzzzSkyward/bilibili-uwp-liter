# FI1 amendment independent re-review evidence

Reviewer `api_followup_review`; 2026-11-25 UTC. Exclusive new scratch **R** is `/workspace/work/api-shape-comparison/followup-review/revision-review/`. Initial review/evidence and all earlier scratch are preserved. Read ACTIVE rereview-dispatch, revised requirements/evidence/amendment, author revision release/binding/complete source fixture and actual caller/control sources. Rechecked fixed deadline and actual clock; no window was restarted. Repository HEAD remained `7356b94893377558191b0d70576a52bae704c1f9`; current concurrent primary-gate correction was preserved.

## Binding and unchanged history

Author revision release SHA256 is `8538891ca2bd972fd08a9af9dd391ba0320ceb38430553430682275efbd55a33`. The exact authoritative revision binding is `d0dfac6cb670d84bef99ffe8a352b3384b338b4555caca90c395adda558895cc`; R/input-binding.json records the actual full release hash and reviewed document hashes. Every revised release document,15 source input, supplemental artifact and6 tool input matched before independent replay. Every original author binding artifact and all25 original current source inputs also still matched. Original118 fixture,89 positive controls, G4 fixture and unsafe7-check reviewer history were not edited.

| Reviewed repository artifact | SHA256 |
| --- | --- |
| `followup/requirements.md` | `388bf069bdcb4ab0360159f8827a1c6d4c2101788975cf740c36ba2ee95a603c` |
| `followup/evidence.md` | `a8a9356cdd30c9116b72e39e6780036672701b40c2310ee835054b2a6c789641` |
| `followup/fi1-amendment.md` | `346566bb67b9ec3f14663266b973eaac5dc1bdbfb40aea4dc21500c9000a2fed` |
| Initial `followup/review.md` unchanged | `6129f1af188469efba646d2974a33745932af4408aae7bec9caf1e10464c413b` |
| Initial `followup/review-evidence.md` unchanged | `5444922416dd109229248ee96e5bfcfde6d0ab1b6ddb8788321aeae199ca40e7` |

FD1's accepted payload hash independently matches `867dd52066526d9dd24078fa1f5f5a727105b5d0c7d2ae2857c350da1c43323b`; its defined hash boundary is the substring after `## FD1 —` and before `## Prospective`. Full revised source/request/filter/error constraints remain as previously accepted.

## Independent complete-source supplement replay

Copied the released supplemental tree into R/supplement without bin/obj/logs and compiled it privately using the same existing exact SDK8.0.427/runtime8.0.33/Newtonsoft reference. Private DOTNET_CLI_HOME/NUGET_PACKAGES contain incidental CLI/cache state. No shared project, dependency installation or source repair occurred.

```bash
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project /workspace/work/api-shape-comparison/followup-review/revision-review/supplement/Followup.csproj -c Debug
# exit1; CHECKS=34 FAILURES=12
# ready-red3, conditions-red5, usable-red2, result-red2
# all22 positive assertions PASS
```

All34 individual assertion outcomes and failure groups exactly match the author. The old-source condition-completion diagnostic has a scheduling-dependent unsafe count: independent observation3 result calls versus author receipt2, with3 conditions calls/3 publications and Page1 in both. The desired-safe exactly-one assertion fails in both. This asynchronous managed diagnostic is not treated as native timing evidence or an exact invariant; no receipt-byte equality is claimed. Author source/log/receipt remain untouched. Exact independent observation and hashes are recorded in R/review-binding.json.

| Independent R artifact | SHA256 |
| --- | --- |
| `supplement/Program.cs` | `eec24a88aa1c831aa6dfc492a601c35ed1075d18c25df01e7335cc5c906eac9f` |
| `supplement/Platform.cs` | `f46e98b366acadd242fde08e60024228a09ffa4e54792647de0bf3de57aa01b0` |
| `supplement/Stubs.cs` | `6b67ae0af9500c16d979620aa41acb9773dd55bc371fbca4c1a22aa2f70bc201` |
| `supplement/Followup.csproj` | `81d345c8d661694d49c2bd6d8e1c469aefd34a0e8d14ef6cd3e8d4e86e4bc02a` |
| `supplement/HttpResults.actual.cs` | `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93` |
| `supplement/run.log` | `2b3074dd1f179bd9e386ba57a2a5fab7c58e61bf88f0fc9499baffb28e54e1b8` |
| `supplement/bin/Debug/net8.0/receipt.json` | `7a9dc66ae447f948f0bf9869b17477dbc5938ebd120e30e398d50bc174fafb9e` |

The supplemental full SeasonIndexVM, SeasonIndexAPI, ApiHelper and all original sources remain unchanged. Complete real AnimeIndexPage and MyAdaptiveGridView are additionally compiled, hashes `89e2d7b0e9aa4d827db38cb54ca5945473ce3781545a70932f5c1dceb579d8cc` and `7e5d833bcc8223ad128979ea731b8f055b53bdfb41a4297655c00f92460622c8`. Platform.cs implements generic navigation/selection/base-page/InitializeComponent and dependency-property storage/change callbacks/template ScrollViewer. It stores the value before delivering callbacks and only invokes them for actual value changes. All conditions/result/page/selection/grid-load policy executes from the real product files. No policy-bearing stub, lifecycle reset or private field assignment was added.

The supplement's IdlePage helper deliberately invokes actual public LoadResult once before metadata. Old source becomes idle because of its already proven faulty guarded finally; correct source must quietly reject this entry. This is a clearly labeled scheduling setup. It is not the fresh navigation acceptance; that remains owned by the original untouched118 fixture and required direct-constructor maintained test. The public warm-up does not replace original red evidence or prove a repaired state.

## Requirement closure checks

- Constructor/startup: separate idle owners allow first LoadConditions and first usable result. Null Parameter rejects before all page/row/flag/error mutations. Guards inside the VM cover actual direct calls, not just page/UI suppression.
- Pending conditions: direct duplicate/Refresh/LoadMore/LoadResult reject without releasing owner or resetting Page. Conditions owns its synchronous publication, so the actual selection handler sees its guard true. Failure releases only the acquiring invocation; later legitimate Refresh retries once. Business/transport/malformed notice counts1/0/1 remain active positive controls.
- Readiness: nonempty collection/field, nonnull item selection/keyword; accepted empty keyword and unknown nonempty fields remain valid. Unmatched selection is preserved, quietly declines result acquisition and can trigger one metadata reload from a legitimate Refresh. No new selection fallback, parser conversion or normalization is implied.
- Result reentry: acquire before Result=null PropertyChanged; a one-shot actual RefreshCommand observer must be rejected. Existing held-result non-owner proof stays frozen. Row/Page commit precedes completed-owner release; older finally has no later write that can clear a new owner.
- Actual grid: full OnApplyTemplate/Loading callback over generic DP primitives actively requests pages[1,2], observes one published row/Page2 with page2 owned/held, then retains ordered rows/Page3 after the viewport fills. Four positive assertions preserve this existing valid append and exclude a completion-wide gate that would suppress autofill.
- Scope: only SeasonIndexVM.cs product changes; four existing season1 fixture paths can host complete-source links/platform delivery primitives and a capture facade. Existing lifetime extraction/run.sh remain untouched. Independent real builder/signer gate retains route/page/pagesize/filter/data/error compatibility. Disjoint FD1/API02 remains accepted and frozen.

No further P0/P1/P2 requirements finding remains. Full fixed source and unchanged-fixture repaired-green proofs are future execution/review gates, not results of this read-only review. Replaying unchanged original118/G4 again was unnecessary: their exact input/source/tool hashes remain unchanged from this reviewer's preceding independent runs.

## Release

Final source/document hashes still match at freeze. Only fi1-review.md/fi1-review-evidence.md and exclusive R scratch were written by this re-review. All read custody and outputs are frozen/released. No source/tests/main-doc/Git/index/ref/cache changes, shared/full-suite build, foreign/live/auth/write call or native/server/hosted claim occurred. Current corrected primary gate/source/docs/archive release and fresh safe dev binding remain prerequisites to later implementation. Checkpoint07:41:29UTC and hard stop08:51:29UTC remain fixed.
