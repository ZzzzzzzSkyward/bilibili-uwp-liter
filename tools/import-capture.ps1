# Turns an exported diagnostic bundle (the zip from "导出诊断包", or an already-unpacked
# folder) into replay fixtures for the offline probes: one JSON per captured endpoint.
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools/import-capture.ps1 -Bundle <zip|folder> [-OutDir plan/tests/g3/fixtures] [-Force]
#
# The fixture holds what a probe needs to pin a response contract: the endpoint, how many
# responses were captured, the latest shape (path -> JSON type) and every drift line seen.
# Body samples are copied only when the capture had them enabled.

param(
    [Parameter(Mandatory = $true)][string]$Bundle,
    [string]$OutDir = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Get-TempDir {
    $path = Join-Path ([System.IO.Path]::GetTempPath()) ("bili-capture-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $path | Out-Null
    return $path
}

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $repo "plan\tests\g3\fixtures"
}

# the bundle keeps captures under api/; a flat folder of *.ndjson is accepted too
function Select-CaptureDir([string]$root) {
    $apiDir = Join-Path $root "api"
    if (Test-Path $apiDir) { return $apiDir }
    return $root
}

$resolved = Resolve-Path $Bundle
$workDir = $null

Add-Type -AssemblyName System.IO.Compression.FileSystem

if ((Get-Item $resolved.Path).PSIsContainer) {
    # an already-unpacked bundle goes through exactly the same selection as a zip
    $sourceDir = Select-CaptureDir $resolved.Path
} else {
    $workDir = Get-TempDir
    [System.IO.Compression.ZipFile]::ExtractToDirectory($resolved.Path, $workDir)
    $sourceDir = Select-CaptureDir $workDir
}

$captureFiles = Get-ChildItem -Path $sourceDir -Filter "*.ndjson" -File -ErrorAction SilentlyContinue
if (-not $captureFiles) {
    Write-Host "no *.ndjson capture found under $sourceDir"
    if ($workDir) { Remove-Item -Recurse -Force $workDir }
    exit 1
}

if (-not (Test-Path $OutDir)) {
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

$lines = @()
foreach ($file in $captureFiles) {
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        try {
            $lines += ($line | ConvertFrom-Json)
        } catch {
            Write-Host ("skipped an unparsable line in " + $file.Name)
        }
    }
}

$byEndpoint = $lines | Group-Object -Property ep
$written = 0
foreach ($group in $byEndpoint) {
    $ordered = $group.Group | Sort-Object -Property ts
    $latest = $ordered[-1]
    $shape = $latest.shape
    if ($null -eq $shape) {
        # keep looking for the last line that carried a shape
        for ($i = $ordered.Count - 1; $i -ge 0; $i--) {
            if ($null -ne $ordered[$i].shape) { $shape = $ordered[$i].shape; break }
        }
    }
    if ($null -eq $shape) {
        Write-Host ("skipped " + $group.Name + ": no shape captured")
        continue
    }
    $drift = @()
    foreach ($item in $ordered) {
        if ($null -ne $item.drift) { $drift += $item.drift }
    }
    $fixture = [ordered]@{
        endpoint      = $group.Name
        responses     = $ordered.Count
        firstCaptured = $ordered[0].ts
        lastCaptured  = $latest.ts
        lastStatus    = $latest.status
        lastCode      = $latest.code
        shape         = $shape
        drift         = $drift
    }
    if ($null -ne $latest.sample) {
        $fixture.sample = $latest.sample
    }

    # a fixture name wants the endpoint's tail, not the whole host: .../v1/feed/all -> web-dynamic-v1-feed-all
    $safeName = ($group.Name -replace '[^A-Za-z0-9]+', '-').Trim('-')
    try {
        $uri = [Uri]$group.Name
        $segments = $uri.AbsolutePath.Split('/') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
        if ($segments.Count -gt 0) {
            $tail = $segments | Select-Object -Last 4
            $safeName = (($tail -join '-') -replace '[^A-Za-z0-9]+', '-').Trim('-')
        }
    } catch {
        # keep the sanitized endpoint
    }
    if ([string]::IsNullOrEmpty($safeName)) { $safeName = "endpoint" }
    $target = Join-Path $OutDir ($safeName + ".json")
    if ((Test-Path $target) -and (-not $Force)) {
        Write-Host ("kept existing " + $target + " (use -Force to overwrite)")
        continue
    }
    # write without a BOM so the fixture is plain JSON for the probes
    [System.IO.File]::WriteAllText($target, ($fixture | ConvertTo-Json -Depth 32), (New-Object System.Text.UTF8Encoding($false)))
    Write-Host ("wrote " + $target)
    $written++
}

if ($workDir) { Remove-Item -Recurse -Force $workDir }
Write-Host ("fixtures written: " + $written)
