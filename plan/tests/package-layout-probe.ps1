param(
    [string]$BuildScriptPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, '..', '..', 'build-release-x64.ps1')),
    [string]$ScratchRoot = [System.IO.Path]::GetTempPath()
)

$ErrorActionPreference = 'Stop'
$fixtureDirectory = $null
$caseCount = 0
$passed = 0
$failed = 0
$exitCode = 2

function Get-TextSha256 {
    param([string]$Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Text)))).Replace('-', '').ToLowerInvariant()
    }
    finally { $sha.Dispose() }
}

function Add-InertEntry {
    param($Archive, [string]$Name, [long]$Length)
    $entry = $Archive.CreateEntry($Name, [System.IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    try {
        $buffer = New-Object byte[] 8192
        $remaining = $Length
        while ($remaining -gt 0) {
            $count = [int][System.Math]::Min($remaining, [long]$buffer.Length)
            $stream.Write($buffer, 0, $count)
            $remaining -= $count
        }
    }
    finally { $stream.Dispose() }
}

function New-PackageFixture {
    param(
        [string]$Name,
        [long]$RootBytes = 8192,
        [switch]$NoRoot,
        [switch]$NestedRoot,
        [switch]$NoEntrypoint,
        [switch]$NoCompanion,
        [switch]$NoManifest,
        [switch]$NoDeclaration
    )
    $path = [System.IO.Path]::Combine($fixtureDirectory, $Name + '.msix')
    $archive = [System.IO.Compression.ZipFile]::Open($path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        if (-not $NoRoot) {
            $rootName = 'BiliLiter.exe'
            if ($NestedRoot) { $rootName = 'nested/BiliLiter.exe' }
            Add-InertEntry -Archive $archive -Name $rootName -Length $RootBytes
        }
        if (-not $NoEntrypoint) { Add-InertEntry -Archive $archive -Name 'entrypoint/BiliLiter.exe' -Length 1 }
        if (-not $NoCompanion) { Add-InertEntry -Archive $archive -Name 'BiliLite.Win32Tools/BiliLite.Win32Tools.exe' -Length 1 }
        if (-not $NoManifest) {
            $entry = $archive.CreateEntry('AppxManifest.xml')
            $writer = New-Object System.IO.StreamWriter($entry.Open())
            try {
                if ($NoDeclaration) { $writer.Write('<Package />') }
                else { $writer.Write('<Package><Extension Category="windows.fullTrustProcess" /></Package>') }
            }
            finally { $writer.Dispose() }
        }
    }
    finally { $archive.Dispose() }
    return $path
}

function New-Case {
    param([string]$Name, [string]$Path, $Expected = $null, [bool]$Throws = $false)
    return [pscustomobject]@{ Name = $Name; Path = $Path; Expected = $Expected; Throws = $Throws }
}

try {
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($BuildScriptPath, [ref]$tokens, [ref]$parseErrors)
    if (@($parseErrors).Count -ne 0) { throw 'Build-script parse failed; no selected function or top-level script was executed.' }
    $names = @('Read-AppxPackageLayout', 'Test-FullPackageLayout')
    $definitions = @($ast.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -in $names
    }, $true))
    $selected = @()
    foreach ($name in $names) {
        $matches = @($definitions | Where-Object { $_.Name -eq $name })
        if ($matches.Count -ne 1) { throw "Missing or duplicate actual function: $name" }
        $definition = $matches[0]
        if (-not ($definition.Parent -is [System.Management.Automation.Language.NamedBlockAst]) -or
            -not [object]::ReferenceEquals($definition.Parent.Parent, $ast)) {
            throw "Expected function is not top-level: $name"
        }
        $selected += $definition
    }
    Write-Output ('BUILD_SCRIPT_SHA256=' + (Get-FileHash -LiteralPath $BuildScriptPath -Algorithm SHA256).Hash.ToLowerInvariant())
    foreach ($definition in $selected) {
        Write-Output ('FUNCTION=' + $definition.Name + ' START_LINE=' + $definition.Extent.StartLineNumber +
            ' END_LINE=' + $definition.Extent.EndLineNumber + ' SHA256=' + (Get-TextSha256 -Text $definition.Extent.Text))
    }
    # Define only the exact two selected function extents; never invoke the release script AST.
    $functionText = ($selected | ForEach-Object { $_.Extent.Text }) -join "`n`n"
    . ([scriptblock]::Create($functionText))
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    Write-Output 'EXTRACTED=2 BUILD_SCRIPT_PARSE_ONLY=YES'

    $fixtureDirectory = [System.IO.Path]::Combine([System.IO.Path]::GetFullPath($ScratchRoot),
        'package layout probe [' + [System.Guid]::NewGuid().ToString('N') + ']')
    [System.IO.Directory]::CreateDirectory($fixtureDirectory) | Out-Null
    $good = New-PackageFixture -Name 'canonical root 8192' -RootBytes 8192
    $boundary = New-PackageFixture -Name 'canonical boundary 65535' -RootBytes 65535
    $compressed = New-PackageFixture -Name 'compressed root 4 MiB' -RootBytes 4194304
    $archive = [System.IO.Compression.ZipFile]::OpenRead($compressed)
    try {
        $entry = $archive.GetEntry('BiliLiter.exe')
        if ($entry.Length -ne 4194304 -or $entry.CompressedLength -ge 64KB) { throw 'Compressed oversize fixture setup failed.' }
        Write-Output ('COMPRESSED_ROOT_LENGTH=' + $entry.Length + ' COMPRESSED_LENGTH=' + $entry.CompressedLength)
    }
    finally { $archive.Dispose() }

    $corrupt = [System.IO.Path]::Combine($fixtureDirectory, 'corrupt archive.msix')
    [System.IO.File]::WriteAllText($corrupt, 'inert non-ZIP bytes')
    $missing = [System.IO.Path]::Combine($fixtureDirectory, 'missing [archive] space.msix')
    $cases = @(
        (New-Case -Name 'canonical root 8192' -Path $good)
        (New-Case -Name 'canonical boundary 65535 and Int64 reader' -Path $boundary)
        (New-Case -Name 'zero root limited-predicate characterization' -Path (New-PackageFixture -Name 'zero root' -RootBytes 0))
        (New-Case -Name 'root absent' -Path (New-PackageFixture -Name 'root absent' -NoRoot) -Expected '缺少根目录 BiliLiter.exe')
        (New-Case -Name 'nested-only root' -Path (New-PackageFixture -Name 'nested-only root' -NestedRoot) -Expected '缺少根目录 BiliLiter.exe')
        (New-Case -Name 'root exactly 65536' -Path (New-PackageFixture -Name 'root exactly 65536' -RootBytes 65536) -Expected '根目录 BiliLiter.exe 必须小于 64 KB')
        (New-Case -Name 'root 65537' -Path (New-PackageFixture -Name 'root 65537' -RootBytes 65537) -Expected '根目录 BiliLiter.exe 必须小于 64 KB')
        (New-Case -Name 'compressed 4 MiB uncompressed-root limit' -Path $compressed -Expected '根目录 BiliLiter.exe 必须小于 64 KB')
        (New-Case -Name 'entrypoint absent' -Path (New-PackageFixture -Name 'entrypoint absent' -NoEntrypoint) -Expected '缺少 entrypoint/BiliLiter.exe')
        (New-Case -Name 'companion absent' -Path (New-PackageFixture -Name 'companion absent' -NoCompanion) -Expected '缺少 BiliLite.Win32Tools')
        (New-Case -Name 'manifest absent' -Path (New-PackageFixture -Name 'manifest absent' -NoManifest) -Expected '清单里没有 fullTrustProcess 扩展')
        (New-Case -Name 'manifest declaration absent' -Path (New-PackageFixture -Name 'manifest declaration absent' -NoDeclaration) -Expected '清单里没有 fullTrustProcess 扩展')
        (New-Case -Name 'old entrypoint diagnostic wins over missing root' -Path (New-PackageFixture -Name 'entrypoint and root absent' -NoRoot -NoEntrypoint) -Expected '缺少 entrypoint/BiliLiter.exe')
        (New-Case -Name 'corrupt physical archive throws' -Path $corrupt -Throws $true)
        (New-Case -Name 'missing physical archive with brackets and spaces throws' -Path $missing -Throws $true)
    )
    if ($cases.Count -ne 15) { throw 'Mandatory fixture inventory is incomplete.' }
    foreach ($case in $cases) {
        $problem = $null
        $exception = $null
        $metadataOk = $true
        try {
            $problem = Test-FullPackageLayout -Path $case.Path
            if ($case.Path -eq $boundary) {
                $layout = Read-AppxPackageLayout -Path $case.Path
                $length = $layout.Entries['BiliLiter.exe']
                Write-Output ('ROOT_LENGTH_TYPE=' + $length.GetType().FullName + ' ROOT_LENGTH=' + $length)
                $metadataOk = $length -is [long] -and $length -eq 65535
            }
        }
        catch { $exception = $_.Exception }
        $ok = $false
        if ($case.Throws) { $ok = $null -ne $exception }
        elseif ($null -eq $exception -and $metadataOk) {
            if ($null -eq $case.Expected) { $ok = $null -eq $problem }
            else { $ok = $problem -is [string] -and $problem -ceq $case.Expected }
        }
        $description = 'ACCEPTED'
        if ($null -ne $exception) { $description = 'THREW ' + $exception.GetType().FullName }
        elseif ($null -ne $problem) { $description = 'REJECTED ' + $problem }
        $caseCount++
        if ($ok) { $passed++; Write-Output ('PASS ' + $case.Name + ' actual=' + $description) }
        else { $failed++; Write-Output ('FAIL ' + $case.Name + ' actual=' + $description + ' metadataOk=' + $metadataOk) }
    }
    $exitCode = 0
    if ($failed -ne 0) { $exitCode = 1 }
}
catch {
    [System.Console]::Error.WriteLine('SETUP_FAILURE: ' + $_.Exception.Message)
    $exitCode = 2
}
finally {
    if ($null -ne $fixtureDirectory -and [System.IO.Directory]::Exists($fixtureDirectory)) {
        try { [System.IO.Directory]::Delete($fixtureDirectory, $true) }
        catch {
            [System.Console]::Error.WriteLine('CLEANUP_FAILURE: ' + $_.Exception.Message)
            $exitCode = 2
        }
    }
}
Write-Output "CASES=$caseCount PASSED=$passed FAILED=$failed"
exit $exitCode
