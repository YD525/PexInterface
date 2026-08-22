[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseRef
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

if ($null -eq ([AppDomain]::CurrentDomain.GetAssemblies() |
        Where-Object { $_.GetName().Name -eq "Microsoft.CodeAnalysis.CSharp" } |
        Select-Object -First 1)) {
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
        throw "Visual Studio with the C# compiler was not found."
    }

    $roslynDirectory = Join-Path $installationPath "MSBuild\Current\Bin\Roslyn"
    Add-Type -Path (Join-Path $roslynDirectory "Microsoft.CodeAnalysis.dll")
    Add-Type -Path (Join-Path $roslynDirectory "Microsoft.CodeAnalysis.CSharp.dll")
}

function Get-AccessibilityModifiers {
    param([Microsoft.CodeAnalysis.SyntaxNode]$Node)

    $property = $Node.GetType().GetProperty("Modifiers")
    if ($null -eq $property) {
        return @()
    }

    return @($property.GetValue($Node) | ForEach-Object { $_.Text })
}

function Test-DeclaredExternalVisibility {
    param([Microsoft.CodeAnalysis.SyntaxNode]$Node)

    if ($Node -is [Microsoft.CodeAnalysis.CSharp.Syntax.EnumMemberDeclarationSyntax]) {
        return $true
    }

    $modifiers = @(Get-AccessibilityModifiers -Node $Node)
    $parentIsInterface = $Node.Parent -is [Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax]
    $hasAccessibility = @($modifiers | Where-Object {
            $_ -in @("public", "protected", "private", "internal")
        }).Count -ne 0
    if ($parentIsInterface -and -not $hasAccessibility) {
        return $true
    }

    return "public" -in $modifiers -or "protected" -in $modifiers
}

function Test-EffectiveExternalVisibility {
    param([Microsoft.CodeAnalysis.SyntaxNode]$Node)

    if (-not (Test-DeclaredExternalVisibility -Node $Node)) {
        return $false
    }

    $ancestor = $Node.Parent
    while ($null -ne $ancestor) {
        if ($ancestor -is [Microsoft.CodeAnalysis.CSharp.Syntax.BaseTypeDeclarationSyntax] -and
            -not (Test-DeclaredExternalVisibility -Node $ancestor)) {
            return $false
        }
        $ancestor = $ancestor.Parent
    }

    return $true
}

function Get-SyntaxPropertyValue {
    param(
        [Microsoft.CodeAnalysis.SyntaxNode]$Node,
        [string]$Name
    )

    $property = $Node.GetType().GetProperty($Name)
    if ($null -eq $property) {
        return $null
    }

    return $property.GetValue($Node)
}

function Get-ContractRanges {
    param([Microsoft.CodeAnalysis.SyntaxNode]$Declaration)

    $location = $Declaration.GetLocation().GetLineSpan()
    $startLine = $location.StartLinePosition.Line + 1
    $endLine = $location.EndLinePosition.Line + 1
    $ranges = [Collections.Generic.List[object]]::new()

    $accessorList = Get-SyntaxPropertyValue -Node $Declaration -Name "AccessorList"
    if ($null -ne $accessorList) {
        $accessorOpenLine = $accessorList.OpenBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        $ranges.Add([pscustomobject]@{ Start = $startLine; End = $accessorOpenLine })

        foreach ($accessor in $accessorList.Accessors) {
            $accessorLocation = $accessor.GetLocation().GetLineSpan()
            $accessorStart = $accessorLocation.StartLinePosition.Line + 1
            $accessorEnd = $accessorLocation.EndLinePosition.Line + 1
            $accessorBody = Get-SyntaxPropertyValue -Node $accessor -Name "Body"
            $accessorExpressionBody = Get-SyntaxPropertyValue -Node $accessor -Name "ExpressionBody"
            if ($null -ne $accessorBody) {
                $accessorEnd = $accessorBody.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            }
            elseif ($null -ne $accessorExpressionBody) {
                $accessorEnd = $accessorExpressionBody.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            }
            $ranges.Add([pscustomobject]@{ Start = $accessorStart; End = $accessorEnd })
        }

        return $ranges
    }

    $body = Get-SyntaxPropertyValue -Node $Declaration -Name "Body"
    $expressionBody = Get-SyntaxPropertyValue -Node $Declaration -Name "ExpressionBody"
    $openBraceToken = Get-SyntaxPropertyValue -Node $Declaration -Name "OpenBraceToken"
    if ($null -ne $body) {
        $endLine = $body.GetLocation().GetLineSpan().StartLinePosition.Line + 1
    }
    elseif ($null -ne $expressionBody) {
        $endLine = $expressionBody.GetLocation().GetLineSpan().StartLinePosition.Line + 1
    }
    elseif ($null -ne $openBraceToken -and $openBraceToken.RawKind -ne 0) {
        $endLine = $openBraceToken.GetLocation().GetLineSpan().StartLinePosition.Line + 1
    }

    $ranges.Add([pscustomobject]@{ Start = $startLine; End = $endLine })
    return $ranges
}

Push-Location $repositoryRoot
try {
    & git rev-parse --verify --quiet "$BaseRef^{commit}" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "The documentation comparison ref does not exist: $BaseRef"
    }

    $diff = & git -c core.quotepath=false diff `
        --unified=0 `
        --no-color `
        --no-ext-diff `
        --diff-filter=ACMR `
        --no-renames `
        $BaseRef `
        -- `
        "*.cs"
    if ($LASTEXITCODE -ne 0) {
        throw "Git could not determine changed C# lines."
    }

    $changedLinesByFile = @{}
    $currentFile = $null
    foreach ($line in $diff) {
        if ($line -match '^\+\+\+ b/(?<Path>.+)$') {
            $currentFile = $Matches.Path
            if (-not $changedLinesByFile.ContainsKey($currentFile)) {
                $changedLinesByFile[$currentFile] = [Collections.Generic.HashSet[int]]::new()
            }
            continue
        }

        if ($null -ne $currentFile -and
            $line -match '^@@ -\d+(?:,\d+)? \+(?<Start>\d+)(?:,(?<Count>\d+))? @@') {
            $start = [int]$Matches.Start
            $count = if ($Matches.ContainsKey("Count")) { [int]$Matches["Count"] } else { 1 }
            for ($lineNumber = $start; $lineNumber -lt ($start + $count); $lineNumber++) {
                [void]$changedLinesByFile[$currentFile].Add($lineNumber)
            }
        }
    }

    $failures = [Collections.Generic.List[string]]::new()

    foreach ($entry in $changedLinesByFile.GetEnumerator()) {
        $path = Join-Path $repositoryRoot $entry.Key
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            continue
        }

        $source = Get-Content -LiteralPath $path -Raw
        $syntaxTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($source)
        $root = $syntaxTree.GetRoot()
        $declarations = @($root.DescendantNodes() | Where-Object {
                $_ -is [Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax] -and
                $_ -isnot [Microsoft.CodeAnalysis.CSharp.Syntax.BaseNamespaceDeclarationSyntax]
            })

        foreach ($declaration in $declarations) {
            if (-not (Test-EffectiveExternalVisibility -Node $declaration)) {
                continue
            }

            $lineSpan = $declaration.GetLocation().GetLineSpan()
            $declarationStart = $lineSpan.StartLinePosition.Line + 1
            $contractChanged = $false
            foreach ($range in @(Get-ContractRanges -Declaration $declaration)) {
                for ($candidate = $range.Start; $candidate -le $range.End; $candidate++) {
                    if ($entry.Value.Contains($candidate)) {
                        $contractChanged = $true
                        break
                    }
                }
                if ($contractChanged) {
                    break
                }
            }
            if (-not $contractChanged) {
                continue
            }

            $leadingTrivia = $declaration.GetLeadingTrivia().ToFullString()
            if ($leadingTrivia -notmatch '(?m)^\s*///') {
                $failures.Add("$($entry.Key):$declarationStart")
            }
        }
    }

    if ($failures.Count -ne 0) {
        throw "Changed public or protected declarations require XML documentation: $($failures -join ', ')"
    }

    Write-Host "Changed public and protected declarations are documented."
}
finally {
    Pop-Location
}
