param(
    [ValidateSet('patch', 'minor', 'major')]
    [string]$Part = 'patch',
    [string]$Version = ''
)

$ErrorActionPreference = 'Stop'
$versionPath = Join-Path $PSScriptRoot 'version.json'
if (-not (Test-Path -LiteralPath $versionPath)) {
    throw 'version.json was not found.'
}

$current = Get-Content -LiteralPath $versionPath -Raw | ConvertFrom-Json
$currentVersion = [string]$current.version
if ($currentVersion -notmatch '^(\d+)\.(\d+)\.(\d+)$') {
    throw 'Current version must use major.minor.patch format.'
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $patch = [int]$Matches[3]

    switch ($Part) {
        'major' { $major++; $minor = 0; $patch = 0 }
        'minor' { $minor++; $patch = 0 }
        'patch' { $patch++ }
    }
    $Version = '{0}.{1}.{2}' -f $major, $minor, $patch
}
elseif ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Version must use major.minor.patch format.'
}

$updatedAt = Get-Date -Format 'yyyy-MM-ddTHH:mm:sszzz'
$metadata = [ordered]@{
    version = $Version
    updatedAt = $updatedAt
}
$json = $metadata | ConvertTo-Json
$jsonWithNewline = $json + [Environment]::NewLine
[System.IO.File]::WriteAllText($versionPath, $jsonWithNewline,
    (New-Object System.Text.UTF8Encoding($false)))

Write-Host ('Version updated: {0} -> {1}' -f $currentVersion, $Version)
Write-Host ('Updated at: {0}' -f $updatedAt)
Write-Host 'Next: update CHANGELOG.md, run build.ps1, commit, tag, and publish the release.'
