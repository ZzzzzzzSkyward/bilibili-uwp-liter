# Hot tab video card: readable titles without tag overflow

Move the recommendation tag onto the thumbnail, keep the two-line title and both metadata lines in a dedicated text column, and size the grid slot from that layout's actual content budget. At narrow widths or large text sizes, stack the thumbnail above the text. This keeps the useful tag without making it compete with the title for vertical space.

This document proposes a new UI; it does not implement it. The reported overflow is the user's observation. The diagnosis below comes from source inspection, not a reproduction or screenshot measurement.

## Scope

- Main repository: `ZzzzzzzSkyward/bilibili-uwp-liter`, local `dev` tracking `origin/dev`.
- Reviewed remote tip: `3b006c1585ac9a9284c9330138a735852def8721`, fetched on 2026-11-02, Asia/Shanghai.
- Continue `.agents/skills/work-code-review/SKILL.md` with the established branch override and this new output path, `HOT_CARD_PLAN.md`. Read `bug_report.md` and `code_review.md`; preserve them and `CARD_PLAN.md`.
- The latest report-only marker is `3b006c1`, which changes only `CARD_PLAN.md` and records remote `b9b17b9848928ab14f55f70e39c75d94e40e86b9`. Since the `code_review.md` source boundary `63e1765651eb1275f8731be5c45cf8f9427f0a6c`, changes are documentation only. This is a newly requested investigation of existing Hot card behavior, not an introduced regression in those documentation commits.
- A Luna reviewer independently traced the Hot template, model, and actions. No applicable `AGENTS.md` was found. Source/Git review only: no builds, tests, app execution, package inspection, API calls, linters, formatters, or other validation. The future checks below have not been executed.
- Scope is the video items in `HotPage`, not the top shortcut tiles, rankings, recommendation page, or dynamics cards.

## 1. Why the current card overflows

[HotPage.xaml:8](src/BiliLite.UWP/Pages/Home/HotPage.xaml#L8) uses `MyAdaptiveGridView` with `ItemHeight="100"` and `DesiredWidth="560"`. The template reserves a 160px cover column and puts everything else in one vertical StackPanel:

| Current element | Source | Constraint |
| --- | --- | --- |
| Title | [HotPage.xaml:35](src/BiliLite.UWP/Pages/Home/HotPage.xaml#L35) | Font size 20, wraps to two lines, ellipsis |
| Recommendation tag | [HotPage.xaml:36](src/BiliLite.UWP/Pages/Home/HotPage.xaml#L36) | Font size 12, wrapping enabled, no line limit; border adds padding/border/margin |
| Metadata 1 and 2 | [HotPage.xaml:39](src/BiliLite.UWP/Pages/Home/HotPage.xaml#L39) | Two additional text blocks with 2px top margins; font size inherited |
| Item container | [HotPage.xaml:9](src/BiliLite.UWP/Pages/Home/HotPage.xaml#L9) | Margin 8, with no explicit content-padding policy in this style |

An illustrative height budget is two title lines at roughly 24px each, a tag line around 16px plus its border, and two metadata lines around 18px each. With three 2px gaps, that is already about `48 + 18 + 36 + 6 = 108px`, before additional container effects. These are explanatory estimates, not measured UWP font metrics. A second tag line adds still more demand.

`MaxLines="2"` only limits the title. `TextTrimming="CharacterEllipsis"` on a wrapping tag does not establish a one-line tag. A vertical StackPanel can request more height than the fixed grid slot allows; its children do not make `ItemHeight` grow. The exact clipping/overlap behavior depends on the container/template at runtime, but the conflicting height constraints are visible in source.

There is also a small empty-state problem: visibility is bound to the `rcmd_reason_style` **object** on the TextBlock, not to its text on the enclosing Border. A null style collapses only the TextBlock; a non-null style with null/empty text still produces a visible empty TextBlock, and the Border remains in either case. Bind the whole chip to normalized nonempty text. The existing [DisplayConvert](src/BiliLite.UWP/Converters/DisplayConvert.cs#L9) has a null/empty-string path that can serve that new binding, but whitespace-only content needs normalization. Its boolean behavior is inverted, so a future `HasReason` bool must not accidentally use the string-visibility binding unchanged.

The tag is `rcmd_reason_style.text`, an arbitrary string in [RecommendRcmdReasonStyleModel](src/BiliLite.UWP/Modules/Home/RecommendVM.cs#L463). No source-level limit guarantees it fits one line. It is a recommendation reason, not necessarily a category or genre.

**The fix must give every visible region a bounded layout and make the item slot large enough for those regions. Raising 100 to another fixed number while retaining an unbounded tag only moves the failure point.**

## 2. Proposed card: put context on the image

Horizontal card at normal text size:

```text
┌──────────────────────────────────────────────────────────┐
│ ┌──────────────────┐  A useful video title can take      │
│ │ [热门推荐…]      │  two lines without losing metadata…│
│ │                  │                                    │
│ │     thumbnail    │  First supplied metadata line       │
│ │          [12:34] │  Second supplied metadata line      │
│ └──────────────────┘                                    │
└──────────────────────────────────────────────────────────┘
```

The tag sits at the thumbnail's top left. The existing cover label stays at bottom right. Both use a readable backed pill, each limited to one line. The title and metadata live in separate Grid rows, never overlaid on each other. One line of title still reserves a two-line title region so metadata stays aligned across adjacent cards.

This creates a simple reading order: **image and reason → title → metadata**. It also means a long recommendation reason cannot push the last metadata line below the card. The tag is descriptive, not a new button; clicking it follows the same video action as the rest of the item.

Keep the image recognizable: one small reason pill, one existing cover label, no extra badges or decorative text over the picture. A long reason is ellipsized visually; its complete text remains available in the card's accessible description and information flyout.

### Base visual tokens

All sizes below are UWP effective pixels at 100% text size and are proposed values, not runtime measurements.

| Element | Proposed value | Rule |
| --- | --- | --- |
| Card surface | Theme card/background resource; radius 8; subtle border | Clear separation on acrylic/image backgrounds; high contrast uses system colors |
| Surface content inset | 12 on every edge, including any stroke | One owner for padding/stroke; no accumulated template/container offsets |
| Thumbnail | 160 × 90 in wide mode | Explicit 16:9 box, `UniformToFill`, existing placeholder and compressed image source |
| Image-to-text gap | 12 | A real grid column, not a negative margin |
| Title | 16, semibold; line-height target 22 | Wrap, maximum two lines, ellipsis; full title available without editing the source string |
| Title-to-metadata gap | 6 | Keep the title visually distinct |
| Each metadata line | 13; line-height target 18 | One line, no wrap, ellipsis within a finite width |
| Metadata gap | 2 | Both supplied lines remain readable |
| Reason pill | 11; line-height target 16; padding 6 horizontal / 2 vertical | One line, no wrap, ellipsis; maximum outer width `thumbnail width − 12` |
| Cover-label pill | 12; line-height target 16; padding 4 horizontal / 2 vertical | Preserve `cover_right_text_1` as supplied; do not assume every value is a parseable duration |
| Pill inset | 6 from the image edges | Top-left reason; bottom-right cover label; do not let the two regions overlap |
| Surface minimum height | 120 in horizontal base mode | Derived from the budget below, not a permanent fixed height at every text size |
| Inter-card spacing | 12 between surfaces | Proposed internal slot inset 6 on each side, with container Margin/Padding normalized to zero |

Use the normal UI font with CJK/emoji fallback. Left-align the title and metadata; do not justify short text. The 16px title is still the strongest text on the card, while allowing a useful two-line title in the current horizontal format. A persistent 20px title is an acceptable alternate preference only if the slot budget is recomputed; shrinking text progressively until it fits is not the solution.

For the reason pill, use a theme-aware backed surface with accent text/border and sufficient opacity to remain readable over any cover. The current model exposes day/night colors but the Hot template reads the day fields directly. If source colors are retained, select the matching theme variant and use a theme fallback for absent/unusable values. Preserve the supplied wording; do not prepend a redundant flame icon or another “热门” label (BR-31).

## 3. Make the height budget explicit

Define these metrics once for the current Hot layout/text size:

```text
P  = 12                         // total surface-edge-to-content inset
Lt = effective title line height, base target 22
Lm = effective metadata line height, base target 18
T  = 2 × Lt                     // reserve two title lines
M  = Lm + 2 + Lm                // both metadata rows
TextHeight = T + 6 + M

HorizontalSurfaceHeight = max(120, 2 × P + max(ThumbnailHeight, TextHeight))
StackedSurfaceHeight    = max(120, 2 × P + ThumbnailHeight + 12 + TextHeight)
GridSlotHeight          = SurfaceHeight + 2 × 6
```

At base sizing, `TextHeight = 44 + 6 + 18 + 2 + 18 = 88`. The wide thumbnail is 90px high, so the horizontal surface requires at least 114px. A proposed 120px surface leaves 6px of breathing room; the proposed slot is 132px including the internal 6px top/bottom spacing.

This distinguishes **surface height** from **grid slot height**. Do not set `ItemHeight=120`, leave the old container margins/padding untouched, and assume the budget matches. Normalize the item container to stretch its content with zero padding/margin; put the 6px outer inset inside the slot. During implementation, confirm how the existing Toolkit template applies `ItemHeight` and account for any remaining border/inset exactly once. No installed package/template inspection was performed for this plan.

The formulas use 12px as the total surface-edge-to-content inset, including its stroke. If a 1px `BorderThickness` participates in measurement, use 11px inner padding to retain that total, or explicitly add the stroke to both width and height budgets. Do not silently spend the same inset twice.

Give the title, metadata, and pill controls their own bounded rows. The title uses `MaxLines=2`; metadata and pill use `MaxLines=1` with `NoWrap`. `Auto` rows may grow with the effective metrics, but a descendant's `MinHeight` cannot override a fixed ancestor's item height. Bind the Hot grid's `ItemHeight` to the calculated slot height too.

The numerical line heights are targets. Use actual effective font/line measurements where fallback glyphs require more room; do not enforce a rigid line box that clips CJK accents, emoji, or enlarged text. The source has no existing Hot text-scale policy. First establish whether this app's XAML runtime automatically scales the explicit font sizes. If necessary, apply `UISettings.TextScaleFactor` through shared typography metrics, exactly once, and recalculate on its change event. Never double-scale the font.

For example, if the effective line heights become 33/27 at 150%, the text region becomes 128px and the horizontal surface becomes 152px; its slot becomes 164px. If a new line-height measurement is larger, the slot grows with that value. Image dimensions and padding need not scale with the text; the image/text mode rules below decide when to stack.

Compute mode and height once per grid width/text-scale change, and use the same metrics for all cards in that mode. Do not resize slots independently on every item's `SizeChanged`, or continuously change `ItemHeight` in response to the layout it just caused. Preserve the visible item anchor during a mode/height change.

## 4. Responsive layout rules

Let `W` be the **actual card surface width**, after the grid slot's outer inset. `DesiredWidth=560` is a preference, not a guaranteed minimum. Use the available cell width, including effects of the window, navigation, scrollbar, and chosen column count.

| Mode | Initial width rule | Image | Text width |
| --- | --- | --- | --- |
| Wide horizontal | `W >= 440` | 160 × 90 | `W − 24 − 160 − 12` |
| Compact horizontal | `360 <= W < 440` | 128 × 72 | `W − 24 − 128 − 12` |
| Stacked | `W < 360`, or the text/overlay conditions below fail | Full inner width at 16:9 | `W − 24`, beneath the image |

Also require a horizontal text lane at least `11 × effective title font size` wide (a starting readability threshold, not a character-count wrapping algorithm). If enlarged text makes the lane too narrow, select stacked mode even above the nominal breakpoint. Overlay pills must fit vertically inside the thumbnail with their insets and a gap; if their enlarged line boxes collide, stack and use the larger image box rather than shrinking the pill text.

Example wide grid slot: 560px slot width minus 12px outer inset gives `W=548`; its text lane is `548 − 24 − 160 − 12 = 352px`. Example compact surface: `W=380` gives a 216px text lane. A stacked `W=320` surface has 296px inner width, a 166.5px image, and about `24 + 166.5 + 12 + 88 = 290.5px` surface height at base metrics, rounded up; its slot is about 303px. This is deliberate reflow, not overflowing a 100px row.

Keep the adaptive multi-column grid at useful widths. All cells should agree on mode and slot height after the grid has chosen its column width. Establish a stable column/mode calculation from available width; avoid alternating between compact and stacked modes because one recalculation changes the scrollbar. Change the Hot page's layout only; do not globally enlarge every `MyAdaptiveGridView` in the app.

If two columns would leave both title lanes below the threshold, prefer one wider column. Taller text settings may therefore reduce the number of columns before increasing card height. The top shortcuts stay in the header and the load-more/footer behavior stays attached to the same scroll owner.

## 5. Data and interaction contract

The existing payload already has the fields needed for this redesign. No API migration or metadata scraping is required.

| Existing field | Presentation | Behavior when missing |
| --- | --- | --- |
| `cover` | Ratio-constrained thumbnail | Keep placeholder and reserved box; a late/broken image must not collapse the card |
| `title` | Main two-line title | Brief fallback such as “未命名视频”; retain raw data for navigation |
| `rcmd_reason_style.text` | Single-line reason pill on image | Normalize whitespace and collapse the **whole chip** when empty; no empty outline |
| `cover_right_text_1` | Bottom-right cover label | Collapse the whole label when empty; retain original value without parsing/reformatting it |
| `right_desc_1` / `right_desc_2` | First/second metadata rows | Render the supplied strings independently; do not invent counts or uploader links |
| `param`, `card_goto`, `uri` | Existing navigation/watch-later data | Preserve current action rules; the styling change does not redefine video IDs |

`right_desc_1` and `right_desc_2` are plain strings in [HotDataItemModel](src/BiliLite.UWP/Modules/Home/HotVM.cs#L134). They may contain author/view information, but this review did not verify current server payload meanings. Do not split those strings with punctuation rules or merge them into one ambiguous line just to save height. For consistency, reserve their two compact line positions within the shared slot budget, while collapsing empty controls visually.

Preserve these concrete paths:

- [HotVM](src/BiliLite.UWP/Modules/Home/HotVM.cs#L46) loads the current endpoint, keeps AV cards, and appends pages using the current cursor fields. Styling does not change filtering, ordering, or pagination.
- [Item click](src/BiliLite.UWP/Pages/Home/HotPage.xaml.cs#L45) opens `VideoDetailPage` with `data.param` for AV cards; the existing URI fallback remains.
- [Watch later](src/BiliLite.UWP/Pages/Home/HotPage.xaml.cs#L81) uses the same `param`. Keep right-click/long-press access and keyboard context-menu access. Do not put a nested button over the entire card that steals item activation.
- Full title, reason, and metadata should be available through a named card tooltip on hover/focus and the item's automation description. Add a short “卡片信息” context-menu entry if a touch-accessible full-text view is needed: a wrapping, selectable info flyout using the same data. The existing watch-later menu entry stays available.

Hover/focus may strengthen the surface border and reveal a secondary watch-later affordance. The card must remain usable without hover. Keep a visible keyboard focus outline around the whole item and ensure focus/Enter opens exactly one video. Pills are descriptive overlays and do not capture focus separately. Do not add autoplay previews, new network requests, or continuous hover animations to this change.

## 6. Why this design, and what not to copy

An independent reviewer proposed a conservative alternative: a roughly 136px horizontal card with a one-line reason row beneath the title. That can fix the base case if every text row and the slot share a bounded budget. The image-overlay design is preferred here because the reason no longer adds height to the title/metadata column, giving the title more room with fewer competing rows. It also gives the Hot card a recognizable visual hierarchy without adding another action.

Changing only the parent StackPanel into a Grid will not fix a 100px slot that is too short. Clipping the card hides useful content. Reducing every title to one line makes scanning worse. An unconstrained wrapping chip remains a problem even after increasing row height. Do not treat any of those as the complete fix.

The previously inspected sibling, [`ywmoyue/biliuwp-lite` at `ca2caddc597b3faf05b4dac6dcb226aa0694365b`](https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Pages/Home/HotPage.xaml#L18), uses `ItemHeight=120`, a 200px image column, a three-line title, and a bottom-aligned reason/metadata StackPanel. That is useful visual reference but not proof that this bug is fixed: its reason still wraps, and title/footer occupy the same text Grid without separate content rows. Borrow its explicit cover treatment if useful; do not copy a second constrained title/footer arrangement wholesale.

## 7. Implementation plan

Only this Markdown is part of the current change. A later implementation should proceed in small, reviewable steps:

1. **Record the reported case and freeze the behavior.** Add the Hot title-plus-tag issue to `bug_report.md` before application changes. Preserve one representative two-line title with a short reason and one with a long reason; synthetic fixtures are fine when no response capture is available. Do not claim they were captured from the live API.
2. **Restructure the Hot template.** In `Pages/Home/HotPage.xaml`, create a single surface, ratio-constrained image with two overlays, and a finite-width text Grid. Move the reason out of the title stack; collapse the chip by its text. Give metadata explicit one-line bounds. Retain the existing placeholder, click, flyout, and footer bindings.
3. **Own the metrics at page/card level.** Expose the chosen mode, effective typography, thumbnail dimensions, and calculated slot height from a small Hot-specific presentation helper or code-behind. Bind the parent grid height and child layout to that same state. A small reusable `HotVideoCard` control is reasonable if it simplifies the three modes; do not refactor every home card or embed layout calculations in the API model.
4. **Handle width and text settings together.** Recompute on actual available width and effective text-scale changes. Normalize container insets and preserve the visible video anchor. Confirm the current Toolkit's slot sizing during implementation before finalizing the binding; do not assume an inner Auto row overrides it.
5. **Add the visual finish.** Theme surfaces, backed pills, consistent corner treatment, keyboard focus, full-text disclosure, and accessible labels. Keep the copy short under BR-31. No new API fields or thumbnail-download system is required for the overflow fix.
6. **Accept the representative cases below.** Compare the same two-line title before/after in tagged and untagged cards, at the column-width and text-size boundaries. Ship the bounded layout before considering changes to recommendation/ranking cards elsewhere.

## 8. Future acceptance — not executed here

| Case | Expected result |
| --- | --- |
| Two-line title + one-line reason + both metadata values | Two visible title lines, reason visible on the image, both metadata lines inside the surface; no neighboring-card overlap |
| Two-line title + very long reason | One-line pill ellipsis, unchanged title/metadata geometry, full reason available through the full-text disclosure |
| Missing/null/empty/whitespace reason | No visible chip, empty border, or invisible reason-row spacing |
| One-line/long/unbroken title; CJK, Latin, emoji | Stable two-line title region, correct wrapping/ellipsis, no cropped glyphs; full raw title remains accessible |
| Long/missing metadata and cover label | Each field bounded independently, no wrapping into another row/overlay, no fabricated values |
| White/black/busy/missing/portrait cover | 16:9 box stays stable, normal crop/placeholder, pill text readable; loading does not move metadata |
| Surface widths around 360 and 440, and grid column transitions | Predictable compact/wide/stacked reflow from actual cell width; no layout oscillation or horizontal overflow |
| Windows text 100%, 150%, 200% | Typography scales once, grid slot grows or mode stacks, both image pills fit, title and metadata remain distinct |
| Light/dark/high contrast and background enabled | Visible focus/surface boundaries and readable pills/secondary text; no forced day-only palette |
| Pointer, Tab/Enter, context menu, touch long-press | One video opens per activation; watch later still uses `param`; full text can be inspected without relying only on hover |
| Resize/text-scale change after several loaded pages | Same video remains the scroll anchor; header/footer/load-more continue working; no per-card relayout loop |

The acceptance condition is simple: **two lines of title, a recommendation reason, and both metadata values remain readable within their assigned regions at every supported layout/text size.** The card may grow or reflow; useful content must not spill into the next card or disappear to preserve an arbitrary 100px height.
