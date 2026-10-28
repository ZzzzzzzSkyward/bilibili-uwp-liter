# Review — `22a9814`

Read the current `bug_report.md` and reviewed the recent changes since the previous review (`4ed90e6`), with two GPT-6-luna reviewers. Source inspection only: no builds, tests, format checks, API calls, or runtime validation. Application code is unchanged. Build/device evidence in the bug report is the author's evidence, not this review's.

## 1. BR-13: clear the other owner's Content, not just its Visibility

`src/BiliLite.UWP/Controls/Dynamic/DynamicItemControl.xaml:80` still binds the generic presenter's `Content` to `Model.Content` for Text/Repost/Reserve cards. The new getter in `Controls/Dynamic/DisplayModel.cs:231-237` only collapses that presenter. Collapsing a control does not clear its Content or detach an already parented UIElement. The templates still hand the same RichTextBlock to RichTextControl (`DynamicTemplate.xaml:9,74,175`), whose callback inserts it directly into `Parent.Children` (`RichTextControl.xaml.cs:49-60,86-88`). The ownership conflict described in the report therefore remains possible; hiding one consumer does not establish a single owner.

For a REST-fallback text/repost card with a nonempty body, both consumers still receive the same instance. Treat BR-13 as unresolved at source level. Give the generic presenter a separate content getter that returns null whenever the template renders the body, or conditionally create only the owning presenter. Keep the visibility getter for spacing if useful. No cloning or broader model rewrite is needed for this fix.

## 2. BR-13: one/four-image gRPC cards now lose their rich body

`src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml:82,139` removes the Content binding from Photo1x1 and Photo2x2, leaving only plain Desc/Subtitle. For 2024/gRPC cards, `DynamicParse.ParseOneRowInfo2024` still builds `data.Content = Render(item.richtext)` (`DynamicParse.cs:219-230`), but `ConvertToDisplay2024` sets `ShowContent=false` (`Modules/User/DynamicVM.cs:1303`). The generic presenter therefore does not display that rich body. After this change, one-image and four-image posts lose rich hyperlinks/emotes and render only the plain description. The report's claim that their body moves to the generic presenter applies to the legacy path, not both converter paths.

Keep one rich-text owner for both converters. A small solution is to restore the photo templates' Content bindings and make the generic content getter return null for the actual photo layouts that render Content themselves. Apply the same ownership choice consistently to outer and reposted cards. Prefer keeping the existing rich rendering; do not clone the blocks just to relocate them. Photo3x3 is a separate existing layout and does not need a redesign for this regression.

## 3. BR-05/11: an empty first REST page overrides valid pagination

`src/BiliLite.UWP/Modules/User/DynamicVM.cs:650-654` calls `PublishFeedError` whenever REST page 1 maps to zero cards. This happens even when the response has `has_more=true` and an advancing offset, so `CanContinuePaging` has just returned true at line 645. `PublishFeedError` immediately sets `CanLoadMore=false` (`:866-874`), preventing the short-viewport completion check from requesting page 2.

An empty cards array, or a page whose entries all lack desc and are skipped at lines 616-622, can therefore show a failure and strand later available cards. Continue paging while the cursor advances and the existing empty-page cap allows it. Only publish the empty-feed failure once pagination stops and the accumulated list is still empty; do not limit that decision to page 1.

## Report decisions

- **BR-11:** keep the REST fallback as practical recovery for the reported gRPC rejection, with the empty-page fix above. A real-buvid metadata change can be a separate follow-up; the report does not establish that it fixes the rejection, so it should not replace the working fallback on that assumption.
- **BR-13:** preserve rich body rendering and fix actual ownership. The issue is which control receives Content, not whether the body sits a few pixels above or inside the photo card.
- **BR-14:** no substantive issue found in the encoding change. No further style or structural cleanup requested.

No runtime/device behavior is claimed verified.
