# AS1 + AS2 query execution — frozen and released

Status: **EXECUTED; READY FOR INDEPENDENT IMPLEMENTATION REVIEW**. Executor `api_query_executor`, gpt-6.1-sol/high, 2026-11-25 UTC. Read/write and same-project build custody is **RELEASED**. Exact [binding.json](evidence/binding.json) binds the runnable paths, final fixture, original discovery proof, commands/logs/reports, pinned tools and input manifests. No further writer or test process remains active in this scope.

## Result and exact change surface

Only three production expressions change: `UserDetailAPI.SubmitVideos` escapes `keyword ?? ""`; the existing `Followings` name-bearing branch escapes `keyword ?? ""`; `CommentApi.CommentV2` escapes the already serialized pagination JSON. Each value is encoded once before the unchanged real WBI parser/signature pipeline. The null-sensitive existing following branch, aliases, hosts/routes, query fields/order choices, headers/cookies, login/CSRF behavior and WBI five-character filter remain. U1 write methods are byte-unchanged.

The complete current UserDetailAPI and CommentApi files are newly linked into the existing API02 project alongside the complete current ApiHelper/WbiKeyService. `Program.cs` receives only one additive dispatch. New `ApiQueryShapeProbe.cs` owns the additional checks; no batch/project/package or inventory stage was added. These are the only five changed product/test paths, with exact hashes in binding.json. Shared API01Stubs needed **no change**; its released SHA256 `034c9d0362e8866b39f252bf0c0420901963b1a31b8d115eba0189d620ca568d` is retained. Original Program bytes are identical after removing the one new dispatch; SearchRequestChecks is byte-unchanged.

## Actual-source old red → repaired green

Private evidence root: `/workspace/work/api-shape-comparison/query-execution/`. The released original discovery `query-repro/{Program.cs,baseline.log,QueryRepro.csproj}` remains untouched and hash-bound. Its 15/25 failures are independently retained within the final fixture, rather than replacing or relabeling the old proof.

| Final fixture category | Checks | Original failures | Repaired failures |
| --- | ---: | ---: | ---: |
| AS1 original discovery cases | 16 | 10 | 0 |
| AS2 original discovery cases | 8 | 5 | 0 |
| AS1 expanded value/branch controls | 624 | 80 | 0 |
| AS2 expanded JSON/value controls | 256 | 56 | 0 |
| Signature/route/membership/auth/default controls | 2927 | 24 | 0 |
| Total added query fixture | 3831 | 175 | 0 |

Original membership-control failures are the expected effects of injected query syntax, not infrastructure errors. Both old and repaired runs compile and execute the full real builders/signer and preserve `PASS 12 original API02 assertions` and `SEARCH PASSED=1258 FAILED=0`. The original expected assertion exception exits 134; no compile-anchor failure is counted as red. Repaired exits 0.

The final fixture covers plain/empty/null, plus, ampersand, equals, literal percent escapes, Unicode/emoji, whitespace, quote/backslash/control characters and injected controls; every declared submit/following order; direct and alias submit; following tid -1/0/17 with the existing empty/null search distinction; configured and official hosts; comment hot/new and login false/true; exact JSON offset/null and serialization; chosen ps/mode/type/oid; unique signed membership and defaults. Independent MD5 recomputation binds each actual final canonical query and its generated wts using the dummy key pair's fixed mixin. The independent existing known digest vector is retained. Existing WBI `!'()*` sanitation remains explicitly expected. The harness throws on any attempted network delivery.

The first actual-repository old run and repaired affected-stage run have their own preserved runner reports. A separate private replay then establishes frozen before/after byte equality: `final-fixture-original-source/` versus `final-fixture-repaired-source/` contain identical final fixture, primitive stubs and dependencies, with only the two permitted complete product files different. Original query source hashes match the released baseline. `replay-results.json` records actual argv/cwd/exits; `original-run-inputs.json` and `final-run-inputs.json` bind Compile inputs and the selected regressions' runtime source reads. The comments generated Compile dependency is separately bound to the unchanged extractor and full actual HttpResults source. The 4728-file compiler/runtime/reference/package tool manifests are equal before/after replay; exact SDK8.0.427, runtime/ref8.0.33 and cached Newtonsoft13.0.3 were used without replacement or repinning. Current scoped inputs equal the final snapshot after all checks.

Two private preparation checks were corrected before the final replay: the preexisting-project inventory omitted the newly linked UserDetailAPI input, so its byte-identical released baseline was added; and the dotnet CLI forwards an assertion abort as 134 on this host, so the replay exit recorder accepts the observed status. Neither preparation failure is counted as behavioral red. The preserved discovery and actual-repository red reports were not altered.

## Commands and affected regressions

From `/workspace/bilibili-uwp-liter`:

```bash
bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:api02 --output /workspace/work/api-shape-comparison/query-execution/original-red-report
# runner exit1, owning child exit134: expected original behavioral red

bash plan/tests/run-all.sh --tools-root /workspace/work/maintenance-foundation/quality --only batch:api02 --only batch:comments --only batch:auth1 --only API01 --output /workspace/work/api-shape-comparison/query-execution/repaired-green-report
# runner exit0; all four selected child exits0, PARTIAL_PASS

python3 /workspace/work/api-shape-comparison/query-execution/replay.py
# exit0 after expected original exit134 and repaired exit0; manifests equal
```

The replay's two exact dotnet commands and isolated snapshot cwd are recorded in replay-results.json, with the same pinned tool/cache environment. Affected regression totals: API02 original12 + search1258 + added query3831; comments1089; auth1 35; API01 76. All repaired checks PASS. Selected verification is **PARTIAL_PASS**, `complete_required=false`; no full-suite or hosted-CI result is claimed.

## Ownership and remaining gates

Authorized dirty continuation was preserved. No foreign implementation/dependency/script was executed, and no authenticated/live/write Bilibili call occurred. No signer/transport/parser/account/shared-cache/other-owner source, main document, Git index/ref/commit/push or publication change was made. Root-owned round files and other owners' mutable bytes are excluded from this scope's stability claim. No commit exists for this grant.

Independent owning/fresh review and the later stable combined51logical/52executable gate remain with root scheduling. Native Windows/UWP build, XAML/device behavior, Bilibili live/cookie/session behavior and release startup remain unverified; account P4/P5 hold and U2 identity/token/host/auth/protocol/session gates remain. No new supported defect or scope conflict remains open within AS1/AS2.
