# Independent subtitle graphics requirements review

**PASS — RELEASED for inclusion in the later combined player grant.** Expert reviewer: `memory_graphics_review`, `gpt-6.1-sol` / `xhigh`. No P0/P1/P2 requirement correction remains. This is requirements/design acceptance, not a product implementation review, source execution grant, Windows build pass or native-memory result.

Reviewed the frozen [requirements.md](requirements.md), [evidence.md](evidence.md), [release.md](release.md), complete actual renderer and both callers, actual package WinMD/projected API documentation, exact-version official upstream source, and privately reproduced controls. Exact hashes, commands, results and limits are in [review-evidence.md](review-evidence.md). PlayerControl and all seven tracked playersess files still match baseline `7356b94893377558191b0d70576a52bae704c1f9` at review.

## Accepted finding and API boundary

G1 is E1/P2 ownership hardening: each successful actual renderer invocation creates five owned Win2D temporaries without deterministic release. The package's actual UAP WinMD confirms `IClosable` for format, layout, geometry and style, and inherited closability for the render target through CanvasBitmap. Package XML and official .NET projection documentation support ordinary `using`/`Dispose`, including the less-obvious text format and stroke style. NSDanmaku2.2.0 establishes a Win2D1.26.0 lower bound; it does not identify an unobserved Windows restore's resolved version.

Do not attribute the user's50-video growth to this finding. Exact-version upstream destructor cleanup, WinRT wrapper timing, finalization, GPU completion and caches remain relevant and unmeasured. Test registry entries and explicit-disposal counters establish call ownership only; they are not surviving managed roots, native allocations, RAM/GPU slopes or pixels.

## Accepted implementation boundary

The conservative private candidate is safe to use as the scope reference, subject to the executor rebinding it to final actual source:

- Acquire the format with a scope immediately after construction; initialize its properties inside that scope. This covers setter failure without changing successful parameter order.
- Nest target, layout and geometry scopes at their present acquisition points, enclosing the existing drawing/stream work and output assembly. Retain them until both awaited operations and output assembly finish. No earlier-disposal inference is accepted.
- Keep style acquisition after `ds.Clear`, at the existing DrawGeometry operation point. Assign the newly constructed style to the nullable local before its DashStyle setter, and release it from the enclosing `finally` after the drawing session has closed. Keep the existing drawing-session scope intact.
- Preserve session close → awaited PNG save → awaited source load → stream close → output assembly, then style → geometry → layout → target → format cleanup. The borrowed shared device stays open; the returned Grid/Image/BitmapImage stays owned by the caller/XAML.
- Preserve all existing text normalization, measurement, font/weight/alignment, dimensions/DPI, colors, border width/style, PNG quality, image properties and output graph. Keep both callers' generation/reference checks and publication behavior unchanged. No catches, swallowed failures, renderer rewrite, caching/GC policy or dependency change belong to G1.

Moving the two initializers into acquired scopes is necessary: putting an object initializer in a `using` resource expression would not acquire the variable if a setter throws. Style allocation must also stay after Clear; hoisting it ahead of drawing would change the failure/acquisition boundary. The private candidate satisfies both constraints with UWP-compatible ordinary C# constructs.

## Independent control results

The unchanged released fixture was copied into exclusive reviewer scratch and rebuilt with SDK8.0.427/runtime8.0.33. Baseline desired-clean exits1 after50 completed renders with50 undisposed instances of each owned type. The private candidate/paired fixture exits0: zero undisposed objects per owned type, exactly-once drawing/stream cleanup, all50 returned graphs retained/read, zero shared-device closes, matching operation/parameter traces, pending save/source controls,23 fault boundaries and2 genuinely suspended async completion faults.

Additional reviewer-only actual-method controls pass50 successes,23 fault boundaries and2 suspended failures, checking exact acquired cleanup order/count, constructor versus setter acquisition boundaries, the original exception **object reference**, preserved output and a later borrowed-device consumer. The actual method copies remain unchanged by those additional controls. Native Close/Dispose failure before its own release is still unverified; a stub that marks drawing closed before throwing does not prove that case or repair R4.

## Integration and remaining gates

Root may add accepted G1 to the **same** future `memory_player_executor` grant for R1/R2/R3/RR1. Only that owner may edit PlayerControl and the existing playersess stage after explicit activation. Preserve all original240 assertions and the51 logical/52 executable aggregate inventory; add actual-method controls within the existing stage. Reproduce genuine baseline-source → final-source red→green against unchanged assertions and record final bindings. No new independent graphics product writer is authorized by this review.

Windows/UWP compile, resolved Win2D assets, visual CJK/multiline/bold/alignment/color/size/DPI subtitle checks, GPU/native/private-memory measurements and the warmed50-cycle subtitles-off/on/idle protocol remain FAILED/BLOCKED here. The requirements retain concrete Windows follow-up. This review neither closes BR-21 nor claims a native leak reduction.

Reviewer writes are limited to this file, `review-evidence.md`, and private `/workspace/work/video-memory/graphics-review/`. No product/test/tooling/main-document/dependency/Git-ref/index mutation, shared-tree integration or full-suite rerun was performed. Other owners' in-flight files are outside this review's source claims.
