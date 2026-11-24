# Workflow environment repair — published, READY FOR CORE RE-REVIEW

Approved correction: workflow-env-correction.md / independent workflow-env-review.md PASS. Execution order follows workflow-env-execution-dispatch.md: source fix/control/full gate, exact config publication/synchronization, return READY FOR CORE RE-REVIEW; no manual dispatch before source P1 closure.

Source fix commit **9c33bce28a6222d73e05be134efa60abf0344ecd** is pushed. The environment repair was published via GitHub contents update-file with current blob lease `5164de4292e04acab65cb7627e9ee7457a957766`, resulting commit **`c4e9dba27f49e77886598d137f6d3d81b370e60c`**, blob `53b40aac5dc9ab10cdc17351052f437a37bf22fc`. Fast-forward pull succeeded; local HEAD/origin/dev/ls-remote matched. Published bytes exactly match prepared/linted scratch bytes. New workflow SHA-256 **`a12d8d6306809be5aa484d535a7314ccd2e4dbe8c6ff18dab13f547b4c85bfe9`**.

Removed exactly seven rejected job-level runner.temp path values, retained the three literal telemetry/first-run/no-logo flags, and inserted this one step after checkout and before setup-dotnet:

```yaml
      - name: Configure isolated offline environment
        run: |
          printf '%s\n' \
            "UPSTREAM_UNIT_TOOLS=$RUNNER_TEMP/offline-tools" \
            "DOTNET_INSTALL_DIR=$RUNNER_TEMP/offline-tools/dotnet" \
            "DOTNET_ROOT=$RUNNER_TEMP/offline-tools/dotnet" \
            "DOTNET_CLI_HOME=$RUNNER_TEMP/offline-tools/cache/dotnet-cli" \
            "NUGET_PACKAGES=$RUNNER_TEMP/offline-tools/cache/nuget" \
            "XDG_CACHE_HOME=$RUNNER_TEMP/offline-tools/cache/xdg" \
            "XDG_DATA_HOME=$RUNNER_TEMP/offline-tools/cache/xdg-data" >> "$GITHUB_ENV"
```

All seven values remain exactly equivalent to their approved runtime paths. Parsed structure comparison asserts all other job/root/trigger/action/pin/permission/step/budget/report/artifact fields unchanged. The valid step-level runner.temp artifact path is retained. Quoted Bash syntax passes; executing this exact setup step against a synthetic runtime path containing spaces writes exactly the seven intended key/value strings, without overwriting any GITHUB_* or RUNNER_* variable. No source/cache/data/account/environment dump is written to uploaded diagnostics.

Read existing current official context and GITHUB_ENV evidence from `/workspace/work/maintenance-foundation/workflow-env-review/`, reusing read-docs:

- https://docs.github.com/en/actions/reference/workflows-and-actions/contexts#context-availability — job env excludes runner; step run/with permit it.
- https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands#setting-an-environment-variable — GITHUB_ENV values propagate to subsequent steps; setup precedes installer/test.

Context-aware lint is available for this correction: installed official **actionlint v1.7.12** to scratch, downloaded its linux-amd64 release archive and verified SHA-256 **`8aca8db96f1b94770f1b0d72b6dddcb1ebb8123cb3712530b08cc387b349a3d8`** against the official release checksum file. Command form: `actionlint -shellcheck= -pyflakes= <workflow>` (those optional lint tools are unavailable; no claim that they ran). Original published workflow exits **1**, with exactly the seven disallowed runner-context diagnostics. Corrected workflow exits **0**, no diagnostics. Receipts: `/workspace/work/maintenance-foundation/runner-execution/actionlint/receipt.json`, original.txt, corrected.txt. Prepared bytes/equivalence/spaced-path test: offline-probes-corrected.yml and workflow-env-validation.json in that execution scratch root. Lint is not an actual hosted CI PASS.

The original one manual request's HTTP422 is preserved in hosted-validation-execution.md as historical validation failure; this correction has not been manually dispatched. Read-only exact-head automatic run observation is recorded separately below if available. No settings/identity/token/permissions/branch/release change, hidden retry or event-test receipt loop. Actual GitHub run/job/artifact evidence remains required after independent source P1 closure; no hosted/Windows PASS from local lint. Ownership/global window is released for core re-review at this READY checkpoint.

Exact corrected-workflow-head read-only observation at `2026-11-24T16:09:50.001649+00:00`: `total_count=0`; no corrected manual dispatch is sent. The final truthful review receipt is an ordinary metadata push; before any later manual request, the continuation owner must check its actual exact head for an automatic run and observe an existing run instead of duplicating it.
