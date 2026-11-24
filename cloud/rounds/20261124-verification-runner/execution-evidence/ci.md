# Hosted CI observation

Workflow publication **SUCCEEDED** through the GitHub contents create-file tool after the runnable source commits were pushed. Commit: `79ba0b04fab9c3b79345e212b6cd1aa6a0229fdf`; exact published bytes/blob and pins are recorded in execution.md and workflow-ready.md. Ordinary git push after publication: evidence commit `00d28afc1fc56657c36d67c2f35869632a653a64`. Both commits are on `dev`; no workflow/branch/Actions settings were changed after publication.

GitHub's workflow API reports ID `429242571`, name `Offline probes`, path `.github/workflows/offline-probes.yml`, state **active**, created `2026-11-24T15:49:11Z`. The repository is public, default `dev`, nonfork, not archived or disabled. These fields establish publication/registration only.

Actual run queries use the generic approved GitHub REST fetch and then authenticated `gh api`, because the specialized commit-workflow wrapper filters PR-triggered runs and cannot certify this push event. Commands (read-only):

```bash
gh api 'repos/ZzzzzzzSkyward/bilibili-uwp-liter/actions/runs?head_sha=79ba0b04fab9c3b79345e212b6cd1aa6a0229fdf&event=push&branch=dev'
gh api 'repos/ZzzzzzzSkyward/bilibili-uwp-liter/actions/runs?head_sha=00d28afc1fc56657c36d67c2f35869632a653a64&event=push&branch=dev'
gh api repos/ZzzzzzzSkyward/bilibili-uwp-liter/commits/79ba0b04fab9c3b79345e212b6cd1aa6a0229fdf/check-runs
gh api repos/ZzzzzzzSkyward/bilibili-uwp-liter/commits/00d28afc1fc56657c36d67c2f35869632a653a64/check-runs
gh api repos/ZzzzzzzSkyward/bilibili-uwp-liter/actions/permissions
```

At `2026-11-24T15:51:11Z`–`15:51:12Z`, both exact-SHA runs requests exited 0 with `{"total_count":0,"workflow_runs":[]}`; both exact-SHA checks requests exited 0 with `{"total_count":0,"check_runs":[]}`. A further observation at `2026-11-24T15:52:08Z`–`15:52:09Z` still returned zero runs for both SHAs, about three minutes after workflow registration. Earlier whole-repository run requests also returned zero runs. Permissions diagnosis returned **HTTP 403**, `Resource not accessible by integration`, documentation endpoint `https://docs.github.com/rest/actions/permissions#get-github-actions-permissions-for-a-repository`; no settings write was attempted. Sanitized timestamped response receipts are retained under `/workspace/work/maintenance-foundation/runner-execution/ci-observations.jsonl`.

Hosted CI is **BLOCKED — no actual push run/check/job/artifact is observable and the available integration cannot read the repository Actions permission setting to diagnose why**. No run URL, runner identity, terminal job conclusion or artifact can be truthfully reported; local Linux PASS, workflow registration and empty API responses are not a GitHub PASS. The underlying cause is not established by the 403 or empty lists. No unapproved workflow_dispatch/PR/settings change, fake run or substituted action pin was introduced.

Next action: repository owner/admin inspects the GitHub Actions enablement/policy and event-producing push credentials, enables execution if appropriate, then makes an authorized normal `dev` push (or `dev`-targeted PR) and observes this same pinned workflow's real run/jobs/artifact to terminal status. Until then hosted CI remains BLOCKED. This external CI observation does not change the proven 11/11 controls, complete 50/51 offline suite or existing Windows/device BLOCKED gates.
