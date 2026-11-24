# Hosted workflow environment correction

Status: READY FOR INDEPENDENT AMENDMENT REVIEW; no configuration repair before PASS. Actual GitHub dispatch receipt in hosted-validation-execution.md is controlling: HTTP 422 rejects seven job-level env references to runner.temp. Earlier empty-run observations did not establish an external outage/policy cause; this is now a concrete workflow-expression defect.

## Exact correction

- Keep the seven isolated path values exactly as reviewed, but remove their runner-context expressions from **job-level env**, where GitHub rejects that context. Retain the literal telemetry/no-logo flags there.
- Insert **one** ordinary Bash configuration step after checkout and before setup-dotnet. Use the runtime `$RUNNER_TEMP` value and write the seven `NAME=value` assignments to `$GITHUB_ENV` using quoted `printf '%s\n'` values. They become job environment values for the subsequent installer/test steps: UPSTREAM_UNIT_TOOLS=.../offline-tools; DOTNET_INSTALL_DIR and DOTNET_ROOT=.../offline-tools/dotnet; DOTNET_CLI_HOME=.../offline-tools/cache/dotnet-cli; NUGET_PACKAGES=.../offline-tools/cache/nuget; XDG_CACHE_HOME=.../offline-tools/cache/xdg; XDG_DATA_HOME=.../offline-tools/cache/xdg-data. No new environment keys, arbitrary env dump, credential or account operation.
- Preserve the exact action pins, SDK/ref pack, read-only contents permission, dev push/PR and already approved no-input manual trigger, job/step budgets, both-status capture, report/artifact paths/limits and optional package exclusion. Existing step-level runner.temp artifact-path use is retained only after confirming that context is supported there.
- Verify against current official Context Availability and setting-environment-variables documentation with read-docs. YAML/Bash parsing alone does not validate GitHub expression contexts. Use an appropriate context-aware lint if available and retain actual GitHub validation as authoritative.

## Source finding and validation order

The original runner executor separately fixes the already-reviewed aggregate/control P1-01 from final-review.md: cancellation and suite budget must cover every provenance/SDK child and stop launching suffix metadata. This is existing acceptance/ownership, not a requirements expansion. Add the requested slow-metadata expiry/SIGINT/SIGTERM regression cases, preserving all prior controls and source/selector/compiler bodies.

After the source fix is locally verified and independent re-review closes it, publish the exact workflow repair, synchronize dev and authorize **one** fresh ref=dev manual dispatch for that genuinely corrected workflow/source revision. The prior single 422 attempt remains history; this is not a blind retry of unchanged invalid config. Observe actual run/head/job/conclusion and artifacts or exact new rejection. No enablement/permissions/identity/branch settings, loop, release/master/live API operation. A new actual error requires evidence and bounded owner action; no CI PASS from parsing or registration.

Owner remains runner_executor; exact scope remains its two Python aggregate/control files, existing workflow and execution evidence. No product/other tests/main docs. Reviewer owns only workflow-env-review.md (PASS/CHANGES), verifies official context/command rules and narrow preserved values/permissions. This amendment does not clear source P1 or grant a hosted PASS.
