# G1 residual rebinding evidence

Status: **PASS / RELEASED; read custody released.** Decision: [residual-rebinding-review.md](residual-rebinding-review.md). Exclusive scratch: `/workspace/work/video-memory/graphics-residual-rebinding/`. Existing [implementation evidence](implementation-review-evidence.md) is retained verbatim and reused, not replaced.

## Exact source and input continuity

| Bound value | SHA256 |
| --- | --- |
| Earlier accepted full Control | `8682d1505752a2dfd99e835259d45429cf78ee3e358caf39553b1375a7d4a3cc` |
| Current frozen full Control | `284be647a5f633b4a39532d26b4a00856bab20eb901963ebafd545a8d52367c0` |
| Complete renderer, both versions | `5789d03300d297cb7bc6115ee0622417b5773960544b3b0899ec2bb8167fbcb7` |
| ActualScoped.g.cs, previous/current compiled input | `0795d31aca6923411e8e0bd76bec38b8b2d495c9e8068a1aa64a2be13fc45277` |
| SubtitleGraphicsProbe.cs | `a6fb7a1dc157691ead5cc147d336e3b695c821b7ef5bbf134091b4d223580764` |
| SubtitleGraphicsStubs.cs | `47d6260a14bad0dec2ce8fe4f16342b22734a46f392ca8f9f3d096126d1df754` |
| SubtitleGraphicsBaseline.cs | `d7844c564cba44289294913062eacd09ee54512fdc33bbe9533a079e797e0bb7` |
| Actual UAP Win2D WinMD | `28e0741c97829b1dd71e9531d5172c2d8de6ff3375040a4c0303a3e01dcb7cc2` |
| Current owning final.log | `cfa83d41f2a761558fca96860a51172c3c6efcf252ee0f05aad79bb42fca28d3` |
| Current captured PlayersessProbe.dll | `e0e2e2019374631e81fcf6bb4c3a531774e551fc25f306d762713ff0f0cd7ff8` |
| Current captured PDB | `0a9a4f4111b18ce075ba1340fc42b64e8a7e57982677c81e5900e5ed4e70235a` |

`rebind.py` independently reads the complete final renderer from2077–2157 and compares it byte-for-byte with both the captured accepted8682 source and the earlier review's `final-actual-method.cs.txt`. It also compares the complete `SubtitleTimer_Tick`/`UpdateSubtitle` bodies and source using header with8682. The final extraction receipt's method hash/range/source hash matches those actual bytes. The saved compiler input at `/workspace/work/video-memory/player-residual-fix/final-extraction/ActualScoped.g.cs` is byte-identical to the prior review's independently compiled copy; reconstructing its wrapper around the complete actual method also gives exact byte equality.

The three graphics fixtures compare against the earlier independent `audit-bindings.json`. All145 discovery evidence-manifest entries, including actual package, WinMD, projected API documentation, official pinned source/docs and metadata logs, still match their reviewed hashes. The historical final-review artifacts match the earlier private status receipt. UWP csproj, three history fixture files and aggregate runner compare against the previously accepted baseline hashes. Thus no new disposal/API/dependency premise is introduced by the residual work.

Broader wrapper inputs changed as declared by the residual writer: extractor SHA256 is now `211a8168286dcd7a34a762f329333789b99b9364b3dafc8eeeccbde6e7e12b2b`, project `71129cbbabc56f21a6613247958b7adc9d801e20189749a453df5829795bedae`, run.sh `5d3f8dbb01bdf82e7190ef69170acf235ba84893d7de8a1cd3242f7b345580a0`, Program.cs `eecb9205367c71c1ca3dbfa017033763cb12faeeeb0a6078b4378559541272ab`. They are not mislabeled unchanged. Inspection confirms the extractor still copies the complete uniquely anchored renderer without body substitutions, generated graphics bytes remain identical, the project still compiles `$(SubtitleGraphicsSource)`, run.sh still binds it to the generated ActualScoped file, and the graphics dispatch is unchanged. Program.cs becomes byte-exact accepted8682 Program after removing only its added residual dispatch block. This preserves the previously verified original240 assertions and graphics check.

The current broader frozen source/fixture/read-time dependency/tool manifest is `../resources/residual-implementation-evidence/bindings.json`; its actual hashes were verified. Before/after snapshots include all declared runnable inputs, current complete source, generated graphics compiler input, compiler/runtime/ref/Newtonsoft files, current log, captured binaries/PDB and new evidence files. All51 recorded inputs remain exact and equal across the rebinding audit; this count is a file-binding count, not an aggregate stage result. Private `before.json`, `after.json` and `rebind-evidence.json` hold the exact maps.

## Reused proof and current receipt

Earlier independent actual-source18fac→8682 red→green remains established by the unchanged review/evidence and private `graphics-final-review/` receipts. It covers50 successful renders with all50 outputs retained/read; exactly-once reverse cleanup for five acquired owned types; zero borrowed-device closes; pending save/source lifetimes;23 constructor/setter/measurement/render/stream/save/load/output fault boundaries;2 truly suspended completion faults; original exception-reference identity; and50 successful operation/parameter/output-contract comparisons. That evidence is reused because the complete method and graphics compiler/fixture/API inputs are identical.

The current writer receipt at `../resources/residual-implementation-evidence/{bindings.json,behavior-summary.json,release.json}` identifies final284be source, exit0, no compilation errors, five graphics groups PASS and241 existing aggregate checks. Actual raw `/workspace/work/video-memory/player-residual-fix/final.log` and all captured binary/runtime/PDB hashes match the receipt. The complete earlier independent final graphics log is a verbatim contiguous block in that current owning log; it is copied read-only as `current-owning-graphics.log` in reviewer scratch. It reports zero undisposed owned objects per type after50 renders,50 usable output graphs,0 device closes,23 fault cases with0 undisposed model resources,2 suspended faults,50 parameter/output comparisons, and all5 groups PASS.

This current owning result is inspected writer evidence, not a new reviewer execution. No graphics test, native build, owning stage, full suite, restore, package fetch or cache mutation was performed. Reproduce the rebinding check with:

```bash
python3 /workspace/work/video-memory/graphics-residual-rebinding/rebind.py
```

It exits0 after read-only hash/body/receipt verification and writes only exclusive reviewer scratch. No source/tool/test/main-doc/index/ref write occurs. The accepted G1 proof is extended to the current complete-source hash without inventing a fresh whole-file graphics test requirement.

## Unchanged ownership and external limits

Actual method inspection confirms format setters inside its acquired scope; target/layout/geometry at their original acquisitions; style construction/assignment after Clear and before DashStyle; existing drawing close→awaited PNG save→awaited source load→stream close→output assembly; then style→geometry→layout→target→format release. The shared device remains borrowed/open and the returned Grid/Image/BitmapImage remains transferred to the caller. No early temporary/stream/output close, render parameter change or catch was added. Identical fixture error checks preserve original injected exception objects under the existing model cleanup behavior.

API reuse still means actual Win2D1.26.0 lower-bound IClosable/projected Dispose evidence for format/layout/geometry/style and inherited target close; no Windows resolved-assets binding is inferred. The upstream destructor and drawing-close fault-double limits remain explicit. Model registries/counters and payload equality do not measure native survival, pixels, RAM/private commit/GPU, finalization, caches or the user's50-video journey. Native Dispose failure before its own cleanup and cleanup-error masking remain unverified. Actual Windows assets/build/final-package rendering and warmed subtitles-off/on50-cycle/idle measurements remain FAILED/BLOCKED.

Only G1 continuity/reuse is accepted here. The owning and fresh reviewers separately decide PF1/FA1/FA2/FA3; root owns common51/52 verification and later integration/publication. Historical PASS artifacts were preserved and all graphics read custody is released.
