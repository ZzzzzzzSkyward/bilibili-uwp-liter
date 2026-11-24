# Concrete reviewed workflow

Target: `.github/workflows/offline-probes.yml` on `dev`. Publication/CI status is recorded in execution.md; this retained artifact alone claims neither.

UTF-8 SHA-256: `f013df703c9b318e184c574aac66872a8e2885ff876625df14bd212f196837b1`. Exact requirements action pins, read-only permissions and captured-status job shape are retained.

```yaml
name: Offline probes
on:
  push:
    branches: [dev]
  pull_request:
    branches: [dev]
permissions:
  contents: read
defaults:
  run:
    shell: bash
jobs:
  offline:
    name: Offline probes (legacy)
    runs-on: ubuntu-24.04
    timeout-minutes: 40
    env:
      UPSTREAM_UNIT_TOOLS: ${{ runner.temp }}/offline-tools
      DOTNET_INSTALL_DIR: ${{ runner.temp }}/offline-tools/dotnet
      DOTNET_ROOT: ${{ runner.temp }}/offline-tools/dotnet
      DOTNET_CLI_HOME: ${{ runner.temp }}/offline-tools/cache/dotnet-cli
      NUGET_PACKAGES: ${{ runner.temp }}/offline-tools/cache/nuget
      XDG_CACHE_HOME: ${{ runner.temp }}/offline-tools/cache/xdg
      XDG_DATA_HOME: ${{ runner.temp }}/offline-tools/cache/xdg-data
      DOTNET_CLI_TELEMETRY_OPTOUT: '1'
      DOTNET_SKIP_FIRST_TIME_EXPERIENCE: '1'
      DOTNET_NOLOGO: '1'
    steps:
      - name: Check out complete source
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7
        with:
          fetch-depth: 1
          persist-credentials: false
      - name: Install exact offline SDK
        uses: actions/setup-dotnet@0f4a354dd5547a28c70b7853dbd95d5b67886e73 # v6
        with:
          dotnet-version: '8.0.427'
          check-latest: false
          cache: false
      - name: Run contract controls and complete legacy suite
        timeout-minutes: 35
        run: |
          set +e
          python3 plan/tests/verify-suite-runner.py --output "$RUNNER_TEMP/offline-results/contract"
          contract_status=$?
          bash plan/tests/run-all.sh --output "$RUNNER_TEMP/offline-results/suite"
          suite_status=$?
          set -e
          if [ "$contract_status" -ne 0 ] || [ "$suite_status" -ne 0 ]; then exit 1; fi
      - name: Upload bounded synthetic diagnostics
        if: ${{ always() }}
        timeout-minutes: 3
        uses: actions/upload-artifact@cf430e030ddbb5b0abf93d22962f4752f3646cd9 # v7
        with:
          name: offline-probes-${{ github.run_id }}-${{ github.run_attempt }}
          path: ${{ runner.temp }}/offline-results/
          retention-days: 7
          if-no-files-found: error
          include-hidden-files: false
          compression-level: 6
```
