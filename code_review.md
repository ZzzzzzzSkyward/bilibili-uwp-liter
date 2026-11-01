# Review pass — 2026-11-01 (in-house, source only)

Reviewed the uncommitted work on `dev` (working tree on top of `6500e14`). Static source inspection:
no build, no device, no API call. Findings are source-provable unless marked *needs device*.

## BR-43 — player toast vertical centering (`Controls/PlayerToast.xaml`, `.cs`)

Accepted. Both blocks now share one line box (`LineStackingStrategy=BlockLineHeight`, `LineHeight =
BaseFontSize * 1.1` through `ApplyScale`), and the single optical constant `BaseIconOpticalOffset` is
applied through `IconGlyph.RenderTransform` so measured size and icon spacing are untouched. Real
payloads (`hh:mm:ss`, `N%`, `跳过OP`, `1.25x`) stay inside the 22 DIP box at every scale — worst case
`跳过OP` headroom 0.19 DIP — and nothing reaches the 8 DIP border padding. No storyboard, remeasure or
collapse regression; `ApplyScale` runs before `SetContent` on the only construction path.

Fixed during review: the offset was applied unconditionally, so the text-less mute pill — one symmetric
glyph with nothing to align to — was pushed ~1.4 DIP high. `ApplyIconLayout()` now gates line height,
offset and margin on whether the text block is visible, and is called from both `SetContent` (after the
visibility flip) and `ApplyScale` (the service calls it while the text block is still visible).

Open, *needs device*: the FA `lineGap` question puts the correct constant between `-2.4` (ships) and
`-1.1`. If the device shows the icon high on digits, that one constant is the only edit.

## P4 step 1 — season detail (`Parsing/Season/*`, `Models/Api/Season/Web/*`, `ApiEnvelope`, `SeasonDetailVM`)

Verdict: correct. The `result` selector is additive (`data` callers keep the old body and strings), the
parser normalises exactly the shapes that used to NRE (`episodes: null`, section `episodes: null`,
throwing `id`/`status` getters), and the mapper makes the old merge/split/dedupe rules explicit.

- Merge order: the old code reversed preview-section order through chained `InsertRange(0, …)`; 正片
  order is identical before and after, and no consumer indexes positionally (current episode, directory
  rows and downloads all match on `id`).
- `section_type` from the source array reproduces `IsPreview` for every episode in exactly one array.
- Dedupe key `id ?? ep_id` is the same expression that produces the row identity, so dedupe and app
  identity agree.
- Deletions are clean: every XAML binding on the retained model still resolves; the retained
  `SeasonDetailStyleItemModel`/`btnOpenIndexWithStyle` pair is unreachable dead code, not a reference.

### Should fix before merge

1. `Modules/Detail/SeasonDetailVM.cs` begins with two BOMs (`EF BB BF EF BB BF`). Cosmetic, compiles.
2. **The no-toast contract does not actually hold.** `ApiEnvelope.ReadPayload` reaches
   `HttpHelper.GetJObject` for a body that is not JSON, and that helper toasts
   (`Helpers/HttpHelper.cs:596`). The old season path used `GetJson`, which only logs, so this toast is
   new for the page. The probe hides it: its stub returns `null` silently (`plan/tests/season1/Season1Stubs.cs:58-68`)
   and `ParsePathShowsNoToast` clears the recorded list inside `LoadSeason` (`Program.cs:470-479`), so
   only the last body is ever observed. Fix in the envelope (silent decode) and add a non-JSON fixture
   that is red today.
3. Documented divergence needing a fixture or device check: an episode listed in both
   `result.episodes` and a section now survives only as a preview, so it leaves the 正片 tab
   (`Program.cs:348-368` pins the new behaviour). No wire evidence either way.
4. Optional, pre-existing: a refresh that fails *after* a successful load keeps the previous
   `Detail`/`Episodes`/`Previews`, so the page rebuilds the directory from the old list. The old code
   behaved the same way; no probe covers success→failure.

Noted, not actioned: `Controls/PlayDirectoryControl.xaml:105` binds `Image.Source` to `Cover`, which is
a URL string, with no image-source converter — directory thumbnails render blank.

## BR-44 — 播放目录 button throws on every click

Confirmed from the maintainer's device log: `Storyboard.Begin()` fails with `Cannot resolve TargetProperty
Opacity on specified object` (HResult `0x800F1000`) from `PlayerControl.PlayDirectoryButtonPress`, and the
throw lands before `DirectoryRequested?.Invoke`, so the drawer never opens on either page. `BottomBtnDirectoryPress`
is a `CompositeTransform` (`Controls/PlayerControl.xaml:726`) and the code animated `ScaleX`/`ScaleY` **and**
`Opacity` on that one target; a `Transform` has no `Opacity`. Introduced by `5ed8d17`.

The earlier static trace was right that the click path contains no `throw` of its own — the failure is the
storyboard compiler refusing the property path, not an exception in the directory model.

Fix shape: fade the button's content `StackPanel` (a real `UIElement`), scale the transform, keep the
"flash once per click" intent and the `DirectoryDrawerMotion` durations, and add a probe that fails when a
`Storyboard.SetTargetProperty` names a property the target type does not have.

## Dispositions after the fixes (2026-11-01, dispatcher)

All four season findings and the BR-44 instruction are closed in `ed9f248` / `a99c3f8`; BR-45 (the noted
`Image.Source` binding) is fixed in `433f128`. Evidence below; nothing here is device-verified.

1. **Double BOM** — no longer true. `xxd -l 12` on `Modules/Detail/SeasonDetailVM.cs` shows one
   `ef bb bf` before `using`; the file was rewritten when the parse layer was extracted.
2. **No-toast contract** — fixed in the envelope, and the probe can now see the defect. The stub
   `HttpResults.GetJObject()` models the product (toast + `null`) instead of returning `null` silently, so
   the new non-JSON fixtures (plain text, HTML error page, empty body) failed with
   `解析json对象失败` *before* the product change and pass after it. `HttpResults.TryGetJObject` is the silent
   decode; `GetJObject()` is reimplemented on it, so `JObject.Parse(results)` appears once in `HttpHelper.cs`
   and the 31 real call sites are untouched. `ParsePathShowsNoToast` now checks each body individually
   instead of observing only the last one. `plan/tests/season1`: 81 → 109 assertions.
3. **Dedupe divergence** — recorded, not assumed. The parser's field table gained the row and rule 5, and the
   claim was checked against the removed code (`git diff SeasonDetailVM.cs` shows the undeduped
   `InsertRange(0, …)`): a duplicated ep used to publish twice and stay in 正片, the mapper keeps the section
   copy only. No wire evidence either way; `DuplicateEpisodesPublishOnce` pins it.
4. **Refresh failure after a successful load** — pinned without changing product code:
   `RefreshFailureKeepsTheLoadedSeason` asserts the second, rejected call sets `ShowError` / `ErrorMsg` while
   `Detail`/`Episodes`/`Previews` keep their identity and ids (`1,2,3`), and that no toast appears.

**BR-44** — fade target moved to the content `StackPanel`, scale stays on the transform, and a `Begin`
failure is logged rather than allowed to swallow the click. The requested probe exists: `plan/tests/dirpress`
(31 checks) reads the XAML `x:Name` → element type map and the `SetTarget`/`SetTargetProperty` pairs, and it
exits 1 against `6500e14`: `Opacity 挂在变换 BottomBtnDirectoryPress(CompositeTransform) 上 —— Transform 没有 Opacity` ,
plus the two `SetTargetProperty` failures that follow from it.

**BR-45** — the noted binding was a real gap in four templates (directory rows, playlist, UGC-season, live
honour icon). All four now use `imageConvert2` with the `'200w'` parameter already used by the season header,
so `Image.Source` receives a `BitmapImage` and an empty cover lands on the placeholder. `DownloadPage:45` is
deliberately unchanged: that model already stores a `BitmapImage`. Residual, unverified on device:
`Controls/Dynamic/DynamicTemplate.xaml:41` binds `OneRowInfo.Cover`, a `string` whose size suffix is baked in
by `DynamicParse`; same shape, still bound without a converter.
