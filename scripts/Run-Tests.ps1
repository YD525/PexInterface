[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64")]
    [string]$Platform = "x64",

    [string]$ResultsDirectory
)

$ErrorActionPreference = "Stop"

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere)) {
    throw "Visual Studio Installer discovery tool was not found."
}

$installationPath = & $vswhere `
    -latest `
    -products * `
    -requires Microsoft.Component.MSBuild `
    -property installationPath
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($installationPath)) {
    throw "Visual Studio with MSBuild was not found."
}

$testRunner = Join-Path $installationPath `
    "Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
if (-not (Test-Path -LiteralPath $testRunner)) {
    throw "The Visual Studio test runner was not found."
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$testOutput = Join-Path $repositoryRoot "PEXInterfaceUnitTest\bin\$Platform\$Configuration"
$testAssembly = Join-Path $testOutput "PEXInterfaceUnitTest.dll"
$testAdapter = Join-Path $testOutput "MSTest.TestAdapter.dll"
if (-not (Test-Path -LiteralPath $testAssembly)) {
    throw "The PexInterface test assembly was not found: $testAssembly"
}
if (-not (Test-Path -LiteralPath $testAdapter)) {
    throw "The pinned MSTest adapter was not found: $testAdapter"
}

$arguments = @(
    $testAssembly
    "/Platform:$Platform"
    "/TestAdapterPath:$testOutput"
    "/Logger:Console;Verbosity=normal"
)

if (-not [string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $resolvedResultsDirectory = if ([IO.Path]::IsPathRooted($ResultsDirectory)) {
        [IO.Path]::GetFullPath($ResultsDirectory)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path $repositoryRoot $ResultsDirectory))
    }

    New-Item -ItemType Directory -Path $resolvedResultsDirectory -Force | Out-Null
    $arguments += "/ResultsDirectory:$resolvedResultsDirectory"
    $arguments += "/Logger:trx;LogFileName=PEXInterfaceUnitTest.trx"
}

& $testRunner @arguments
if ($LASTEXITCODE -ne 0) {
    throw "PexInterface tests failed with exit code $LASTEXITCODE."
}
