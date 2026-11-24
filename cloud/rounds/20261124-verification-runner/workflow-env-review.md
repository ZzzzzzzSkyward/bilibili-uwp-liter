# 审查：hosted workflow environment correction

- 轮次：`20261124-verification-runner`
- 审查类型：focused amendment / requirements
- 审查员：`logic_review`
- 结论：**PASS — exact environment relocation only; one corrected-source dispatch remains gated on independent core P1 closure.**
- Controlling input：`workflow-env-correction.md`, actual 422 receipt in `hosted-validation-execution.md`, and the separate source P1/fix dispatch. This approval does not clear that source P1, grant CI PASS or authorize an unchanged-config retry.

## 审查范围与证据

Read the correction proposal, original approved QF2 requirements/review, existing hosted amendment/review/dispatch, exact current workflow and hosted execution receipt, current implementation `final-review.md` and `review-fix-dispatch.md`. The current workflow hash is `d05b81e7011de2f33fc0f02dfc11f2a02743c563e34f59f7e3d55a0fcf3dd491`, matching the single-trigger publication receipt. It still has the seven rejected runner.temp references in job env, literal telemetry flags and the already approved no-input manual trigger.

The **one** POST at 2026-11-24 15:57:49–15:57:51 UTC returned **HTTP 422**, explicitly identifying `Unrecognized named-value: 'runner'` in seven job-level env expressions, lines 19–25. It created no observable run/job/artifact. This is a concrete configuration-validation defect. Earlier empty-run observations and the unrelated permissions-read 403 did not establish an outage/policy cause; the 422 must not be reframed as a dispatch permission failure. It proves this current parse failure, without requiring a complete causal explanation of every historical empty query.

The earlier requirements review missed this context restriction; this amendment corrects that mistake with authoritative context rules. Prior local YAML/Bash checks and workflow registration are not GitHub expression validation.

Reused the already-read [read-docs skill](skill://plugin_asdk_app_6a314a73f8ac819195b0d55e36b9c609/read-docs/SKILL.md). Firecrawl's HTTP 402 retrieval limit was observed in the preceding focused review, so no repeated failed request was made. Read current official pages directly over HTTPS on 2026-11-24, both HTTP 200; extracted evidence is review-local scratch under `/workspace/work/maintenance-foundation/workflow-env-review/`:

- [Contexts — Context availability](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts#context-availability): `jobs.<job_id>.env` permits github/needs/strategy/matrix/vars/secrets/inputs, **not runner**. `jobs.<job_id>.steps.run` and `.steps.with` explicitly permit runner. Thus the job-level expressions are invalid, while the existing upload step's `with.path: ${{ runner.temp }}/offline-results/` is allowed at that location.
- [Workflow commands — Setting an environment variable](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands#setting-an-environment-variable): append `NAME=value` to the quoted GITHUB_ENV file; the writing step cannot consume the newly set value, but subsequent steps in the same job can. Default GITHUB_* and RUNNER_* variables cannot be overwritten. The proposal only reads RUNNER_TEMP/GITHUB_ENV and sets seven separate custom tool/cache variables, so it does not overwrite those protected variables.
- The previously inspected immutable [setup-dotnet v6 README](https://github.com/actions/setup-dotnet/blob/0f4a354dd5547a28c70b7853dbd95d5b67886e73/README.md#environment-variables) explicitly documents setting DOTNET_INSTALL_DIR through GITHUB_ENV in a step before setup-dotnet. Its bound local README supports this use; no action/version change is needed.

Neither actionlint nor shellcheck was found on PATH during this review. No context-aware lint PASS is claimed, and this requirements review does not install or modify tools. The executor should use a suitable context-aware lint if available, then retain actual GitHub acceptance/execution as authoritative; YAML/Bash parsing alone cannot close the observed defect.

## P0（必须修复，阻断）

None in the bounded proposal.

## P1（必须修复，可本轮闭环）

No unresolved amendment finding. The existing **core P1-01 remains open under its own reviewer** and is an explicit publication/dispatch dependency. This review approves only the configuration design below and its bounded hosted continuation after that closure.

### Exact environment correction accepted

Remove the seven runner-context path assignments from job-level env and insert **one Bash run step after checkout and before setup-dotnet**. Use one quoted `printf '%s\n'` emission of the seven `NAME=value` strings to `"$GITHUB_ENV"`, deriving each value directly from the runtime `"$RUNNER_TEMP"` path. Do not depend on newly emitted variables being available within that same step, evaluate supplied text, print an environment dump or add keys. The subsequent installer/test steps receive the intended job environment.

| Key | Exact value suffix after runtime RUNNER_TEMP |
| --- | --- |
| UPSTREAM_UNIT_TOOLS | `/offline-tools` |
| DOTNET_INSTALL_DIR | `/offline-tools/dotnet` |
| DOTNET_ROOT | `/offline-tools/dotnet` |
| DOTNET_CLI_HOME | `/offline-tools/cache/dotnet-cli` |
| NUGET_PACKAGES | `/offline-tools/cache/nuget` |
| XDG_CACHE_HOME | `/offline-tools/cache/xdg` |
| XDG_DATA_HOME | `/offline-tools/cache/xdg-data` |

Keep the three literal telemetry/first-run/no-logo flags at job level. Preserve the exact checkout/setup-dotnet/upload-artifact pins; SDK 8.0.427/runtime/ref pack 8.0.33; read-only contents permission; dev push/PR and no-input workflow_dispatch; ubuntu runner/job; 40/35/3-minute budgets; controls+suite status capture; report/artifact locations, limits and retention; optional package exclusion. The valid step-level runner.temp artifact expression remains unchanged. No product/test/default behavior or credential/settings change belongs in this correction.

### Source and hosted continuation gate accepted

1. Original runner executor closes the separately reviewed metadata/preflight deadline/cancellation P1 in its two already-owned Python files, with genuine red/unchanged-green slow-metadata expiry/SIGINT/SIGTERM controls, required current-source aggregate evidence and **independent implementation re-review closure**. This focused approval is not that closure.
2. After that closure, publish the exact workflow relocation and synchronize dev; record the final corrected workflow/source SHA and hashes. Do not stage or alter other owners' main-doc/product/test files. Preparation/syntax inspection under this amendment cannot be used to skip the source gate or claim hosted acceptance.
3. Then allow **one** fresh POST for the genuinely corrected revision to `/repos/ZzzzzzzSkyward/bilibili-uwp-liter/actions/workflows/429242571/dispatches`, structured body `{"ref":"dev"}`, same already-authorized identity. The prior single 422 attempt remains immutable history. No repeated dispatch/receipt-push loop, alternate identity, settings/permission/branch change or automatic-review bypass is authorized.
4. Observe the actual new event/head SHA, run/job/step conclusions and artifacts, or record the exact new rejection/unavailability. A request accepted with 204, registration or local lint result is not hosted CI PASS. Any new actual failure returns as bounded evidence to the appropriate owner; it does not authorize another blind retry. Windows/UWP/install/device/session gates remain independently BLOCKED.

Caller Actions-write capability, if already present, remains separate from the workflow's unchanged `contents: read` job token. This approval grants no token scope, repository enablement, connection, credential or identity escalation. A denied operation stays denied and recorded.

## P2（记录到 ROADMAP）

None. No broad workflow refactor, cache policy change, tool migration or permissions repair is necessary. Earlier documentation/source-review statements about the original workflow must remain honest historical observations; the new 422 and eventual corrected hosted outcome belong in the owned execution receipts.

## 验收标准核对

| Contract | Review |
| --- | --- |
| Actual 422 versus speculative external blocker | PASS — explicit seven-expression defect identified; no policy/identity cause inferred |
| Context availability at job env versus step run/with | PASS — current official table checked |
| GITHUB_ENV propagation and step ordering | PASS — runtime paths written before installer, consumed by later job steps |
| Seven values/three literal flags | PASS — exact paths retained, no new or protected environment key written |
| Action pins/job permissions/budgets/artifacts/triggers | PASS — preservation required; no scope expansion |
| Context-aware lint and actual GitHub evidence | PASS — lint if available, no false equivalence with YAML/Bash parsing or CI acceptance |
| Core P1 and one corrected-source dispatch | PASS — independent source closure precedes publication/one new request; no unchanged-config retry |
| Ownership/platform boundaries | PASS — runner executor's two Python files/workflow/evidence only; no main docs/product/account settings |

## 测试/构建证据

Reviewer read official docs and existing source/receipts, checked the current workflow hash and local lint availability, and wrote only this review. No source/workflow/test/main-doc edit, commit/push, dispatch, repository/account-setting operation, identity change or new suite execution occurred. The proposed repair is not implemented or hosted-validated by this document. **Focused amendment PASS** permits the narrow correction under the stated order; source P1, exact implemented-diff review and actual hosted result still require their own evidence.
