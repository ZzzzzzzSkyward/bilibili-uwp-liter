# SR1 independent requirements review — PASS

Reviewer `short_review_review`, 2026-11-25 UTC. **PASS; FROZEN / RELEASED.** No P0/P1/P2 change is required for the bounded SR1 proposal. This approves requirements only. Root must still adopt SR1, bind exact current FI1/FD1 and accepted FI2/FI3 bytes, and give the same dynamic/API02 owner the exact additive grant before implementation. Fixed deadline remains **08:29:30 UTC / 16:29:30 Asia/Shanghai**.

## Scope and contract findings

Independently inspected the complete immutable SeasonDetailPage code/XAML, actual SeasonReviewVM/row models, RelayCommand, SeasonApi and ApiHelper.ApiModel. The page constructs the VM, assigns successful detail.media_id, calls GetItems on review pivot3 and binds the real LoadMoreCommand. GetItems requests ShortReview(MediaID, Next), publishes existing rows/content, reads data.next as text while Items.Count < total, and the real append/retry command reuses it. Refresh clears rows and resets Next. Utils.Request chooses no-cookie GET; actual HttpHelper.Get constructs new Uri(url). The fixture exercises the same final URI boundary without transport.

The single supported change is raw `next` → one escaped cursor value in the existing ShortReview interpolation operand. Preserve `string next=""`, explicit-null/empty cursor inclusion, media_id, sort/default0, ps20, configured host, GET/no-cookie, optional access_key appended in its existing position, and no app/WBI signing. Access-key encoding and every other method/enum remain outside scope. Preserve the complete legacy response, total/count branch, append/terminal/refresh, error/toast and retry behavior. Compatibility evidence does not impose a new fault-atomicity guarantee or authorize repairs to the unchanged VM/parser/publication logic.

Pinned S1's exact short-list route, nullable String next named cursor parameter, String response.next and actual controller reuse corroborate this field-specific string contract. S3 independently accepts str|None and passes next as cursor; its inherited JSON describes cursor as int, so this discrepancy cannot justify numeric conversion. S1 omission/cookie/web_location/count-fallback and modern write/action routes are family-specific observations. No S2-specific cursor authority, service liveness or production-character claim is adopted. Nine cited S1/S3 source files retain their released raw hashes.

## Independent private execution

The exact fixture and all nineteen project/source/seam files were copied byte-for-byte into `/workspace/work/index-dynamic-contracts/short-review-review/repro/`. Fourteen complete product files execute; the full actual HttpResults class and actual ToInt32 method independently equal their immutable extraction bodies. Stubs provide settings, platform delivery, logging/toast capture, timestamp and MD5 primitives only; they contain no cursor escaping, query decoding, VM/parser/builder or signing policy. The complete actual helper/signers remain product sources.

| Private check | Result |
| --- | --- |
| Unchanged 3446-check old-A fixture | **2958 PASS / 488 expected desired-safe failures**, exit1; output byte-identical to author replay |
| Active compatibility controls | **1572 PASS / 0 failures** |
| Final URI matrix | 19×2×2×3×3 = 684 requests ×5 checks; **486 expected failures** |
| Actual response→VM→append/retry→builder | Remaining **2 expected failures**; existing row/cursor/terminal/refresh controls pass |
| Current frozen FD1 API02 + private SeasonApi link | Build exit0, zero errors/two existing warnings; **12+1258+3831+270 = 5371 PASS**, run exit0 |

The matrix uses actual ApiModel.url, new Uri and exactly one standard query decode. It covers default/null/empty, ordinary and large/leading-zero numeric text, plus/percent-looking text, duplicate cursor/control attempts, fragment, JSON-shaped text, Unicode and controls; both hosts/login states and caller media/sort values. It checks unique cursor and controls, exact field set, optional access key and unchanged route/signing policy. The real VM flow receives synthetic responses and invokes actual commands, including failed append/retry; it does not assign Next or private readiness state to manufacture the scenario.

The private link changes only the current API02 project copy by adding the complete SeasonApi file. Current Program and FD1 checks remain exact. API01Stubs is unchanged; SeasonApi/SeasonIdType/ReviewType compile with no enum/type/stub change. Season1Stubs belongs to a separate project and remains untouched. Prospective implementation ownership remains exactly SeasonApi ShortReview operand, API02 project link, additive Program call and new SeasonShortReviewRequestChecks.cs, all under the same dynamic owner.

## Evidence, preservation and release

[Exact binding](review-evidence/binding.json), SHA256 `1acf9307c2686d95b5d552cbb4e3999424bacef0c9424bba3e4202f81115eeec`, records commands/cwd/environment, every compiled fixture/source and current FD1 closure hash, six tool/reference hashes, outputs/exits/counts and compiled artifacts. Raw logs/snapshots remain private. A proof-driver summary parser initially expected descriptive SEARCH/QUERY labels after the API02 command had already exited0; it was corrected and finalized from saved output, with no fixture edit or test rerun. This tooling correction is recorded in the binding.

Before/after hashes match for **150 named inputs**, including original118/supplement34/G4 fixtures, G4 references, author source/fixture/proof, cited sibling files and current frozen SeasonApi/FD1 closure. Those historical fixtures were preserved and not rerun. Moving FI2/FI3 SeasonIndexVM/SeasonIndexProbe bytes are excluded; no whole-tree stability is asserted. No source, test, main-document, Git/index/ref, shared cache/auth/dependency mutation, foreign/shared build, full suite or live request occurred. No repaired candidate, owning/full/native/hosted acceptance is claimed.

All review-owned commands ended and read/build custody is **released**. The later exact frozen selected union and its owning/fresh/full gates remain root's separate responsibility.
