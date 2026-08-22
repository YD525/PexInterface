[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

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

$msbuild = Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path -LiteralPath $msbuild)) {
    throw "MSBuild was not found."
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$solution = Join-Path $repositoryRoot "PexInterface.sln"
$arguments = @(
    $solution
    "/restore"
    "/target:Rebuild"
    "/maxCpuCount"
    "/property:Configuration=$Configuration"
    "/property:Platform=$Platform"
    "/property:TreatWarningsAsErrors=true"
    "/property:RunAnalyzersDuringBuild=true"
    "/verbosity:minimal"
    "/nologo"
)

& $msbuild @arguments
if ($LASTEXITCODE -ne 0) {
    throw "PexInterface build failed with exit code $LASTEXITCODE."
}
