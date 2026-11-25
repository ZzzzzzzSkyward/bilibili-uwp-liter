# Independent graphics review evidence

Status: **PASS / RELEASED**. Decision: [review.md](review.md). Reviewer-owned scratch: `/workspace/work/video-memory/graphics-review/`; released author evidence remains read-only at `/workspace/work/video-memory/graphics/`.

## Immutable bindings

| Input | SHA256 / identity |
| --- | --- |
| requirements.md | `fe8c2514db39a06181a8ea7aeafa2223239cb394918c757ca9828eecc7847263` |
| evidence.md | `54db896b6b930c297ce61558cf9a0e4d2439f3342734ca4afa5e7ee1995f7090` |
| release.md | `3b13af4bf025e094c6d5649e180f93342bca0fa5cd5cfebf5fc2663f51a7abcf` |
| Baseline source commit | `7356b94893377558191b0d70576a52bae704c1f9` |
| Complete PlayerControl source | `18fac7cc1afcdf38f5896364f1606dd05446358965f3aae51cff00a7929b1e4f` |
| Complete GenerateSubtitleItem method | `8775d4a82d509e92076cfaa0ab54475bbfad4cfaf8f5100a5f9c3929e94d24ea` |
| ActualOriginal.cs wrapper | `0c37aa52ebf94262bacb748a7034a5cfcc7412c8124e8f7dab855dbb96caeb47` |
| Private ActualScoped.cs | `5f1941338cb4346ed93e2a6f050ca01d196b6c1b973d726171c03f50c42d8877` |
| Author evidence-manifest.sha256.json | `9eb4124ed18c3098ca14015fc588b9a4df38e14a143573f8de6b831e8d4b38b2` |
| Actual UAP Microsoft.Graphics.Canvas.winmd | `28e0741c97829b1dd71e9531d5172c2d8de6ff3375040a4c0303a3e01dcb7cc2` |
| Independent metadata/raw.log | `14a75bf3176138b2a41712356f7672a91a9dfdae7e1ef4e70afe23e0573d9c59` |
| Independent unchanged-fixture probe.log | `8d4f2e5edc4b3634035e2c946fb1c1e8c6b9457a143010d478a4388f281da723` |

`verify-bindings.py` independently validates all145 manifest entries, compares full PlayerControl bytes and all7 tracked playersess files with `git show` of the baseline, slices the complete actual renderer, and proves byte equality to the original harness body plus its wrapper. It records source/project/method/candidate and playersess hashes in `bindings-verification.json`; extracted complete renderer/callers are `actual-method.cs.txt` and `actual-callers.cs.txt`. This was a focused frozen-source check, not a clean whole-tree claim. Git was only read; concurrent season/tooling owner activity is excluded.

The current source scan finds no UWP `project.assets.json` or `packages.lock.json`. An unrelated `src/BiliLite.gRPC/obj/project.assets.json` now exists, so the release's earlier broad no-assets observation is interpreted narrowly for the relevant Win2D/UWP graph. It supplies no actual resolved Windows Win2D binding and does not alter the dependency conclusion. The project itself has18 direct references, NSDanmaku2.2.0/Toolkit Controls7.1.3 and no direct Win2D pin; `bindings-verification.json` records them.

## Dependency, projection and upstream review

`verify-upstream.py` independently fetched and byte-matched35 official artifacts: the NSDanmaku2.2.0 and Win2D.uwp1.26.0 nupkgs plus all33 released exact-version upstream source/doc files. URLs, bytes and hashes are in `upstream/verification.json`. The package's `Win2d.githash.txt` identifies Microsoft/Win2D commit `36026efcb3fcd07af1d7a919fdf4201a07f05a20`; all source downloads use that exact commit, not the moving default branch.

Official nuspecs inspected: NSDanmaku2.2.0 → `Win2D.uwp` version1.26.0; Toolkit Controls7.1.3 → Media7.1.3 → version1.25.0. These bare NuGet dependency versions are lower bounds. Neither metadata inspection nor the portable restore identifies which package a separate Windows UWP restore resolved. No source project/dependency restore or dependency edit was performed.

The independent metadata build runs the released System.Reflection.Metadata inspector against the actual package UAP WinMD with `MetadataReaderOptions.None`. Its raw output exactly matches the released log:

| Value | Raw metadata / projected API |
| --- | --- |
| CanvasTextFormat | `Windows.Foundation.IClosable`; package XML `CanvasTextFormat.Dispose` |
| CanvasTextLayout | `Windows.Foundation.IClosable`; package XML `CanvasTextLayout.Dispose` |
| CanvasGeometry | `Windows.Foundation.IClosable`; package XML `CanvasGeometry.Dispose` |
| CanvasStrokeStyle | `Windows.Foundation.IClosable`; package XML `CanvasStrokeStyle.Dispose` |
| CanvasRenderTarget | Base CanvasBitmap; that base implements `IClosable` and documents Dispose |
| CanvasDrawingSession | `Windows.Foundation.IClosable`; existing source using remains |
| CanvasDevice | `Windows.Foundation.IClosable`; API closability does not grant per-cue ownership |

Official IClosable documentation states that .NET projects it as System.IDisposable; the stream documentation lists IDisposable. BitmapImage derives BitmapSource and supplies the returned Image.Source. Its output is not the temporary Win2D bitmap. No Dispose is invented for XAML TextBlock, BitmapImage, Image or Grid.

Exact upstream source/contract checks:

- `CanvasDevice.xml:177–207` explicitly warns that shared-device disposal can break other users. Bitmap `Close` in `CanvasBitmap.h:363–366` resets its device reference; geometry `GeometryDevicePtr::Close` at `CanvasGeometry.cpp:36–40` resets references; layout `Close` at `CanvasTextLayout.cpp:1832–1836` uses `ClosablePtr::Close`, which swaps the pointer at `ClosablePtr.h:64–69`. These do not close the borrowed device itself.
- `CanvasRenderTarget.xml:24–39` requires drawing-session close before SaveAsync because GPU commands are usually deferred until close. `CanvasBitmap.cpp:1334–1358` retains bitmap/device/stream ComPtrs in its asynchronous save action. The proposed scopes conservatively outlive both awaited operations; no early resource-close rule is inferred from capture details.
- Official BitmapSource.SetSourceAsync example keeps the stream inside a using through the awaited load and publishes the BitmapImage as Image.Source. It then closes the stream. This supports current output ownership/stream ordering, not a pixel or native-allocation claim.
- `ResourceWrapper.h:47–50` calls Close from the native destructor. This contradicts an inference that missing explicit disposal makes native resources immortal. Format/style Close and actual WinMD close contracts were inspected separately; no broad assumption based on class names is used.

## Actual-method and boundary controls

Reviewed the complete source renderer at PlayerControl:1849–1912 and both callers at1797–1847. There are exactly two call sites: changed cue text during playing and visible-text settings refresh. Both await rendering, then apply existing session/subtitle-reference checks. Returned visual graphs contain no format/target/layout/geometry/style reference. The private fixture runs the complete renderer, not its callers or a native timer/player.

The candidate differs by acquired scopes, initializer statements inside the format scope, and style assignment/finally at the original post-Clear point. No parameter, successful operation order, caller/generation behavior, package or renderer algorithm changes. Ordinary `using`, `try/finally` and null-conditional Dispose need no language/project upgrade. Reverse dependency cleanup occurs only after the existing drawing and stream scopes have closed.

| Independent control | Result / bound meaning |
| --- | --- |
| Released fixture copied unchanged and rebuilt | exit0; log bytes identical to release |
| Same desired-clean assertion on complete original method | exit1 after50 completed renders;50 unclosed format/target/layout/geometry/style each |
| Private candidate50 paired cue/setting renders | exactly50 releases per owned type;0 unclosed;50 retained output graphs read after cleanup;0 device closes |
| Successful render parameters/operation records |50 paired comparisons pass: padding/newlines, font/weight/alignment/measure dimensions, width+4/height/DPI, clear/border/fill, PNG1.0, source load and output graph |
| Pending save and source load | drawing closed before save; stream and all5 temporaries remain open at both gates; no premature returned output |
|23 constructor/setter/measurement/render/stream/save/load/output faults | actual original failure propagates; baseline77 accumulated unclosed model temporaries; candidate0 and exactly-once acquired cleanup |
|2 genuinely suspended save/source completion failures | both baseline and candidate suspend before failure; candidate closes stream/all5 owned objects; device remains usable |
| Additional reviewer-only controls |50 successes,23 faults,2 suspended faults pass exact acquired cleanup order/count and exception-reference identity |

Additional controls are generated by `review-controls.py` under `controls/`. The original/candidate method files remain byte-identical to their reviewed copies. Only reviewer model instrumentation adds the thrown exception reference. It checks drawing/stream closure before every acquired owned temporary, then style→geometry→layout→target→format, filtering out values never acquired. Constructors/factories throwing before return register no acquisition; format/style setter failures do register an acquired value and must release it. The original exception object is retained across cleanup in all injected cases, including suspended completion. `controls.log`/`controls-status.json` record PASS/exit0.

The test registry intentionally retains model resource objects for counter inspection. Stub drawing/measurement/PNG/source payloads verify contract order/parameters/output access; they implement no native encoding, text shaping or XAML visual behavior. The drawing-close injected fault marks that model object closed before throwing. This does not cover native Dispose failing before release, arbitrary cleanup exceptions masking an existing error, device loss, finalizers or driver/cache reclamation. These limitations remain explicit in the accepted requirements and decision.

## Reproduction and gate scope

Commands were run only in exclusive scratch, using `/workspace/work/maintenance-foundation/quality/dotnet/dotnet` (SDK8.0.427/runtime8.0.33):

```bash
python3 /workspace/work/video-memory/graphics-review/verify-bindings.py
python3 /workspace/work/video-memory/graphics-review/verify-upstream.py
# cwd: /workspace/work/video-memory/graphics-review
/workspace/work/maintenance-foundation/quality/dotnet/dotnet restore GraphicsProbe.csproj --configfile NuGet.Config
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project GraphicsProbe.csproj --no-restore
# cwd: /workspace/work/video-memory/graphics-review/metadata
/workspace/work/maintenance-foundation/quality/dotnet/dotnet restore Metadata.csproj --configfile NuGet.Config
/workspace/work/maintenance-foundation/quality/dotnet/dotnet run --project Metadata.csproj --no-restore -- /workspace/work/video-memory/graphics/packages/win2d.uwp.1.26.0/lib/uap10.0/Microsoft.Graphics.Canvas.winmd
python3 /workspace/work/video-memory/graphics-review/review-controls.py
```

The final script runs the baseline `--expect-clean original` command (expected exit1), compares metadata logs, then builds/runs the additional actual-method controls (exit0). No original240-player-assertion or full aggregate rerun is claimed; seven owning-stage files were verified unchanged and their original assertions/inventory must survive later integration. The eventual executor must rebind baseline/final actual-source proof to the granted implementation before product acceptance.

Linux lacks the Windows UWP/WinRT/XAML/GPU substrate; the downloaded native Win2D DLL is Windows PE. Windows compilation, actual resolved assets/final package, visual subtitle checks and warmed50-cycle native memory/GPU/idle controls remain FAILED/BLOCKED. No GC/cache policy or resource-memory attribution was added. Reviewer changed no source, repository tests, dependency, tooling, main document, index or Git ref.
