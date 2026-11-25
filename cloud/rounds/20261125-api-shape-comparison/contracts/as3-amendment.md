# AS3 amendment — empty append and fallback precedence

Status: **READY FOR SAME REVIEWER RE-REVIEW**, 2026-11-25 UTC. The sole P1 in [review.md](../review.md) identified a contradiction: unconditional empty-page termination and exact absent/null count fallback cannot both hold for Page2/prior-one-row/count42. Root's [ACTIVE amendment dispatch](../requirements-revision-dispatch.md) decides conservatively to preserve absent/null and Type21 behavior, including empty appends. The amended [requirements](requirements.md) contain the complete16-row source/flag/row decision table and explicit publication/failure outcomes. AS1/AS2 text and all initial fixtures/logs/snapshots remain unchanged.

## Exact decision

Normal /x/v3/fav/resource/list with a present valid boolean is the only new empty-append policy: true+nonempty offers more/increments once; false with any row count and true+empty stop without Page increment. Normal absent/null and every Type21 /x/space/fav/season/list valid flag shape preserve the exact existing Videos.Count != FavoriteInfo.media_count rule after append, including an empty append. The existing successful first-empty short circuit remains for all flags/families: publish info/ownership fields, Nothing=true, keep pre-call Videos reference/content, no Page increment and no count decision. First-page missing info rejects publication. Existing failed append retains rows/info/Page with existing hidden-load-more/categorized-notice completion behavior. No new early return may bypass absent/null or Type21 fallback.

Concrete Page2/prior-one-row/count42/empty-page outcomes:

| Source | Flag | Required ShowLoadMore / Page / row count | Evidence classification |
| --- | --- | --- | --- |
| Normal | true | false /2 /1 | Supplementary desired-safe original red |
| Normal | false | false /2 /1 | Supplementary desired-safe original red |
| Normal | absent | true /3 /1 | Unchanged original-green compatibility |
| Normal | null | true /3 /1 | Unchanged original-green compatibility |
| Type21 | true | true /3 /1 | Unchanged original-green compatibility |
| Type21 | false | true /3 /1 | Unchanged original-green compatibility |
| Type21 | absent | true /3 /1 | Unchanged original-green compatibility |
| Type21 | null | true /3 /1 | Unchanged original-green compatibility |

Every row retains identical accepted row/info objects. Count-equality controls preserve false/Page2 for the COUNT rows. Nonempty controls preserve appended row order/IDs and the applicable ON/OFF/COUNT rule. This is explicit conservative compatibility, not a claim that an empty legacy page has been live-validated.

## Frozen supplemental actual-source controls

Both projects reuse the original favorite fixture's full absolute Compile links (actual VM/API/DTO/parser/mapper/envelope and complete byte-extracted HttpResults) with existing c2 delivery/platform primitives. Only the dummy ApiHelper base-url seam is seeded to https://api.bilibili.com so the real builder route can be asserted; Request is still offline. No product algorithm or copied paging implementation substitutes for production methods. SDK8.0.427/runtime8.0.33 and existing cached Newtonsoft13.0.3 are unchanged.

| Command / immutable fixture | Original-source outcome |
| --- | --- |
| bash /workspace/work/api-shape-comparison/contracts/as3-boundary-controls/run.sh; Program SHA-256 5fb14709db756735f06d2c7361d4fbc72bf0211ace818a14397ad2bd7e71f037 | **Exit0,124 checks PASS.** Normal absent/null ×empty/nonempty ×equal/unequal count; Type21 true/false/absent/null equivalents; both families' first-empty behavior across four flag shapes; first-page missing-info rejection. Assert exact Page/ShowLoadMore, rows/info identities, actual route/pn and encoded keyword, completion/toasts. Frozen log: as3-boundary-controls/result.log. |
| bash /workspace/work/api-shape-comparison/contracts/as3-present-empty-repro/run.sh; Program SHA-256 b813ecda508bb58251157eeaa0597e5734c490fbcbc5f48791e492149d291856 | **Exit1,2 failures/8 checks.** Normal present true/false empty append currently returns true/Page3; amended desired-safe result is false/Page2. Six route/encoding/retention/completion controls PASS. Frozen log: as3-present-empty-repro/original-red.log. |

Private /workspace/work/api-shape-comparison/contracts/as3-boundary-manifest.json binds all supplemental projects/Programs/scripts/linked source/stub/decoder inputs and result logs, plus unchanged initial files. The initial source-manifest14 hashes (12 sources +2 fixtures) and all12 baseline snapshots were reverified unchanged; initial query15/25 red and favorite6/9 red fixtures/logs were neither edited nor rerun/overwritten. The supplemental green-control harness's initial draft omitted seeding the base-url primitive, so only host assertions failed against the existing fixture.invalid seam; its draft log is preserved separately and corrected by configuring that seam, with every assertion retained. It was not a product failure.

## Closing acceptance and custody

The final additive c2 fixture must retain the original6/9 desired-safe cases, add both present-bool empty desired-safe cases, and preserve the124 original-green compatibility controls without changing their expected outcomes. All final assertions must run unchanged against byte-bound complete original and repaired sources. Existing failure/retry/ID/null-row/optional-payload/action coverage and AS1/AS2 source red→green remain mandatory. No live/native/session/code execution acceptance is inferred.

All amendment output/supplemental input/result read custody is **RELEASED** for the same independent reviewer. Original scratch fixtures/logs/snapshots and all sibling caches remain frozen. Repository writes are limited to this file and assigned contracts Markdown; no main source/test/tools/docs, index or ref changed. Product execution remains blocked pending revised independent PASS, memory safe integration and root baseline/file grants.
