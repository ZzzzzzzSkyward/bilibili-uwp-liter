<#
.SYNOPSIS
  生成 BiliLite x64 Release 包（MSIX），默认输出到 Z:\（与 BiliLite.UWP.csproj 的 AppxPackageDir 一致）。
  每次运行都会先把包版本号加一（修订号 +1，与清单保持同源），保证每个版本都能覆盖安装。
.PARAMETER Clean
  先清理再重新生成（Rebuild）。
.PARAMETER OutputDir
  MSIX 输出目录，默认 Z:\。
.PARAMETER NoVersionBump
  不递增版本号（用于调试/重复出包）。
.EXAMPLE
  .\build-release-x64.ps1
.EXAMPLE
  .\build-release-x64.ps1 -Clean
#>
param(
    [switch]$Clean,
    [string]$OutputDir = 'Z:\',
    [switch]$NoVersionBump
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root 'BiliLite.sln'
if (-not (Test-Path $solution)) {
    throw "找不到解决方案：$solution"
}
if (-not (Test-Path $OutputDir)) {
    throw "输出目录不存在：$OutputDir（可用 -OutputDir 指定其他路径）"
}
$OutputDir = (Resolve-Path $OutputDir).ProviderPath.TrimEnd('\') + '\'

# ---------- 版本号：递增后再生成，确保每个发布版本都更高 ----------
$manifests = @(
    (Join-Path $root 'src\BiliLite.UWP\Package.appxmanifest'),
    (Join-Path $root 'src\BiliLite.Packages\Package.appxmanifest')
)
$versionPattern = '(?s)<Identity\b[^>]*?\bVersion="(\d+)\.(\d+)\.(\d+)\.(\d+)"'
$versionReplace = '(?s)(<Identity\b[^>]*?\bVersion=")[^"]+(")'

$texts = @{}
$versions = @()
foreach ($manifest in $manifests) {
    if (-not (Test-Path $manifest)) {
        throw "找不到包清单：$manifest"
    }
    $text = [System.IO.File]::ReadAllText($manifest)
    if ($text -notmatch $versionPattern) {
        throw "包清单里没有找到 Identity 的版本号：$manifest"
    }
    $texts[$manifest] = $text
    $versions += [version]("{0}.{1}.{2}.{3}" -f $Matches[1], $Matches[2], $Matches[3], $Matches[4])
}

$current = ($versions | Sort-Object | Select-Object -Last 1)
$next = $current
if (-not $NoVersionBump) {
    if ($current.Revision -lt 65535) {
        $next = [version]::new($current.Major, $current.Minor, $current.Build, $current.Revision + 1)
    }
    elseif ($current.Build -lt 65535) {
        $next = [version]::new($current.Major, $current.Minor, $current.Build + 1, 0)
    }
    else {
        throw "版本号已达上限，无法递增：$current"
    }

    $utf8Bom = New-Object System.Text.UTF8Encoding($true)
    foreach ($manifest in $manifests) {
        $updated = $texts[$manifest] -replace $versionReplace, ('${1}' + $next.ToString() + '${2}')
        [System.IO.File]::WriteAllText($manifest, $updated, $utf8Bom)
    }
}

# 通过 vswhere 定位 Visual Studio 自带的 MSBuild
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw "找不到 vswhere.exe，请安装 Visual Studio 2022（含 UWP 工作负载）：$vswhere"
}
$msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
    throw '找不到 MSBuild.exe，请安装 Visual Studio 2022（含 MSBuild 组件）。'
}

$target = if ($Clean) { 'Rebuild' } else { 'Build' }
Write-Host "MSBuild : $msbuild"
Write-Host "解决方案: $solution"
Write-Host "配置    : Release|x64 ($target)"
Write-Host "输出目录: $OutputDir"
if ($NoVersionBump) {
    Write-Host "包版本  : $current（未递增）"
}
else {
    Write-Host "包版本  : $current -> $next（两个清单已更新）"
}

# 还原 NuGet 包：packages.config 与 PackageReference 两种项目都要覆盖
& $msbuild $solution -t:Restore -p:RestorePackagesConfig=true -v:m -nologo
if ($LASTEXITCODE -ne 0) {
    throw "NuGet 还原失败（退出码 $LASTEXITCODE）。"
}

$buildOutput = @()
& $msbuild $solution -t:$target -p:Configuration=Release -p:Platform=x64 -p:AppxPackageDir=$OutputDir -v:m -nologo |
    Tee-Object -Variable buildOutput
if ($LASTEXITCODE -ne 0) {
    throw "生成失败（退出码 $LASTEXITCODE）。"
}

$packages = $buildOutput | ForEach-Object {
    if ($_ -match '->\s+(\S+\.msix)$') { $Matches[1] }
} | Select-Object -Unique
if ($packages) {
    Write-Host ''
    Write-Host '生成产物：'
    $packages | ForEach-Object { Write-Host "  $_" }
}
