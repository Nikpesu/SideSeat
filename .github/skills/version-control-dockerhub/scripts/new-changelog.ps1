[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$Summary = "TODO: Dodaj kratki sazetak izdanja.",

    [string[]]$Added,

    [string[]]$Changed,

    [string[]]$Fixed,

    [string[]]$Security,

    [string[]]$Docker,

    [string]$OutputDirectory = "changelogs",

    [switch]$Force
)

$ErrorActionPreference = "Stop"

$normalizedVersion = $Version.Trim().ToLowerInvariant()
if (-not $normalizedVersion.StartsWith("v")) {
    $normalizedVersion = "v$normalizedVersion"
}

if ($normalizedVersion -notmatch '^v\d+\.\d+(?:\.\d+)?$') {
    throw "Version '$Version' must use vX.Y or vX.Y.Z format."
}

$outputRoot = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDirectory))
$repositoryRoot = [System.IO.Path]::GetFullPath((Get-Location).Path)
if (-not $outputRoot.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Changelog output must stay inside the repository."
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$outputPath = Join-Path $outputRoot "$normalizedVersion.md"
if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw "Changelog already exists: $outputPath. Use -Force to overwrite it."
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# SideSeat $normalizedVersion")
$lines.Add("")
$lines.Add("Datum izdanja: $(Get-Date -Format 'yyyy-MM-dd')")
$lines.Add("")
$lines.Add($Summary.Trim())

function Add-Section {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [string[]]$Items
    )

    $validItems = @($Items | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($validItems.Count -eq 0) {
        return
    }

    $lines.Add("")
    $lines.Add("## $Title")
    $lines.Add("")
    foreach ($item in $validItems) {
        $lines.Add("- $($item.Trim())")
    }
}

Add-Section -Title "Added" -Items $Added
Add-Section -Title "Changed" -Items $Changed
Add-Section -Title "Fixed" -Items $Fixed
Add-Section -Title "Security" -Items $Security
Add-Section -Title "Docker" -Items $Docker

Set-Content -LiteralPath $outputPath -Value $lines -Encoding utf8
Write-Host "Created changelog: $outputPath"
