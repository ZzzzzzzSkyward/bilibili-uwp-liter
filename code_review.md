# Review origin/work 33c254252f7fdf0c688756ac0424232776df3a94

Read the current `bug_report.md`. Two worthwhile issues in the new response-shape diagnostics; these are source findings, with no tests, builds, or application execution run for this review.

## 1. Later objects in an array are invisible to shape collection

`src/BiliLite.UWP/Services/ApiShape.cs:124-134` deduplicates array elements by `Describe(element)`, so every object after the first is skipped as another `object`. For example, `[{"emoji":{}},{"emoji":"text","extra":true}]` records only the first object's shape. Changing only the second object produces no drift. Dynamic cards and rich-text nodes vary within the same array, so the diagnostics can miss the item that breaks the mapper; reordering healthy cards can also look like a field change.

Suggested fix: inspect a bounded number of elements and merge their per-path types, keeping a stable set of observed types for each path. Bound elements/paths explicitly rather than treating all objects as one shape, and indicate when sampling was truncated.

## 2. The depth limit excludes the actual BR-17 emoji field

`src/BiliLite.UWP/Services/ApiShape.cs:98-100` stops walking at depth 8. In the real web-feed envelope, `$.data.items[].modules.module_dynamic.desc.rich_text_nodes[]` is already depth 8, so its `emoji` child at depth 9 is never recorded. Even when the affected card and rich-text node are the first sampled objects, an `emoji` change from object to string leaves no evidence in the shape or drift. The new probe uses a shortened `data.items[].emoji` path, which does not expose this gap.

Suggested fix: make the bounded traversal reach the real mapper's nested fields (including `emoji` and its children), or budget array levels separately from object nesting. Record truncation explicitly so a missing path cannot be mistaken for an absent server field. Use the full BR-17 envelope as the reference shape for the eventual probe update.
