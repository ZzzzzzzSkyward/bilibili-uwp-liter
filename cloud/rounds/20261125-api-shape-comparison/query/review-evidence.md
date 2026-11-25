# Query implementation review evidence

Status: **PASS; FROZEN/RELEASED**. Owner `api_query_review`, independent AS1+AS2 reviewer, 2026-11-25 UTC. Sole repository writes: this file and [final-review.md](final-review.md). Private outputs: `/workspace/work/api-shape-comparison/query-review/`. No new behavioral test/build was run; exact released owning receipts were reused after independent inspection and hash verification, as required by the implementation-review dispatch for unchanged inputs.

## Authority and exact inputs

Read AGENTS.md, cloud/CONTRACT.md, REPO.md and memory/INDEX.md; the accepted requirements binding, initial AS1/AS2 acceptance, final AS3 PASS, scheduling PASS/adoption, exact execution grant and latest query ACTIVE activation. Requirements SHA256 remains `a097e905f6193e24014a11e2769dcc23156485ea078e01de6d93ea5d2c778aa2`; AS3 PASS `02e419f61f8d5334a97f11bd957eac9d4029a5377f33eb52b8fb0e336e20a2e4`; scheduling PASS `66ffb52f634a819220ffc2f53895ea5e2419bb2073f236c3d03fa9befce5a69f`. Query owner was frozen/released before review. Disjoint memory work is excluded from this scope's stability claim.

The independent validation command, from `/workspace/bilibili-uwp-liter`, exited **0**:

```bash
python3 /workspace/work/api-shape-comparison/query-review/verify_receipts.py
```

It hashes actual file bytes and inspects XML Compile entries, project-local implicit sources, API01 csc arguments, the complete HttpResults extraction output, final-fixture snapshots, runner metadata and full log endings. Output: `PASS_STATIC_AND_EXACT_RECEIPT_VALIDATION`, artifacts23, live inputs54, tool files4728, only old/new differences the two authorized builder files, restored Program SHA256 `ddbee124f47508e08cbb902e4cc15860e2a27d16efc8b01ea76e5bf3b1eb1225`, new test runs0. Detailed actual paths/hashes/commands/categories are in `receipt-validation.json`; exact production diff is `production.diff`; before/after scope manifests and final artifact hashes are bound by `review-release.json`. A private preparation assertion initially compared Path-component ordering with string ordering; normalizing list ordering passed with identical4,728-file sets and hashes. No product/test change or behavioral outcome was involved.

| Frozen input | Verified SHA256 |
| --- | --- |
| query/execution.md | b381b8cb4dba3f96e388b274a8a0835912066acd4f52ccafae7bae070c1d85c7 |
| query/evidence/binding.json | 0560ec7c04f585f5a413b7fd840b3aa237878a94c2168ec2ca735978e125c483 |
| UserDetailAPI.cs, repaired | 2c263ecaaf65d29e62fb3bd313b37c78c52496afcf872a6daf6471eb8f557225 |
| CommentApi.cs, repaired | 1578461fe31d2b50f5106280ebed24c57f5adb4448073db7cec9ed33d908bdb2 |
| api02/API02.csproj | b15672b40f0177627f4d4b004c8ea1476750af0d10202651b69d2e572578baca |
| api02/Program.cs | f6fe17ac00ee5b880036d3042c7651521bce98d983c372899fb61625f86034bd |
| api02/ApiQueryShapeProbe.cs | 28abac1738b3e3f0051691deda71c2bf1425270c1800b0ea9f7e1f460c58c18a |
| API01Stubs.cs, unchanged | 034c9d0362e8866b39f252bf0c0420901963b1a31b8d115eba0189d620ca568d |
| SearchRequestChecks.cs, unchanged | fe366983030cb255dca05b9dfdf61c029dd687d44260a56118e16ab4a2864584 |
| final-run-inputs.json, 54 entries | 14302d21743e242785caceff960a55890e5f33d7c746e0c0c0032c556276b795 |

All five granted product/test files are regular non-executable files; the tracked four retain Git100644 with no mode diff, and the untracked new fixture is included in every final hash binding. No extra API01 primitive was introduced. The old source snapshots are exactly UserDetailAPI SHA256 `4fe1fc0287a81682b064f72ee402b49d0036bb56f6898240e2a49ba73db39158` and CommentApi `e05ddc67608afb206e285416f1f984bf4c5fb44a2edf17fe204e27dfca713d76`. Final old/new snapshots have identical fixtures, stubs, project and dependencies; only those two complete product files differ. The independent restored-Program hash proves every original assertion remains unchanged, rather than relying on a count or diff label.

## Actual implementation and test semantics

Inspected complete current UserDetailAPI, CommentApi, ApiHelper and WbiKeyService; the new complete fixture and original Program/SearchRequestChecks/API01Stubs; actual UserInfoPage XAML/code, UserSubmitVideoVM, UserFollowVM, CommentControl, comment parser/mapper; actual Utils.Request/ToMD5, ApiModel.url, HttpHelper Get/GetWithWebCookie and CookieService CSRF selection. The additional static caller/transport inputs are separately hashed in `receipt-validation.json` and the scoped before/after manifest.

The only production differences are raw `keyword`/`name` → `Uri.EscapeDataString(keyword ?? "")`, and serialized `paginationStr` → `Uri.EscapeDataString(paginationStr)`. Branch conditions, JSON serializer, route selection, aliases and metadata are untouched. Real WBI signing parses once, strips its established five characters, sorts ordinally, escapes decoded values, signs the exact canonical query and appends w_rid. Real UTF-8 MD5 is represented by the unchanged primitive equivalent in API01Stubs; no signer/builder is stubbed. ApiModel forms `baseUrl + "?" + parameter`; Utils passes that URL to cookie/no-cookie HttpHelper delivery, which constructs a Uri without query-value re-encoding.

The fixture checks actual decoded raw values, one occurrence per field, exact allowed membership and id/page/size/order controls, so a final digest over corrupted or injected fields cannot conceal corruption. It independently hashes the actual final sorted canonical text at the generated wts using fixed mixin `ea1db124af3c7062474693fa704f4ff8`, separately retains vector `6149fdadf571698ca7e6a567265cd0ee`, and tests the accepted sanitizer. Offset checks parse the final one-field JSON, require null/string type and exact offset, and compare the complete serializer output subject to that sanitizer. This detects double encoding as well as raw-query splitting/plus/percent decoding. Calls attempting actual delivery throw through the test hook.

Coverage includes declared enum orders, direct/alias submit, following tid-1/0/17 and null-versus-empty branch behavior; official/configured hosts; plain/null/empty/plus/ampersand/equals/percent/literal%2B/Unicode/emoji/whitespace/quote/backslash/control/injection text; comment hot/new, login true/false, exact optional CSRF, JSON-like offset text, chosen oid/type/ps and defaults. The existing global search's app-order/BUVID/auth policies and error/unknown-shape/identity/cursor boundaries remain in the unchanged regressions. Comment parser carries next_offset text, mapper keeps source ownership, and CommentControl advances only accepted published cursor pages; no caller-side pre-encoding or alternate cursor policy was added. U1 write bodies and caller message-encoding ownership are byte-unchanged.

API02 has15 actual Compile inputs, comments19, auth1 10 and API01's script15, all accounted for. Comments'19 include the generated complete actual HttpResults body; the independent reconstruction from unchanged HttpHelper and run.sh is byte-equal SHA256 `8aa4c8734aed85517df1dc3863a479fc076a262e775a7c657e45e9f8b9bc6a93`. Runtime source reads in comments and auth1 (including CommentControl/CommentApi, playurl, settings/App and ApiHelper/root markers) are covered by the54-entry manifest. The comments request seam deliberately rejects signing; it is a write/dialog/parser compatibility regression, while the new API02 fixture owns real CommentV2 signing, avoiding any masked read result.

## Same-fixture and affected receipts

All paths below are under `/workspace/work/api-shape-comparison/query-execution/`. Both full fixture snapshots use the same final fixture SHA above and the same API01 primitive/dependency bytes. SDK8.0.427, runtime/ref8.0.33 and cached Newtonsoft13.0.3 are fixed. Independent verification hashes all4,728 current compiler/runtime/reference/package files against the byte-identical before/after manifests; no file is missing or added to those fixed tool sets.

| Final query category | Checks | Original failures | Repaired failures |
| --- | ---: | ---: | ---: |
| AS1-original | 16 | 10 | 0 |
| AS2-original | 8 | 5 | 0 |
| AS1 expanded | 624 | 80 | 0 |
| AS2 expanded | 256 | 56 | 0 |
| Signature/route/membership/auth/default controls | 2927 | 24 | 0 |
| Total | 3831 | 175 | 0 |

`replay-results.json` records actual dotnet argv and isolated cwd for `final-fixture-original-source/plan/tests/api02/API02.csproj` and `final-fixture-repaired-source/plan/tests/api02/API02.csproj`, both `-c Debug`, with observed exits134/0. `replay-original.log` SHA256 `256395c33227e06838f5f53c70f14fe2f210a8c3d7ba9958bc71b5c8e649403b` and `replay-repaired.log` `4d697c4d0fec112d9f6c8c72b8409ab07d83aa43595588ea2275d827f15a102f` contain the complete totals and original12/search1258 PASS. The175 red checks execute before the expected `ApiQueryShapeProbe.RunAsync` assertion exception. All24 control failures are exact-membership failures for injected-looking raw text; they are behavioral red. The preserved discovery Program/project/baseline log are hash-verified separately, with the same original four text/four cursor cases and known vector; the original15/25 proof was not overwritten or relabeled.

The actual-repository runner command was `bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:api02 --output .../original-red-report` (runner1, child134). Its bound log reproduces175/3831. The repaired command adds `--only batch:comments --only batch:auth1 --only API01`, uses `.../repaired-green-report`, and exits0 with all four selected children0. Repaired API02 passes12+1258+3831; comments1089; auth1 35; API01 76. Results and logs are untruncated, `PARTIAL_PASS`, `complete_required=false`, four selected logical/executable stages. No complete-suite/hosted result is inferred.

## Release and limits

The final `review-release.json` binds both reviewer Markdown outputs, private verification script/receipt/diff and before/after scope manifests. All54 owning runnable inputs plus five additional static caller files retain exact bytes at review close, including the untracked fixture. Frozen execution/release/binding, original proofs, snapshots and tool hashes remain unchanged. This is scoped custody, not a moving whole-tree claim; disjoint owners' mutable work is excluded. No new test/build process exists, so there was no concurrent same-project build. Read/write custody is **RELEASED to root** after the final check.

Native Windows/UWP/XAML/device, live Bilibili/cookie/session and release startup remain unverified; account P4/P5 hold and U2 gates remain. No source/test/tooling/main-doc/index/ref/cache or foreign/live/auth/write mutation occurred. Combined stable51logical/52executable verification, independent fresh-audit closure and exact publication remain root-scheduled subsequent gates.
