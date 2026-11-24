# Remote verification and terminal credential observation

After storage's successful push/release, root read local HEAD and tracking origin/dev=e928bf0e818869c03485f8ef6f31fa47e98ac0f0. Its ordinary `git ls-remote origin refs/heads/dev` then failed exit128 with credential-read error; `gh auth status` identifies its GH_TOKEN as invalid and gh API returns HTTP401 Bad credentials. This establishes a terminal credential failure, not a repository permission/outage cause or source defect. No token value was printed, changed, copied or requested.

Root corrected its prematurely prepared remote/index claims in checkpoint/memory and paused the next publication window while checking. Independent later `git diff --cached --name-only` and exact storage-owned `git status --short` both succeeded with empty output; owned paths/index are clean. Source work and all prior pushes are preserved.

Authoritative alternate remote read via connected GitHub `fetch`:

`GET https://api.github.com/repos/ZzzzzzzSkyward/bilibili-uwp-liter/git/ref/heads/dev`

Tool returned isError=false and actual server `ref=refs/heads/dev`, `object.type=commit`, `object.sha=e928bf0e818869c03485f8ef6f31fa47e98ac0f0`, exactly equal to local/tracking SHA. Storage publication synchronization is therefore independently established by the live Git ref resource, while terminal transport authentication remains unverified. A read does not prove future push permission.

Cloud environment status reports current connected/running environment, unrestricted/enforced network, no declared GitHub outbound identity or secret binding; this does not establish an auth-refresh facility. Relevant Cloud runtime repositories/networking guidance read; existing edits preserved. No credential, auth header, network policy, repo/account permission/settings/identity/history change or environment restart was performed.

Recovery may continue and has the sole publication window subject to a normal git remote preflight **before committing**. If the source agent's configured git client also fails, write exact failure/blocker and retain source/test work; do not create unpushable commits, bypass settings or infer a need to rewrite history. Root can use connected GitHub reads for SHA/CI verification. Further publication recovery must use supported configured access, with no secret exposure or duplicate CI dispatch. Independent local verification/review can proceed if transport is blocked.

## Continuation observation and superseding grant state

At2026-11-25 02:28–02:30 Asia/Shanghai, connected Git-ref GET again returned actual servere928bf0e818869c03485f8ef6f31fa47e98ac0f0. Current source fixes remain unstaged/unpublished. review-fix-dispatch.md supersedes all earlier conditional publication windows; no current source actor may stage or publish yet. The connected Git Data transport protocol is now independently PASS after158 scratch checks, with no live write made; final source acceptance, separately reviewed exact manifest and explicit designated executor grant remain prerequisites.

Connected `GET /repos/ZzzzzzzSkyward/bilibili-uwp-liter/actions/runs` returned total_count1, the same manual run45434541348 on169b2b0a0205a14cad7d24558920250989720cc2, queued/null conclusion. Exact run GET agrees and `/actions/runs/45434541348/jobs` returns total_count0. No final-source hosted execution is established. A workflow-name/query collection URL was rejected by the tool's allowed-endpoint validator (HTTP400); the supported repository runs endpoint supplied the observation. This does not establish a repository outage, policy or write permission. No workflow was dispatched, rerun or reconfigured.

## Configured terminal read recovered after runtime reconnection

At2026-11-25 02:50:09 Asia/Shanghai, after the environment returned ready, root executed ordinary `git ls-remote origin refs/heads/dev` once against the changed runtime context. **Exit0**, actual remotee928bf0e818869c03485f8ef6f31fa47e98ac0f0, equal to current local/tracking. This supersedes the current-read blocker, while retaining the earlier real401/credential-read evidence as history. No root credential/configuration/header/permission change or manual environment restart was performed, and no cause for recovery is inferred.

Normal configured git publication is the preferred future route after all source gates and an exact designated-executor grant. No staging/commit/push has happened for the local recovery/FA-01 changes. A successful read does not establish a future write/protection result. The reviewed Git Data protocol is standby only and no API object/ref write is currently granted or needed.

## Verified ordinary source publication — current fact

The source gate/manifest passed and the designated executor's normal dev commit/nonforce push both succeeded. Current published source is **7fe7a5ff417d0bc2437b5dc3f0507543f744da9b**, sole parente928bf0 and root tree30b9e7d997e25e7a4b04c3c0dab1d9a0eabbf614. Root independently read local HEAD/dev/tracking and actual normal ls-remote: all exactly C. All915 committed/current input bytes matched executed0a80ac1c… with empty index. See recovery/publication.md and post-publication-observations.md for exact receipt, tests and protected/master proof.

Thus the earlier credential failure is historical, not a remaining source-publication blocker. No credential/configuration change or recovery cause is inferred. Git Data remained an independently reviewed standby and was **never executed against production**. Main-doc and cloud archive publication have separate exact windows; their eventual metadata-only tips do not change the immutable source binding. Current actual hosted collection still establishes no final-source result; native/session/platform gates remain unverified. No duplicate workflow dispatch/settings operation occurred.
