[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$manifest = Get-Content -LiteralPath (Join-Path $repositoryRoot "dependencies.json") -Raw | ConvertFrom-Json
$dependency = $manifest.PexReader
$dependencyDirectory = Join-Path $repositoryRoot "dependencies"
$nativePath = Join-Path $dependencyDirectory $dependency.asset
$checksumPath = Join-Path $dependencyDirectory $dependency.checksumAsset

foreach ($path in @($nativePath, $checksumPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "The restored native dependency is incomplete: $([IO.Path]::GetFileName($path))"
    }
}

$checksumLine = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
$checksumMatch = [Text.RegularExpressions.Regex]::Match(
    $checksumLine,
    "^(?<Hash>[A-Fa-f0-9]{64})\s+[*]?(?<FileName>.+)$")
if (-not $checksumMatch.Success -or $checksumMatch.Groups["FileName"].Value -ne $dependency.asset) {
    throw "The restored dependency checksum has an invalid format."
}

$actualHash = (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash
if (-not $actualHash.Equals($checksumMatch.Groups["Hash"].Value, [StringComparison]::OrdinalIgnoreCase)) {
    throw "The restored dependency checksum does not match: $($dependency.asset)"
}

$expectedVersion = $dependency.tag.TrimStart("v")
$versionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($nativePath)
if ($versionInfo.FileVersion -ne $expectedVersion -or $versionInfo.ProductVersion -ne $expectedVersion) {
    throw "The restored dependency version does not match $($dependency.tag)."
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw "Visual Studio Installer discovery tool was not found."
}

$installationPath = & $vswhere `
    -latest `
    -products * `
    -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
    -property installationPath
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($installationPath)) {
    throw "Visual Studio with the x64 C++ toolset was not found."
}

$dumpbin = Get-ChildItem -LiteralPath (Join-Path $installationPath "VC\Tools\MSVC") `
    -Filter dumpbin.exe `
    -File `
    -Recurse |
    Where-Object { $_.FullName -match "\\bin\\Hostx64\\x64\\dumpbin\.exe$" } |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if ($null -eq $dumpbin) {
    throw "The x64 dumpbin tool was not found."
}

$headers = & $dumpbin.FullName /headers $nativePath
if ($LASTEXITCODE -ne 0 -or ($headers -join "`n") -notmatch "8664 machine \(x64\)") {
    throw "The restored dependency is not an x64 PE binary."
}

$exportsOutput = & $dumpbin.FullName /exports $nativePath
if ($LASTEXITCODE -ne 0) {
    throw "The restored dependency exports could not be inspected."
}

$exports = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($line in $exportsOutput) {
    $match = [Text.RegularExpressions.Regex]::Match(
        $line,
        "^\s+\d+\s+[0-9A-F]+\s+[0-9A-F]+\s+(?<Name>C_[A-Za-z0-9_]+)(?:\s|$)")
    if ($match.Success) {
        [void]$exports.Add($match.Groups["Name"].Value)
    }
}

$interopSource = Get-Content -LiteralPath (Join-Path $repositoryRoot "PexInterface\PexReader.cs")
$imports = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($line in $interopSource) {
    $match = [Text.RegularExpressions.Regex]::Match(
        $line,
        "\bpublic\s+static\s+extern\s+.+?\b(?<Name>C_[A-Za-z0-9_]+)\s*\(")
    if ($match.Success) {
        [void]$imports.Add($match.Groups["Name"].Value)
    }
}

if ($imports.Count -eq 0) {
    throw "No managed native imports were discovered."
}

$missingExports = @($imports | Where-Object { -not $exports.Contains($_) } | Sort-Object)
if ($missingExports.Count -ne 0) {
    throw "The native dependency is missing managed ABI exports: $($missingExports -join ', ')"
}

Write-Host "Verified $($dependency.tag), SHA-256, x64 architecture, and $($imports.Count) managed ABI exports."
