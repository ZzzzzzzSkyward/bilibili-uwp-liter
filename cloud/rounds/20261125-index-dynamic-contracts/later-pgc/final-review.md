# FI2/FI3 independent implementation review — PASS

**PASS for the exact two-path FI2/FI3 amendment over released FI1. No unresolved in-scope P0/P1/P2 finding.** Reviewer `index_contract_review`, 2026-11-25 UTC. [Binding](implementation-review-evidence/binding.json), [commands](implementation-review-evidence/commands.json), [counts](implementation-review-evidence/summary.json) and [release](implementation-review-evidence/release.json) contain exact source/fixture/tool/output/custody bindings. This remains selected **PARTIAL_PASS**, with no SR1/full/native/hosted acceptance.

Reviewed VM SHA256 `34536344c659d90394246e65c6dda34252be450f056b6aa7c926621429988a87` and maintained probe `0a74f449f7594fd5e9d064ce6b4f68feea2cb45a9d3eb0fb4022df60f078c593`, starting from frozen FI1 `ca599d3d7f56f86d015b6a0a110e0887ab6d0ec449ce71428156e714c684b0ff` / `b383ee1f3f5757525bc3948fd591ad8252658b9708527c4aeddef0b4ccc4f1a4`. Original requirements `6c13cd16…`, approved amendment `a261bff0…`, independent amendment PASS `8a24b7b1…` and the exact root grant were revalidated. The versioned approved140 Program stays `a1b843fae39658e378083ecc78dc50a0f98b1e73f1e0affdb90460fe7e724b14`; initial wrong-oracle140 and CHANGES history remain immutable.

## Product and responsibility assessment

The independent two-path diff exactly equals the released author diff. VM line144 selects the first exact decoded keyword match, then retains the original label match and first-entry fallback. It introduces no trimming, normalization, numeric/code allowlist, default change or filter policy. Existing casts, malformed/empty catalog notices and old-metadata retention remain intact.

Lines193–195 inspect the optional flag only after the existing successful code/list decode accepts a nonempty list. `JTokenType.Integer` plus textual integer value `0` recognizes exactly integer zero without narrowing/conversion or overflow. Recognition and the internal terminal write occur before any row publication, Page advance or Loading=false callback. Null/absent/string/bool/float/other/negative/huge/object/array values remain list-based continuation. Empty/missing lists and unaccepted business/transport/wrong-payload/malformed-list responses retain their existing publication, notices and retry behavior; no terminal fact is committed from their has_next text.

All FI1 owner/readiness guards, acquisition/rejection paths, Conditions publication, result reset reentrancy and row/Page-before-release ordering are byte-identical outside these two accepted rules. Genuine integer1/absent grid page2 still acquires and retains its owner. No API/helper/parser/page/grid/model/generation/signature/cookie/auth change is introduced. Existing project, Program, stubs, runner, extractor, original141 and lifetime497 inputs remain exact. The same compiled test file gains meaningful active assertions; primitive/delivery code is unchanged, with no copied owner/selection/paging rule, orphan or dead implementation.

Both paths are regular nonexecutable files and normalize to Git mode **100644**. The executor's `0o600` entries describe private filesystem permissions; they do not assert an executable product mode. No Git command or mode mutation was performed during this review.

## Additive setup and assertion preservation

Every line of the first FI1 probe remains an exact ordered subsequence. Its75 assertion names, expressions, order and outcomes remain unchanged in both independently replayed arms; the separate first-FI1 source/fixture/proofs/review are preserved.

The addition before the old missing-list assertion is a justified precondition restoration. Immediately before it, the original integer0 append has accepted rows104,105 at Page3; FI3 now correctly makes that state terminal. The inserted actual public Refresh requests a nonterminal row104, then a normal result append accepts row105, restoring the same two-row/Page3 state before the unchanged missing-list response and assertion. This runs on both sources, uses no lifecycle flag/private assignment, and leaves earlier FI1 scenarios untouched. It neither skips the missing-list dispatch nor suppresses terminal checks: separate first/append/direct/command/short-grid terminal assertions actively fail on the baseline and pass on the candidate. The original row/Page assertions are retained literally.

## Independent private replay

SDK8.0.427/runtime8.0.33/Newtonsoft13.0.3, exact complete real VM/page/grid/API/ApiHelper/HttpResults/ToInt32/signer closure and private output/cache/home directories were bound. All raw evidence stays under `/workspace/work/index-dynamic-contracts/index-review/pgc-amendment/`.

| Same immutable fixture | Released FI1 baseline | Candidate |
| --- | --- | --- |
| Approved PGC-REV1-140 |129 PASS/11 failures; exit1 (6 sort,4 terminal,1 terminal-grid)|140/140 PASS; exit0|
| Final maintained season1 |141+497 PASS;152 reached index checks,139 PASS/13 failures; all first75 FI1 PASS; exit1|141+497+152 PASS; exit0|
| Unchanged original118 with frozen FD1 |Historical proof preserved|118/118 PASS; exit0|
| Unchanged supplement34 with frozen FD1 |Historical proof preserved|34/34 PASS; exit0|

Each old/candidate pair differs only in the complete VM. There are no timeouts, exceptions, omitted checks or changed denominators in these amendment replays. All127 applicable approved140 positives and all129 FI1-arm positives remain green, including constructor and stronger public Refresh checks. All11 proposed failures close. Maintained13 failures are confined to new assertions; all77 additions pass on the candidate. All four private receipt outputs and both maintained assertion-result sequences equal the author's exact results.

Actual app route/GET/no-cookie/type/page/pagesize/filter/sort/signature controls, all six season types, fallback/selection, optional-flag forms, notices, retries and actual synchronous grid boundaries remain active in approved140. The full HttpResults class and actual ToInt32 extraction equal current product bodies. G4's earlier fresh179 PASS is reused only after verifying exact source/fixture/reference/tool/report identities; it was not rerun.

## Preservation, freeze and limits

All61 named review/history files and the113 initial plus56 amended private requirements-review artifacts verified exact. First FI1 review/proofs, original118/supplement34/G4 history and initial incorrect-oracle140 remain untouched. The three protected FI1 wiring/Program/stub hashes and all896 compared outside-owned source/test inventory paths remain exact;933 applicable paths from the earlier fresh snapshot, including protected/main-document inputs, also remain exact. The four disjoint prospective SR1 paths are excluded from these stability claims and from this acceptance. No moving API02 source was used as a replay input.

Both reviewed code/test hashes and all private replay inputs remained stable after execution. All review outputs and private snapshots are **FROZEN / RELEASED**; only this report and concise `implementation-review-evidence/**` Markdown/JSON were written in the repository. No source/test/main-document/Git/shared project/full-suite/dependency/live/foreign mutation occurred. Independent fresh/final integrated51logical/52executable, SR1, Windows UWP/XAML/native/device/server and pushed-SHA hosted results remain separate gates. Fixed08:29:30UTC/16:29:30AsiaShanghai deadline is unchanged.
