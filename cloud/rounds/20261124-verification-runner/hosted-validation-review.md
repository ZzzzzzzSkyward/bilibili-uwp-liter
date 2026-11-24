# 审查：bounded hosted-validation fallback

- 轮次：`20261124-verification-runner`
- 审查类型：focused amendment / requirements
- 审查员：`logic_review`
- 结论：**PASS — remaining additive manual entry and one dispatch only**
- Controlling input：`hosted-validation-amendment.md`, including its completed-receipt addendum. Step 1 is already completed and is **not** authorized again. This is not hosted CI PASS or an account/settings permission grant.

## 审查范围与证据

Read the amendment/addendum, original approved QF2 requirements/review, current workflow, execution record, `execution-evidence/ci.md` and concrete workflow artifact. Current workflow SHA-256 is `f013df703c9b318e184c574aac66872a8e2885ff876625df14bd212f196837b1`, matching the published/bound artifact. It has only dev push/dev-targeted PR events, the approved pinned offline job, `contents: read`, no persistent checkout credentials, and the existing timeout/report/artifact limits.

Publication commit `79ba0b04fab9c3b79345e212b6cd1aa6a0229fdf`, ordinary evidence push `00d28afc1fc56657c36d67c2f35869632a653a64` and final receipt `b59ae0d60dd40a03fa191c08ca870b99a9384247` are separately recorded. Inspected the latter two immutable commit stats: they change only round execution evidence, not the tested runner/workflow bytes. The addendum explicitly forbids repeating the completed event-test receipt.

The cited observed workflow ID `429242571`, active registration, zero push-run/check counts and Actions-permissions GET 403 establish only the recorded visibility/diagnostic limit. They do **not** establish disabled Actions, a token class, event suppression, billing/policy cause or workflow correctness/incorrectness. No causal inference or settings “repair” is approved. Local 11/11 controls and 50/51 suite evidence remain local evidence, not a hosted result.

Applied the [read-docs skill](skill://plugin_asdk_app_6a314a73f8ac819195b0d55e36b9c609/read-docs/SKILL.md). Firecrawl developer search returned HTTP 402; did not repeat that failed retrieval. Read the same needed official public documentation directly over HTTPS, all HTTP 200, on 2026-11-24. Review-local extracted text is under `/workspace/work/maintenance-foundation/hosted-validation-review/`:

- [Manually running a workflow](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow): workflow_dispatch must be configured, the workflow must be on the default branch, and manual execution requires repository write access.
- [Workflow syntax — workflow_dispatch](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#onworkflow_dispatch): inputs are optional; the event requires the workflow file on the default branch.
- [REST — create a workflow dispatch event](https://docs.github.com/en/rest/actions/workflows#create-a-workflow-dispatch-event): POST the workflow dispatch endpoint, required body ref is a branch/tag; inputs may be omitted. Fine-grained/App tokens require **Actions repository permission (write)**; classic/OAuth tokens require the documented repo scope. The ordinary no-details success response is 204, not a terminal job conclusion. Current documentation also describes optional run-detail responses; no additional API parameter is required by this narrow approval.

## P0（必须修复，阻断）

None.

## P1（必须修复，可本轮闭环）

No unresolved amendment finding. Approval is bounded by the following concrete implementation/evidence contract already expressed by the amendment:

1. **Exactly one workflow-field addition.** Under the existing `on` mapping add `workflow_dispatch:` with no inputs. Keep the dev push/PR events and every job/action/tool/permission/timeout/output/artifact byte unchanged except the event insertion. No schedule, repository_dispatch, PR creation, arbitrary ref/input, secret, deployment or settings change is approved. Verify the published diff and dev synchronization before calling the API.
2. **The workflow must remain on the actual default branch.** Existing repository evidence says default is dev. Consume that fact with the normal source/ref check; do not change the default branch if a newer observation differs. A missing default-branch prerequisite is a blocker, not permission to alter repository policy.
3. **One dispatch, same authorized identity.** The concrete request is `POST /repos/ZzzzzzzSkyward/bilibili-uwp-liter/actions/workflows/429242571/dispatches` with structured JSON `{"ref":"dev"}` and the existing identity. The documented workflow filename endpoint is equivalent, but do not substitute another workflow/repository/ref. Omit inputs. No token text or shell-interpolated request body belongs in output. A normal absent connector capability can use the already-authorized gh API route; an explicit policy/API authorization denial cannot be bypassed through another client, credential or identity.
4. **Separate caller and job permissions.** Dispatch is a state-changing request that creates one run; it is not itself a read-only REST operation. The *workflow job* remains read-only with `contents: read`. Calling dispatch may use an existing identity that already possesses the documented Actions-write capability; this PASS does not authorize adding that capability, changing a token scope, reinstalling a connection, reauthentication/identity escalation or modifying workflow GITHUB_TOKEN permissions. The earlier denied settings-read endpoint proves nothing about this distinct dispatch capability. If dispatch itself is denied, stop that operation and record the actual response.
5. **Observe the actual new event.** A 204/accepted request, registration or matching local YAML is not CI PASS. Record the request time/status, actual workflow_dispatch run ID/URL, exact head SHA and its relation to the synchronized source/workflow inputs, job/step terminal conclusions and artifact availability. Select the actual new workflow/event rather than an unrelated historical push/PR run. Use bounded read-only observation; do not repeat dispatch, manufacture receipt pushes or run an indefinite polling loop. An ambiguous/lost response is not authority to retry the write.
6. **Preserve truthful blockers.** If no run is visible, a run remains pending/unavailable, or dispatch/visibility/settings access is denied, record observed status and the next external prerequisite without guessing the cause. Any automatic approval rejection is terminal for that attempted route; no alternate-client bypass. Keep hosted CI BLOCKED until actual evidence exists, and retain all independent Windows/runtime gates. Docs may describe the manual entry only after it exists, with its actual outcome.

These are acceptance/permission boundaries for the already proposed single change and single API request, not an expanded implementation scope. The orchestrator still controls the runner owner's exact workflow/evidence assignment and serialized push window. Reviewer performs no dispatch or workflow edit.

## P2（记录到 ROADMAP）

None. No repository/identity permissions, test behavior, product source or maintenance backlog change is needed for this fallback. Do not repeat step 1 despite its historical numbered description; the explicit addendum and this approval mark it complete.

## 验收标准核对

| Amendment element | Review |
| --- | --- |
| Prior ordinary receipt-push experiment | Already completed; no repeat authorized |
| Additive manual YAML event | PASS — no-input workflow_dispatch is documented and leaves the existing job contract intact |
| Default-branch/ref requirement | PASS — dev is observed default; no branch-setting change permitted |
| API endpoint/body/count | PASS — exact workflow/repository, structured ref=dev, one request |
| Permission boundary | PASS — existing dispatch caller capability only; job token remains read-only, no scope/settings/identity escalation |
| No-cause inference from zero runs/403 | PASS — amendment explicitly preserves uncertainty |
| Hosted evidence versus local results | PASS — actual new run/SHA/jobs/artifacts required; accepted dispatch is not terminal success |
| Failure/denial handling | PASS — explicit blocker, no bypass/retry loop or unnecessary permission question |
| Source/docs ownership | PASS — runner owner workflow/evidence only; later docs owner after publication |

## 测试/构建证据

This is a focused requirements review. No workflow/source/test/main-doc edit, commit/push, repository/account-setting write, dispatch request, credential action or new suite run was performed by the reviewer. Only `hosted-validation-review.md` is written. No fresh test expansion is needed to review one additive event field; the eventual executor must verify its exact diff/published bytes and actual service outcome. This PASS approves the remaining bounded fallback, not the completed receipt experiment or hosted/Windows/runtime success.
