# AF2 companion folder publication — independent requirements review

Verdict: **CHANGES** — one P1 requirement error-boundary contradiction; no P0 or additional P2 finding. The MyFavorite notification defect and minimal property repair are supported. The proposed failure contract is broader than the unchanged legacy methods actually provide. No code grant follows this review.

I independently replayed the complete frozen AF2 proof: **11 positive controls PASS, exactly2 binding failures**, through the real VM/API/HttpResults and exact Task.Run DeserializeJson helper. Initial notification/default insertion, actual pending RefreshCommand, CollectFavorite and same-reference append controls are active. I also executed a new complete-source boundary probe: **7 current-behavior controls PASS**, demonstrating partial publication before later payload errors. Details and bindings are in [af2-review-evidence.md](af2-review-evidence.md).

## P1 — failure retention promises conflict with the required unchanged methods

**Location:** [contracts/af2-requirements.md](contracts/af2-requirements.md), “Actual commands, paging and error compatibility”: failed refresh leaves model/binding null; direct LoadFavorite failure keeps the existing created-folder reference; failed LoadCreateList retains existing rows. These unqualified statements conflict with the same document's instruction to preserve those methods byte-for-byte and add no transactional rollback.

**Actual boundary:** MyFollowVideoVM.LoadFavorite assigns MyFavorite at82, inserts the default at87, then reads has_more at88 and only later decodes CollectFavorite. A missing default payload or invalid later has_more can throw after the created-folder reference was already replaced. LoadCreateList adds rows at135 before reading has_more at137; malformed has_more can fail after rows were appended, while Page remains unchanged. Refresh's preceding null reset does not make either method atomic.

Concrete unchanged-source outcomes reproduced offline:

- Direct LoadFavorite with a valid created list but missing default detail replaces the old collection with `created-partial`, then reports an error; Page1/HasMore=false/Loading=false and the old collected collection remain. With an invalid has_more after valid default detail, the new created collection already contains `default-partial,created-partial` when the error is reported.
- LoadCreateList with a valid new row and invalid has_more retains the same collection **with the new row already added**, reports an error, leaves Page2 and HasMore=false. A successful same-Page retry follows existing append behavior, including another copy of that row. No rollback/dedup policy exists here.

A property-only identity setter will correctly notify any real assignment that occurs before such a late error. It cannot also guarantee the unconditional old-reference/null/unchanged-row outcomes without rewriting the forbidden loading/parser policy.

**Required correction before PASS:** Distinguish failures before publication/mutation from failures after one of the existing assignments/inserts/appends. Limit old-reference/null/row-retention promises to the applicable pre-publication cases. Explicitly preserve and record late partial-publication behavior as an existing out-of-scope limitation, or return a separately evidenced scope proposal to root; do not silently add rollback, parser validation, dedup or transactional loading to this property amendment. Keep the original13 proof assertions and all11 positives unchanged.

**Closing controls:** Add actual-source pre-publication transport/business/decode failures and late missing-default/invalid-has_more controls. Assert created/collected references and row order, notices, Loading/HasMore/Page and same-Page retry at the actual failure point. For Refresh, account for its prior null reset and for any later assignment that really occurred. Preserve these current-behavior controls unchanged across the property fix, while the original two binding failures and added identity/null/echo cases become green. Re-review the amended expected-outcome text before a root code grant.

## Accepted scope and retained requirements

The AF1-style reference contract is appropriate for MyFavorite: pure getter; old/incoming identity comparison before storage; exactly one inherited event for a real change; null/same-reference no-op; stored value visible inside callback; bounded same-reference echo; later distinct/equal-content references still observable. The two private flags occur only in this property and may be removed. CollectFavorite's separate unconditional-notification setter stays unchanged. No broad observable or neighboring VM change is justified.

Actual FavoritePage XAML/callers establish initial tab load, visible RefreshCommand and create/edit/delete completion refresh paths. They do not establish native dialog/render/reorder or live endpoint acceptance. Default-folder insertion, created/collected order, numbered append requests and sorting request order remain their current owners.

The proposed c2 support is minimal and justified: link the full MyFollowVideoVM (including its real FavoriteItemModel), reuse real IModules/RelayCommand/API/decoder, make only the existing Utils stub declaration partial, and extract the complete exact product DeserializeJson body into a generated partial wrapper. No handwritten response parser belongs in the stub. The final extractor must reject missing/duplicate/incomplete method boundaries and prove emitted body equality; private discovery extraction already matches the actual Task.Run method. No product helper, route, auth, envelope or parser change is authorized.

Final extended fixtures must use deterministic pending-response and actual async-void completion/fault observation with bounded guards/timeouts, not sleeps or model polling. Keep AS1–AS3/AF1 source/tests/proofs, original c2/action/paging assertions and c2+c3 affected checks. The later stable51logical/52executable combined gate and account/live/proto/native limitations remain unchanged.

All AF2 artifact/source/snapshot/fixture read custody is **RELEASED**. Reviewer scratch is frozen at `/workspace/work/api-shape-comparison/requirements-review/af2-review/`. Sole repository writes are this file and `af2-review-evidence.md`; no product/tests/tools/main docs/index/refs/cache or author/auditor proof bytes were changed. Return the P1 to the requirements author, then obtain independent re-review and root's exact same-owner grant.
