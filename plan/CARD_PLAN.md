# Dynamics card parsing and rendering plan

Keep the current source selection and pagination, but give every source the same small card-data contract and every card one shared body renderer. Fix lost rich content and broken sentence flow first; then make the author, pictures, previews, and reposts follow one responsive layout. Good typography cannot recover information that the mapper has already discarded.

This is an implementation plan, not a claim that these changes have shipped. The proposed sizes below are starting values in UWP effective pixels, to be tuned together on a device.

## Review scope and evidence

- Date: 2026-11-02, Asia/Shanghai.
- Main repository: `ZzzzzzzSkyward/bilibili-uwp-liter`, local `dev` tracking `origin/dev`. Fetched and reviewed remote tip: `b9b17b9848928ab14f55f70e39c75d94e40e86b9`.
- Last source review boundary recorded by a `code_review.md`-only review commit: `63e1765651eb1275f8731be5c45cf8f9427f0a6c`, marker `c6fcbf2a2c9e9506549aa4d75dd486862007e337`. The intervening commits change documentation/review reports only. The newer `b9b17b9` marker changes `plan/SETTINGS_PLAN.md`; it adds no source changes. Findings here describe existing behavior, not newly introduced regressions.
- Sibling reference: [`ywmoyue/biliuwp-lite`](https://github.com/ywmoyue/biliuwp-lite), the fork named under “其他fork” in this repository's README. No sibling checkout existed in this environment, so its default HEAD was fetched into an isolated reference checkout. Reviewed commit: `ca2caddc597b3faf05b4dac6dcb226aa0694365b`. The `dev` / `origin/dev` override applies to the main repository; the sibling is a pinned source reference and is not integrated or pushed.
- Applied `.agents/skills/work-code-review/SKILL.md`, overriding its branch and output defaults with `dev`, `origin/dev`, and `plan/CARD_PLAN.md`. Read `bug_report.md` and `code_review.md`; preserve both. Luna reviewers inspected data extraction, current UI, and sibling behavior, followed by source/caller cross-checks. No applicable `AGENTS.md` was found.
- Source and Git history only. No application, build, test, linter, formatter, package inspection, API calls, or live payload verification was run. Network use for the investigation was Git fetch only. Acceptance work listed below is future work.

All main-repository links below refer to the reviewed source. Sibling links include its full commit SHA so the comparison remains reproducible.

## 1. What the application does now

There are two different browsing surfaces, plus space/detail/repost callers. They should share data semantics without forcing the video-only surface to show every dynamic kind.

| Surface | Current path | Keep |
| --- | --- | --- |
| Home video dynamics | `Modules/Home/DynamicVM.cs` → gRPC/web/legacy adapters → `DynamicItemModel` → `Pages/Home/DynamicPage.xaml` | Compact video/season rows and their direct playback actions; the web home mapper intentionally accepts AV items only |
| General dynamics | `Modules/User/DynamicVM.cs` → `DynamicCardModel2024` for web/gRPC, or legacy conversion → `DynamicItemDisplayModel` → shared item shell and template selector | All/video/article/season tabs, current source selection and stale-request guards |
| User space | Same user VM, with independent `DynSpace` / legacy space pagination | User-specific request ownership and source-reset boundaries |
| Dynamic detail | Selector-chosen gRPC or legacy detail, then the alternate if needed → same display templates | Reuse of feed content rendering; expanded body is the proposed detail default |
| Repost list | `DynamicRepostVM` / `DynamicRepostMapper` → author/time/plain text rows | Lightweight list data; it need not instantiate the complete feed VM |

Useful starting points: [web feed request](src/BiliLite.UWP/Api/User/WebDynamicFeedApi.cs#L16), [web page parser](src/BiliLite.UWP/Parsing/Dynamic/WebDynamicFeedParser.cs#L27), [web card mapper](src/BiliLite.UWP/Parsing/Dynamic/WebDynamicFeedMapper.cs#L120), [gRPC card mapper](src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs#L210), [legacy parser](src/BiliLite.UWP/Parsing/Dynamic/LegacyDynamicFeedParser.cs#L35), [display conversion](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L1851), and [templates](src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml).

The current separation is useful but incomplete. `DynamicCardModel2024` lives inside the home VM source file and derives presentation strings, including XAML. The display model stores a `RichTextBlock`. As a result, node parsing, navigation, paragraph construction, and visual ownership are coupled across several files. Extend the current adapter boundary incrementally; a wholesale replacement of the feed machinery is unnecessary.

## 2. Fix these concrete problems first

| Priority | Trigger and visible effect | Source evidence | Practical change |
| --- | --- | --- | --- |
| P0 | A modern photo post has 2, 3, or 5+ valid images. Its plain description remains, but its rich links, mentions, and emoji are not hosted. Video/other OneRow posts also have no common body host when modern conversion turns it off. | [Modern conversion sets `ShowContent = false`](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L1919); [host ownership/visibility](src/BiliLite.UWP/Controls/Dynamic/DisplayModel.cs#L221); [Photo3x3 binds only plain text](src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml#L106); OneRow contains preview fields only at line 24 | Render the body once in the shell for every kind. Remove all template-level body hosts (Text, Repost, Reserve, and photo templates) and update the ownership/visibility properties in the same change. Until then, bind rich content into Photo3x3 and update ownership consistently; preserve the existing single-parent protection. |
| P0 | `hello ` + `@user` + ` world` arrives as three nodes. The trailing ordinary-text node opens another paragraph; the per-node parser also adds newlines. One sentence acquires artificial breaks. | [Description markup builder](src/BiliLite.UWP/Modules/Home/DynamicVM.cs#L905), especially lines 923–946; [per-node parser](src/BiliLite.UWP/Modules/Home/DynamicVM.cs#L1252) | Append ordered semantic inlines to one paragraph. Only source newlines/paragraph boundaries create breaks. |
| P0 | A long or link-heavy post exceeds the hardcoded 1000-character limit in generated XAML. Short links/emoji consume much more budget than visible text, and following nodes silently disappear. A single very large node is not meaningfully bounded by this check either. | [`maxlength` and post-append `str.Length` check](src/BiliLite.UWP/Modules/Home/DynamicVM.cs#L905) | Preserve full parsed text. Collapse by rendered lines with “展开”; expansion/detail must reveal every source segment. Do not truncate markup or cut a Unicode grapheme. |
| P0 | Nonempty plain fallback enters `RichTextControl.UpdateText`, creates a `TextBlock`, and returns before common styling. Long fallback prose lacks the same explicit wrapping, selection, size, and theme foreground as rich content. | [UpdateText](src/BiliLite.UWP/Controls/Dynamic/RichTextControl.xaml.cs#L64), [shared styling](src/BiliLite.UWP/Controls/Dynamic/RichTextControl.xaml.cs#L90) | Apply one body style after either branch, including recycled/cleared content. Prefer one renderer for both plain and rich segments. |
| P1 | WEB nodes display the complete destination URL and use an inline `HyperlinkButton`, which behaves as an embedded control rather than ordinary flowing text. Long destinations consume the line; profile links use `NavigateUri` instead of the app's existing profile command. | [Parser.At/Topic/Web](src/BiliLite.UWP/Modules/Home/DynamicVM.cs#L1211) | Use text `Hyperlink` inlines with readable labels and an explicit action target; dispatch through existing app navigation. |
| P1 | A gRPC item carries OPUS summary/title data, or a web/gRPC item has a reserve/vote/common attachment. Current adapters do not read those fields into the card. Some useful content becomes absent or unsupported. | [gRPC selects author/dynamic/stat/desc/fold only](src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs#L222); [web card construction](src/BiliLite.UWP/Parsing/Dynamic/WebDynamicFeedMapper.cs#L141); local proto already defines [OPUS summary](src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto#L4076) and `module_additional` | Read both `Extend.OpusSummary` and the separate OPUS-summary module when supplied, plus attachment modules already in the local schema. Normalize title/summary/covers and attachment summaries; preserve readable text and a destination when a specialized card is unsupported. |
| P1 | Unknown card kind is converted to `Other`. The modern early return carries an ID but no extracted author/body/actions; the template shows a generic error with no open-original action. | [Early unsupported return](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L1855); [Other template](src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml#L163) | Keep the supported fields from the outer card and show a compact unsupported-content block with “打开原文” when a real target exists. |
| P1 | Preview cover has an empty/bad URL, extreme aspect ratio, or a long subtitle. Its image has no explicit converter/placeholder/aspect box, and subtitle is not capped/wrapped. | [OneRow image and subtitle](src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml#L34) | Use an explicit `ImageSource`/existing converter, image placeholder/failure state, sized aspect box, and finite-width text column. String-to-ImageSource behavior is not proven broken by this review; the missing explicit sizing/fallback is source-visible. |
| P1 | A long author name, decoration, and header actions compete for width. The name is in an unbounded horizontal StackPanel. Repost profile and detail actions are nested buttons. | [Author/header layout](src/BiliLite.UWP/Controls/Dynamic/DynamicItemControl.xaml#L30), [nested repost actions](src/BiliLite.UWP/Controls/Dynamic/DynamicItemControl.xaml#L61) | Put a finite star-width name cell and independent action cells in a Grid. Make profile and detail buttons siblings; ellipsize the name and move optional decoration to overflow at narrow widths. |
| P1 | Deep repost chains recursively create display models and nested ListViews. Content width shrinks at every level. | [Recursive conversion](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L1898), [nested ListView](src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml#L171) | One compact embedded original; represent deeper ancestry as a short navigable row. Bound mapping depth too. The existing `IsRepost` visibility already hides the inner interaction row; retain that behavior. |

The image gallery's valid-URL filtering, stable index, original-URL list, and long-image dimension guard are already present. Preserve them rather than reporting those earlier fixes again: [picture conversion](src/BiliLite.UWP/Controls/Dynamic/DynamicParse.cs#L422), [image rules](src/BiliLite.UWP/Helpers/DynamicImageRules.cs#L13).

## 3. Parse responses into useful, explicit data

### Preserve the request/page boundary

Continue using `ApiEnvelope` / `ApiParseResult` to distinguish transport failure, invalid envelope, rejected business code, and bad payload. A successful empty page and an unknown card kind are separate cases. Do not make a new renderer responsible for selecting endpoints.

| Source | Existing data shape | Cursor / ownership rule |
| --- | --- | --- |
| Polymer web general feed | Cookie-authenticated `/x/polymer/web-dynamic/v1/feed/all`, `data.items[]`, `data.has_more`, `data.offset` | Keep offset opaque and source-specific; current callers use `all`, `video`, and a separate `article` branch with its own article offset |
| gRPC | `DynamicItem.Extend`, typed module list, typed dynamic payload | Keep history offset/update baseline/page state in that operation; do not reuse a web cursor |
| Legacy REST | `data.cards[]`, legacy `card`/`extend_json` JSON strings, profile/desc/display metadata | Decode each embedded JSON blob once in its adapter; preserve the endpoint's own cursor or space-history ID |
| Detail / space / repost list | Current dedicated gRPC/legacy requests and parser methods | These are separate operations. A web-parser comment mentioning `space` is not evidence of an implemented Polymer space/detail request |

The current VM already discards superseded results, resets pagination on source changes, and stops on nonadvancing cursors/empty-page limits ([Polymer publication](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L875), [space fallback](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L1185)). Preserve these behaviors. The [article branch](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L565) currently reads `GetData<JObject>` and page fields directly; route it through the shared web page parser too, while retaining its independent offset. Add ID-based deduplication on append so overlapping server pages do not repeat cards; calculate pagination progress from the server cursor and parsed page, not from how many unique visible cards survived filtering.

Keep the current request feature flags initially. The web API currently requests OPUS-related features and fixes `timezone_offset=-480` ([request builder](src/BiliLite.UWP/Api/User/WebDynamicFeedApi.cs#L16)). For later timezone support, derive the offset using the API's sign convention. Store the actual timestamp separately from server `pub_time`; neither relative copy nor a missing timestamp should become a fabricated publication date.

### One card-data contract

Introduce a small source-neutral data object between source adapters and the display model. The existing 2024 model can be evolved into this contract; creating a second parallel card system is unnecessary. Names below describe the intended contract, not already implemented classes.

```text
CardData
  Identity: DynamicId?, OriginalDynamicId?, SourceKind, RawCardType
  Author?: UserId?, Name, AvatarUrl?, PublishedAt?, PublishedLabel?, Badges
  Body: ordered Text / Mention / Topic / Link / Emoji / LineBreak segments
  Title?: semantic segments                    // OPUS/article title, distinct from body
  Primary?: ImageSet / Video / Season / Article / LinkPreview / Live / Music / Collection
  Attachments: ordered Reserve / Vote / CommonLink / Unsupported summaries
  Original?: CardData | UnavailableOriginal | AncestryLink
  Stats: optional counts, liked state, action availability
  Targets: Detail, User, CommentTarget?, MediaTarget?, SourceUri?
  Completeness: Full | Summary | Unsupported | Unavailable
  Warnings: source/type/field-path categories   // diagnostic metadata, no response body ownership
```

This layer contains strings, numbers, enums, lists, and typed targets. It owns no `JToken`, response wrapper, `RichTextBlock`, `Visibility`, or command. The UI layer injects existing commands and creates controls only for realized cards. A temporary adapter may continue producing `DynamicItemDisplayModel` while callers migrate.

| Useful information | Web fields / current extraction | gRPC / legacy equivalent | Normalization rule |
| --- | --- | --- | --- |
| Dynamic identity | `id_str`; `orig.id_str`; `basic.*` | `Extend.DynIdStr`, `OrigDynIdStr`; legacy `desc.dynamic_id[_str]` | Keep decimal IDs as strings across common data; parse numeric IDs only at a caller that requires them |
| Author | `modules.module_author.mid/name/face/pub_ts/pub_time/vip/official_verify/pendant` | `ModuleAuthor`; legacy `user_profile` / owner / origin-user | Absent author remains absent. Do not manufacture a user from a media/season ID. Verification/VIP are optional decoration |
| Body | `module_dynamic.desc.rich_text_nodes` and `.text` | `ModuleDesc.Desc/Text`; legacy content plus ctrl/emote metadata | Preserve node order and all visible text. If no useful nodes exist, use the plain text. Keep raw node kind for unsupported semantics |
| OPUS | `major.opus.title/summary/pics/jump_url` | `Extend.OpusSummary` / `ModuleOpusSummary` title, summary, covers; local `TextNode` schema | Prefer OPUS title+summary for OPUS content, ordinary desc as fallback; do not render both copies. Covers are not proof of a full article body |
| Pictures | `major.draw.items[].src/width/height`; `opus.pics[].url` with `src` fallback | `DynDraw.Items`; legacy pictures | Filter missing URLs once, then assign contiguous gallery indices. Keep original URL and dimensions separately from thumbnail URL |
| Video / season | Archive aid/bvid/title/cover/duration/jump URL and stats; PGC epid/season_id/aid | Typed archive/PGC payload; legacy video/season | Preserve aid, bvid, episode, season, duration, and supplied URI independently; choose native navigation by target type |
| Article / music / collection | Article covers/title/desc/link; music id/cover/title/label; web collection kinds currently unsupported by the mapper | Typed `DynArticle`, `DynMusic`, `DynMedialist`, `DynUgcSeason`; legacy corresponding card | Keep their real kinds even if they share a preview layout. Do not route an audio ID through an article action |
| Live | `major.live`; nested JSON `live_rcmd.content.live_play_info` | `DynLive` / `DynLiveRcmd`; legacy live share | Decode nested content once, preserve room ID, title, cover, state, area, supplied watching label. Missing state is unknown, not “ended” |
| Attachments | `module_dynamic.additional` is currently omitted from web mapping | `ModuleAdditional`; legacy `display.add_on_card_info` (reserve subset currently read) | Separate attachments from the primary card: a text/photo/video post may also carry a vote or reservation |
| Interactions | `module_stat` counts/status/forbidden; `basic.comment_id_str/comment_type/rid_str` | `ModuleStat`, derived comment object ID; legacy desc fields | Preserve availability and ID semantics; optional/unknown is different from a confirmed zero or false |

Source anchors: [web author/body/pictures/major mapping](src/BiliLite.UWP/Parsing/Dynamic/WebDynamicFeedMapper.cs#L179), [gRPC comment-ID selection](src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs#L266), [description mapping](src/BiliLite.UWP/Helpers/DynamicGrpcMapper.cs#L473), [local Description kinds](src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto#L1479), [OPUS TextNode kinds](src/BiliLite.gRPC/bilibili/app/dynamic/v2/dynamic.proto#L5050).

### Mapping rules that protect readable content

1. Read envelopes strictly at page level; tolerate malformed optional fields at card level. A malformed card becomes a readable fallback while neighboring cards remain. Record source, raw type, ID, and failed field path so a future payload can be diagnosed.
2. Select a primary payload using its explicit type plus a matching, usable object. The current web mapper picks the first nonnull object in a fixed order; an empty unrelated object must not hide a later useful payload. If type and payload disagree, preserve body/author and use a generic preview for available fields.
3. Keep text when an inline kind is unknown. Replace `WireScalars.MapDescriptionType`'s substring matching with explicit mappings for each supported kind. Media/business links can retain their supplied URI even before native support exists. Do not infer mentions merely because an arbitrary type name contains `at`.
4. Normalize known URL forms once: `//host/path` becomes HTTPS; a root-relative path needs a known source base; full app URIs remain distinct. Preserve readable label and destination separately. Do not concatenate `https:` with every leading slash. Missing destination means selectable text, not a dead button.
5. Preserve both a raw numeric count when available and a server display label such as `1.2万`. A compact label is rounded and should not be reverse-engineered into authoritative interaction state. Current `ParseCount`/`ToInt` defaults are compatibility behavior, not a good representation of unknown data.
6. Distinguish absent body from a summary. Feed summaries can offer “查看全文” via detail/source navigation. Expanding a local feed card must not claim it has downloaded text omitted by the API. This matters especially for OPUS titles and summaries.
7. Normalize originals with a bounded depth (proposed maximum two card-data levels plus an ancestry target). A missing/deleted/private original gets a specific unavailable block while the repost author's own text stays readable. Unsupported is not the same state as deleted.
8. Release response/JToken ownership after normalization. Keep optional per-card warnings rather than holding whole responses through the visible card model.

### IDs and actions must remain separate

`DynamicId` is for dynamic detail, like, repost, and delete. `UserId` opens a profile. Gallery index selects an image. Media IDs select playback/read/live targets. Comments need a paired object ID and API comment type; none of these targets is interchangeable with `aid` or `rid` in general.

There is a concrete compatibility issue to resolve during migration: modern conversion currently sets `ReplyID = basic.rid_str` and `ReplyType = basic.comment_type`, but page handlers derive comment type from the display kind and ignore `ReplyType` ([conversion](src/BiliLite.UWP/Modules/User/DynamicVM.cs#L1878), [home handler](src/BiliLite.UWP/Pages/Home/UserDynamicPage.xaml.cs#L100), [detail handler](src/BiliLite.UWP/Pages/User/DynamicDetailPage.xaml.cs#L30)). The gRPC adapter currently puts the same derived ID in both basic ID fields and a placeholder type `1`. Simply switching all callers to `ReplyType` would therefore be incorrect.

Normalize a real `CommentTarget(Oid, Type)` per adapter. For web, prefer the explicitly paired comment ID/type when usable; use a source/type-specific documented fallback only when that pair is absent. For gRPC/legacy, preserve the existing known mappings for video aid, photo object ID, article ID, song ID, and ordinary dynamic ID. OPUS visual classification as WORD/DRAW must not itself determine the comment namespace. Do not replace working action routing until the equivalent targets have been mapped.

Respect reported forbidden states in the action row. Unknown capabilities should not look like known successful actions. Keep outer repost actions attached to the outer dynamic; an original's profile/detail/media target must remain its own.

### Worked example: a two-picture post

This is a schematic example, not a captured server response. `D`, `U`, and `C` stand for distinct dynamic, user, and comment-object IDs.

```text
Input card:
  id_str = D; type = DYNAMIC_TYPE_DRAW
  module_author: mid = U, name = "摄影者", pub_time = "2小时前"
  desc.rich_text_nodes:
    TEXT("今天和 "), AT("@朋友", rid=U2), TEXT(" 去拍照。\n详情："),
    WEB("拍摄地点", jump_url="https://example.com/place")
  major.draw.items: [first original URL + dimensions, second original URL + dimensions]
  basic: comment_id_str = C, comment_type = 11

Normalized:
  DynamicId D; Author.UserId U; CommentTarget(C, 11)
  Body [Text, Mention(U2), Text, LineBreak, Text, Link(label, destination)]
  Primary ImageSet [Image(original1, index=0), Image(original2, index=1)]
```

The body displays as one sentence through the mention, then breaks only at the explicit newline. “拍摄地点” is the link label. Its destination is not substituted into the prose. At a 360px available viewport, 8px page gutters and 12px card padding give 320px inner width; two square tiles with a 6px gap are 157px each. Body text uses that same 320px width with the proposed 16/24 base metrics. At a wide viewport, the gallery stops growing at 480px while the reading column remains at most 800px.

Clicking the author opens U; clicking the mention opens U2; clicking picture two opens gallery index 1 with both original URLs; comments open `(C, 11)`; like/repost/detail use D. These relationships survive changes to grid layout, caption wrapping, and source adapter. The current two-picture template path loses the rich caption; this example is a concrete completion case for steps 1–3.

## 4. Render semantic text as text

Build `Paragraph`, `Run`, `Hyperlink`, `LineBreak`, and emoji containers in the UI layer. Do not rebuild paragraphs by concatenating XAML strings or apply a succession of global string replacements. Besides improving sentence flow, this keeps repeated labels, quotation marks, ampersands, and literal markup readable without a whole-post formatting fallback.

Proposed renderer rules:

- One initial paragraph; append each segment in order. A change from Text to Mention/Link/Emoji and back does not introduce whitespace. Normalize CRLF to LF once; actual LF creates `LineBreak`, including intentional blank lines. Preserve emoji sequences, variation selectors, and zero-width joiners.
- Ordinary prose uses `Run`. Mentions/topics/links use text `Hyperlink` inlines that can flow across a line; route activation through the same app navigation policy used by card actions. Avoid `InlineUIContainer` around every link. Reserve it for actual images.
- Link label order: source label → meaningful title → host/short path → destination text. Keep the full URL for copy/tooltip. Do not shorten ordinary words, mention names, or the action target. Do not fetch remote page metadata just to render a card.
- Prefer authoritative ordered nodes. Legacy ctrl offsets apply to the original text, before any escaping/replacement. Map nonoverlapping ranges in order; verify the offset convention against a fixture containing emoji before an @ mention. An invalid range degrades that segment to ordinary text. Regex recognition is a fallback for plain-text URLs/AV/BV/CV IDs, not a second pass over already-linked segments.
- Emoji use their supplied image only when usable; otherwise show `text`/`orig_text` as an ordinary run. Proposed size is 1.2 body em for normal emoji, capped at 1.5 em for known large variants. Treat wire `size` as a semantic variant, not an unbounded pixel multiplier. Align to the text baseline and ensure line spacing grows when needed.
- Full plain text must be retained for selection/copy and accessibility even if the visible body is collapsed. Empty body hides the body region; it must not insert “无内容” into an otherwise valid picture/video card.
- On recycled card content changes, clear old inlines, handlers, and expansion state before rendering the new identity. The same card data may create separate UI instances in feed/detail; never attach one `RichTextBlock` to two parents.

Suggested expansion policy: feed body initially six rendered lines; repost-origin body four; detail body expanded. Show “展开” / “收起” only when local content overflows. When only an API summary exists, use “查看全文”. Keep expansion state by dynamic ID and preserve the scroll anchor when height changes. A line cap is a visual policy, not a parser limit.

## 5. A consistent card composition

The current shell reserves an avatar column for the entire card. At narrow widths this wastes body width. Use a header row with avatar/name/actions, followed by full-width body/media/attachment rows. This also removes the need for separate caption hosts in every photo template.

```text
┌────────────────────────────────────────────┐
│ avatar  Author name                 more   │
│         time / optional source             │
│                                            │
│ Body text, @user and a readable link wrap   │
│ together as one sentence.                  │
│ [展开, only when needed]                    │
│                                            │
│ Pictures / media preview                   │
│ Optional attachment or compact original    │
│                                            │
│ share count     comment count   like count │
└────────────────────────────────────────────┘
```

Render order is stable: header → optional title → body → primary media → attachments → compact original (for a repost) → outer action row. Usually reposts have no separate primary media, because that media belongs to the original. Empty sections consume no padding. Repost-list rows remain deliberately compact and plain-text unless a separate product need justifies rich interactions there.

### Layout and typography tokens

Use shared resources so rich text, fallback text, previews, and originals cannot silently diverge. Use the app/system UI font with CJK and emoji fallback; avoid introducing a bundled font solely for cards.

| Token | Proposed base value | Behavior |
| --- | --- | --- |
| Reading column | Max width 800, matching current list/detail containers | Center in wide windows; stretch down to the available width; child text has no independent 800px width assumption |
| Page gutter | 12; 8 when available width is below 480 | Content stays within the viewport |
| Card inner padding / gap | 16; 12 below 480 / 12 between cards | One outer spacing owner; avoid adding padding repeatedly in nested templates |
| Card surface | Theme surface with subtle border/divider; radius 8 | Text stays readable over the optional background image; high contrast uses system resources |
| Body | 16 regular | Baseline line-height target 24 (1.5 em), minimum rather than a rigid box that clips larger glyphs/emoji |
| Author name | 15 semibold, one line | Finite-width cell, ellipsis, complete name available through tooltip/accessibility |
| Publication metadata | 12 regular | Secondary theme foreground; allow a second row under enlarged text |
| Preview / OPUS title | 18 semibold, target line height 26 | Preview max two lines; detail title can expand |
| Preview description | 14, target line height 21 | Max two lines in feed; hide when absent or duplicating the body |
| Action count / small badge | 13 / 12 | Counts remain legible; no duplicated icon-plus-label wording (BR-31) |
| Header avatar | 40 × 40; original 24 × 24 | Pendant is an overlay, not an extra invisible column that shrinks the whole body |
| Section / fine spacing | 12 / 4–8 | Use consistent gaps between header, body, media, attachments, and actions |
| Interactive hit area | At least 40 × 40 for standalone actions | Icon may be 16–18; target size need not equal icon size |

These are base logical sizes. The current card renderer has no explicit text-scale handling. Establish whether this app's XAML runtime already applies Windows text-size scaling to its explicit font sizes; if it does not, consume `UISettings.TextScaleFactor` and its change event through the shared typography resources exactly once. Avoid manually scaling text that the platform already scales. Derive emoji and line-spacing targets from the effective body metrics. Test 100%, 150%, and 200% text settings without fixed-height text rows. If a local card-font setting is later added, keep it separate from player/toast/danmaku sizing and avoid double application.

Use left-aligned text, finite-width Grid columns, and natural wrapping. Use `TextTrimming` for capped preview/name fields, not as the sole overflow policy for complete body text. Long unbroken URL labels must stay within the text column; retain a short link label or allow appropriate wrapping. Never calculate line count by character count: CJK, Latin text, emoji, and proportional fonts have different widths.

### Responsive behavior

- Base responsiveness on the card's actual available width, including space taken by side panes. Below 480, give body/media the full inner width, move optional decoration into overflow, and show a single overflow action in the header. Keep avatar/name/time visible.
- At 480 and above, a 160 × 90 media thumbnail may sit next to a flexible title/description column with a 12px gap. Below that, use a 96 × 54 thumbnail or stack a 16:9 preview above its text if the title would become unreadable. Audio/article thumbnails may use a square presentation when their content warrants it.
- Use three equal action columns instead of the current three 100px buttons. At large text scale, allow action counts to use a second row or shorten the count label without reducing the hit targets.
- Keep list mode as the reading default. Existing waterfall mode uses a `StaggeredPanel` with negative column spacing ([current panel](src/BiliLite.UWP/Pages/Home/UserDynamicPage.xaml#L11)); replace that compensation with positive, predictable gaps if the layout is changed. Add columns only when each has a useful minimum width (proposed 420). Do not change list-to-waterfall preference silently.
- The current list uses `ItemsStackPanel`; waterfall uses another panel. Verify realization behavior for each later; inheriting `ListView` alone does not prove every replacement panel virtualizes.

## 6. Pictures: composition without losing the original

Preserve original image URLs for viewer/save, and make thumbnail selection a presentation concern. Current 1/4/other routing and fixed 100/120px tiles work as a starting point but do not adapt cleanly to the card width.

| Valid image count | Proposed feed arrangement |
| --- | --- |
| 0 | No image region; text/other payload still renders |
| 1 | Natural aspect ratio, width at most `min(inner width, 480)` and never enlarge beyond useful source resolution; target maximum height 480. Long images show a top crop with “长图” and open the full original |
| 2 | Two equal squares in one row |
| 3 | Three squares when each can be at least 88px; otherwise two columns |
| 4 | Two-by-two squares |
| 5–9 | Three columns where they fit, otherwise two; six-pixel gaps and stable source order |
| More than 9 | First nine with `+N` overlay on the ninth; viewer still receives the complete original list |

For a grid of width `G`, gap `g=6`, and column count `c`, compute tile width as `(G - (c - 1) × g) / c`. Use a gallery maximum width of 480 and shrink to the available inner width; do not impose a tile minimum that forces horizontal overflow. If the card cannot fit even two useful tiles at enlarged text/narrow width, reduce columns.

Squares use `UniformToFill`; a single normal photo uses its aspect ratio without accidental stretching. Reserve the ratio box before the image finishes loading, so late downloads do not repeatedly move the feed. Unknown/zero dimensions use a neutral ratio and no false “长图” badge. Preserve the existing valid-dimension guard (`height > width × 2`). Broken images retain their box and offer retry/open-original, with a concise accessible label.

Request thumbnails appropriate to tile size and rasterization scale, with a modest cap; a 100px tile should not decode the entire original. Keep CDN transforms in `DynamicImageRules`/the image presentation helper, not API parsing. Avoid appending another transform to a URL that already has one, and only apply the transform to known supported image hosts. The current “加载原图” preference needs an explicit compatibility decision: retain its chosen download behavior initially, but still bound display/decode size where supported. Full-image viewing and saving always use the original URL.

All grid arrangements keep click-to-open at the correct filtered index, gallery next/previous order, and save context actions. The current four-image template lacks the save flyout present in the single/other templates; give all arrangements the same commands ([current photo templates](src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml#L78)).

## 7. Previews, reposts, users, and states

### Links and media previews

A preview is its own bounded content row, not a replacement for the author's prose. Use supplied title, description, cover, domain/type, duration/live state, and one primary action. Do not show a long raw URL as the title if a useful label exists. Ordinary inline links remain inline; create a separate preview only when the API supplies preview data.

Video/PGC use distinct playback targets, article/OPUS use read/detail targets, audio uses its audio URI, live uses its room target, and collections use their collection URI. Prefer a usable supplied target or a verified type-specific native route. Current `ParseOneRowInfo2024` reconstructs several routes, has placeholder live subtitles, and overloads IDs for web/music ([route construction](src/BiliLite.UWP/Controls/Dynamic/DynamicParse.cs#L252)); remove the visible placeholder strings when mapping real fields. Do not advertise an “ended” live state when the source did not supply one.

Reserve/vote/common attachments show a short title, one useful metadata line, and a supported open action. The current reserve CTA is commented out ([Reserve template](src/BiliLite.UWP/Controls/Dynamic/DynamicTemplate.xaml#L5)); restore a real actionable destination before styling it as a button. Do not invent reservation/vote mutations just to make the card appear complete. Unsupported attachments keep their label/text and a source link when available.

### Reposts

Show the reposter's author/time/prose as the outer card. Place one inset original below it with a quiet theme surface, 12px padding, small author header, four-line body, and its media/preview. Its header profile action and detail action are separate controls. Deeper ancestors become a single “查看原动态” row rather than another full nested ListView.

Missing original: show “原动态不可用” without deleting the reposter's body. Unknown original kind: preserve its available title/text and “打开原文”. Navigate only when there is a valid ID/URI. Keep only the outer action row, as the existing visibility rule already does; do not guess that the original's interaction counts belong to the repost.

### Users and publication context

Avatar/name open the same profile target. Missing avatar uses the existing placeholder; a missing user ID does not create a clickable profile. Preserve supplied verification/VIP meaning, but decoration may not consume the name's last usable width. Display relative publication text in the header and the real absolute time in tooltip/details when known. A synthesized season card without a user stays authorless and has no fabricated dynamic actions, consistent with the current CustomSeason handling.

### Empty, loading, unsupported, and failed content

An absent optional picture or author must not make a whole post disappear. Initial loading uses a stable placeholder structure; paging shows progress at the footer. Empty successful feed has a short empty state. Failed feed keeps retry available. An unknown card gets partial readable content; a removed original gets an unavailable-original block. Do not label all four situations “无法显示此动态,有待修复”.

User-visible labels stay short under BR-31. Use icon plus count or a text action label; do not duplicate the same meaning with both. Tooltips/accessibility names can explain the action without placing implementation details in the feed.

## 8. What to borrow from the sibling

The sibling is a useful reference for semantics and composition, not an automatically better replacement. Its current modern pipeline consumes V2 view models; this repository already has additional web/legacy selection, parsing failure classification, and request ownership that should survive.

| Sibling evidence, pinned to reviewed commit | Useful idea | Limit / adoption decision |
| --- | --- | --- |
| [V2 content and OPUS title/summary](https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/ViewModels/UserDynamic/DynamicV2ItemViewModel.cs#L228) | Read OPUS-specific content rather than assuming ordinary desc always exists | Main's local proto already has the relevant modules. Adapt them into the existing mapper; do not add the sibling VM framework |
| [TextNode @ and emote handling](https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Extensions/DynamicParseExtensions.cs#L178) | Preserve API-provided node semantics and explicit targets | It still replaces strings globally and inserts markup. Its ordinary Desc branch also passes OPUS-summary nodes rather than proving all Desc nodes are covered. Borrow fields/semantics, not the replacement algorithm |
| [Shared V2 shell](https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Controls/Dynamic/DynamicItemV2Control.xaml#L14) | Full-width content below the header, theme card background, 12px padding | Main already has a shared shell; adjust that shell and consolidate body ownership instead of creating a second one |
| [Gallery source list and indices](https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/ViewModels/UserDynamic/DynamicV2ItemViewModel.cs#L296) | Viewer input is independent of the cropped thumbnail | Already substantially present in main, with save actions worth preserving |
| [Original/missing-original model](https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/ViewModels/UserDynamic/DynamicV2ItemViewModel.cs#L139) | Explicit missing-original state | Main already supplies `MissingCard`; add bounded nesting rather than copying another recursive display hierarchy |
| [Wrapping renderer and plain fallback](https://github.com/ywmoyue/biliuwp-lite/blob/ca2caddc597b3faf05b4dac6dcb226aa0694365b/src/BiliLite.UWP/Extensions/DynamicParseExtensions.cs#L71) | A failed rich transformation should leave readable ordinary text | Its fixed line height, generated XAML, and global replacements share this repo's limitations. Its control-character stripping can also remove meaningful joiners; do not port it wholesale |

## 9. Memory and scrolling: address the card contribution separately

BR-21 reports growing memory but has no measurements. Retained card UI is a plausible contributor: feed collections append pages, the display model holds a `RichTextBlock`, and origins retain child display models. This report does not attribute native player memory or the whole-session growth to cards.

First stop keeping constructed visual trees in long-lived data. Realize the semantic body in the visible control, release image sources and event handlers on recycle/unload, and retain only card data/expansion state. Keep list-mode virtualization; assess waterfall mode independently. Moving UI objects out of models helps even before deciding how much history to keep.

Then measure one feed-only journey before adopting a page cap. If history retention is necessary to bound memory, use a rolling page window (starting proposal: ten response pages), preserve an anchor by dynamic ID, and retain the cursor/checkpoint needed to reload evicted pages. Only evict pages well outside the viewport; preserve a way back to earlier content. Do not merely remove the first N items and make the top of the feed inaccessible. Drop old response/card/image references on eviction and account for cached pages and image decode caches separately.

Future measurement should record loaded page/item count, realized card count, managed heap, working set, and image decode/cache state through 20+ pages, navigation away/back, and refresh. An unbounded source collection and a native playback leak are different hypotheses and need different journeys, as `bug_report.md` already requests.

## 10. Implementation sequence and ownership

Each step should leave a usable feed. No application source changes are included in this report commit.

| Step | Files / boundary | Deliverable and completion condition |
| --- | --- | --- |
| 1. Repair body ownership and plain fallback | `Controls/Dynamic/{DynamicItemControl,DynamicTemplate,RichTextControl,DisplayModel}`; modern conversion in `Modules/User/DynamicVM.cs` | Shell replaces every template-level body host, with ownership/visibility properties updated together; identical text/link/emoji content for 1/2/3/4/5/9 images; no double-parent UI ownership |
| 2. Replace text markup construction | Existing `Parser` / `DynamicCardModel2024_desc` in `Modules/Home/DynamicVM.cs`; UI rich-text control | Ordered semantic segments and object-based inlines; no node-created paragraphs/newlines or markup-length cap; plain copy/expand remains complete |
| 3. Fill source-data gaps | `Parsing/Dynamic/{WebDynamicFeedMapper,WireScalars}`, `Helpers/DynamicGrpcMapper`, legacy conversion | OPUS title/summary/covers, explicit node kinds, optional attachments, honest fallback and IDs; no new source-selection machinery |
| 4. Centralize card data and targets | Evolve/move the 2024 model into `Models/Dynamic`; adapter/display boundary; home/user/detail handlers | Data contains no UI/response objects; paired comment targets and typed media routes agree across sources; lightweight home/repost projections stay supported |
| 5. Apply layout tokens and responsive media | Shared shell/templates, image presentation helper, page containers | Finite wrapping width, consistent padding/type scale, responsive grid/previews, independent author actions, accessible theme states |
| 6. Bound originals and assess retained history | Original-card mapping/template; feed lifetime/recycling | One inset original, navigable deeper ancestry, image/UI cleanup; measured decision on page-window retention |

Avoid mass-moving every model or copying sibling dependencies while repairing the four P0 issues. A small intermediate contract plus existing compatibility projections is sufficient. Register any future new source files in the UWP project when implementation begins; that is not part of this documentation-only pass.

## 11. Future acceptance matrix — not executed in this review

Start from captured, representative payloads already available to the maintainer or purpose-built minimal source fixtures. Static source analysis cannot confirm which fields the current service sends. These cases define the later parser/rendering work, not extra validation performed for this report.

| Area | Representative inputs / journey | Expected result |
| --- | --- | --- |
| Envelopes/page state | Valid empty page, missing/null items, wrong items type, rejected code, repeated/absent cursor, overlapping pages, refresh during load | Empty and failure stay distinct; no duplicate cards; no repeated first page or stale publication; source cursors remain isolated |
| Mixed text | `hello @user world`, topic/link between text nodes, repeated identical mention labels, literal `<>&"'`, CJK/Latin punctuation, explicit blank lines | Same source order and intended breaks; inline targets correct; no unexplained new paragraphs or lost trailing text |
| Text limits | Long plain post and equally long visible post with many links/emoji | Comparable six-line collapse; all local text available after expansion/copy; server summary offers full-content navigation |
| Legacy ranges | Emoji before @ mention, repeated text, overlapping/out-of-range ctrl fields | Correct range or a localized plain-text fallback; neighboring prose remains intact |
| OPUS | Web title+summary+pics, summary without desc, gRPC Extend/module summary, no pictures, article-shaped OPUS | Title/body shown once; no fabricated article ID or comment type; incomplete summary marked by an appropriate full-content action |
| Pictures | 0/1/2/3/4/5/9/12 images; invalid URL among valid images; portrait/landscape/long/unknown dimensions; image download failure | All captions retain rich content; layout stays inside width; stable original gallery order/index; save works in every arrangement |
| Actions | Video aid/bvid, PGC episode vs season, ordinary dynamic, photo object, article/music, collection, parent/original repost, forbidden actions | Correct typed destination/comment pair; outer and original IDs never swapped; unavailable actions do not behave as working buttons |
| Width / scale | 320, 360, 480, 800, 1200 effective-pixel viewports; list and waterfall; text 100/150/200% | No horizontal overflow or clipping; no header collisions; text remains readable; target hit areas remain usable |
| Themes / input | Light, dark, high contrast, optional background; pointer, Tab/Enter/Space; selection/copy | Readable theme surfaces; profile/detail/media actions independent; links named meaningfully; body selection preserved |
| Reposts / unknown | Three-level chain, missing/private original, unsupported kind with useful text/URI, authorless synthesized season | One bounded embedded original; remaining ancestry navigable; outer content preserved; honest partial/unavailable states |
| Feed lifetime | Feed-only 20+ page scroll, expand/collapse, resize, navigate away/back, refresh | Scroll anchor stable; old controls/images released when expected; retained data/UI measured separately from playback/native resources |

Before marking an implementation accepted, compare the same representative cards side by side in feed, user space, and detail, then check the deliberately compact home-video and repost-list projections. The primary success criterion is that useful source content survives parsing and is readable at the width the user actually has.
