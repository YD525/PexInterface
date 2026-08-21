$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$manifestPath = Join-Path $repositoryRoot "dependencies.json"
$outputDirectory = Join-Path $repositoryRoot "dependencies"
$temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$temporaryDirectory = Join-Path $temporaryRoot ("PexInterface-dependencies-" + [System.Guid]::NewGuid().ToString("N"))
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$dependency = $manifest.PexReader

if ($dependency.repository -notmatch "^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$") {
    throw "The dependency repository name is invalid."
}

if ($dependency.tag -notmatch "^v[0-9]+\.[0-9]+\.[0-9]+(?:[-.][A-Za-z0-9.-]+)?$") {
    throw "The dependency tag is invalid."
}

foreach ($fileName in @($dependency.asset, $dependency.checksumAsset)) {
    if ([System.IO.Path]::GetFileName($fileName) -ne $fileName) {
        throw "The dependency asset name is invalid: $fileName"
    }
}

if (-not $temporaryDirectory.StartsWith($temporaryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The temporary directory is outside the system temporary directory."
}

New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

try {
    $releaseBaseUri = "https://github.com/$($dependency.repository)/releases/download/$($dependency.tag)"
    $assetPath = Join-Path $temporaryDirectory $dependency.asset
    $checksumPath = Join-Path $temporaryDirectory $dependency.checksumAsset

    Invoke-WebRequest -Uri "$releaseBaseUri/$($dependency.asset)" -OutFile $assetPath
    Invoke-WebRequest -Uri "$releaseBaseUri/$($dependency.checksumAsset)" -OutFile $checksumPath

    $checksumLine = (Get-Content -LiteralPath $checksumPath -Raw).Trim()
    $checksumMatch = [System.Text.RegularExpressions.Regex]::Match(
        $checksumLine,
        "^(?<Hash>[A-Fa-f0-9]{64})\s+[*]?(?<FileName>.+)$")

    if (-not $checksumMatch.Success -or $checksumMatch.Groups["FileName"].Value -ne $dependency.asset) {
        throw "The dependency checksum file has an invalid format."
    }

    $expectedHash = $checksumMatch.Groups["Hash"].Value
    $actualHash = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash
    if (-not $actualHash.Equals($expectedHash, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "The dependency checksum does not match: $($dependency.asset)"
    }

    Copy-Item -LiteralPath $assetPath -Destination (Join-Path $outputDirectory $dependency.asset) -Force
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
