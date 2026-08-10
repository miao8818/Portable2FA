param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin')
)

$ErrorActionPreference = 'Stop'
$versionPath = Join-Path $PSScriptRoot 'version.json'
if (-not (Test-Path -LiteralPath $versionPath)) {
    throw 'version.json was not found.'
}

$versionData = Get-Content -LiteralPath $versionPath -Raw | ConvertFrom-Json
$version = [string]$versionData.version
$updatedAt = [string]$versionData.updatedAt
if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'version.json must contain a three-part semantic version.'
}
if ([string]::IsNullOrWhiteSpace($updatedAt)) {
    throw 'version.json must contain updatedAt.'
}

$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'Windows C# compiler was not found.'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$resourceDirectory = Join-Path $PSScriptRoot 'Resources'
New-Item -ItemType Directory -Force -Path $resourceDirectory | Out-Null
$zxingDll = Join-Path $PSScriptRoot 'ThirdParty\ZXing.Net\zxing.dll'
if (-not (Test-Path -LiteralPath $zxingDll)) {
    throw 'ThirdParty\ZXing.Net\zxing.dll was not found.'
}
$windowsMetadata = 'C:\Windows\System32\WinMetadata\Windows.Security.winmd'
$windowsFoundationMetadata = 'C:\Windows\System32\WinMetadata\Windows.Foundation.winmd'
$windowsRuntime = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.WindowsRuntime.dll'
if (-not (Test-Path -LiteralPath $windowsRuntime)) {
    $windowsRuntime = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Runtime.WindowsRuntime.dll'
}
$windowsInteropRuntime = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.InteropServices.WindowsRuntime.dll'
if (-not (Test-Path -LiteralPath $windowsInteropRuntime)) {
    $windowsInteropRuntime = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Runtime.InteropServices.WindowsRuntime.dll'
}
$systemRuntime = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Runtime\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Runtime.dll'
$winRtReferences = @($windowsMetadata, $windowsFoundationMetadata,
    $windowsRuntime, $windowsInteropRuntime, $systemRuntime)
foreach ($reference in $winRtReferences) {
    if (-not (Test-Path -LiteralPath $reference)) {
        throw ('Windows Credential Locker reference was not found: ' + $reference)
    }
}
$toolsDirectory = Join-Path $PSScriptRoot 'bin\tools'
New-Item -ItemType Directory -Force -Path $toolsDirectory | Out-Null
$generatedDirectory = Join-Path $PSScriptRoot 'bin\generated'
New-Item -ItemType Directory -Force -Path $generatedDirectory | Out-Null

$generatedVersion = Join-Path $generatedDirectory 'GeneratedVersion.cs'
$assemblyVersion = $version + '.0'
$assemblyVersionLine = '[assembly: AssemblyVersion("{0}")]' -f $assemblyVersion
$fileVersionLine = '[assembly: AssemblyFileVersion("{0}")]' -f $assemblyVersion
$informationalVersionLine = '[assembly: AssemblyInformationalVersion("{0}")]' -f $version
$versionConstantLine = '        public const string Version = "{0}";' -f $version
$updatedAtConstantLine = '        public const string UpdatedAt = "{0}";' -f $updatedAt
$generatedLines = @(
    'using System.Reflection;',
    $assemblyVersionLine,
    $fileVersionLine,
    $informationalVersionLine,
    'namespace Portable2FA',
    '{',
    '    internal static class BuildInfo',
    '    {',
    $versionConstantLine,
    $updatedAtConstantLine,
    '    }',
    '}'
)
$generatedText = $generatedLines -join [Environment]::NewLine
[System.IO.File]::WriteAllText($generatedVersion, $generatedText,
    (New-Object System.Text.UTF8Encoding($false)))

$iconMaker = Join-Path $toolsDirectory 'IconMaker.exe'
& $compiler /nologo /target:exe /optimize+ `
    /reference:System.dll /reference:System.Drawing.dll `
    /out:$iconMaker (Join-Path $PSScriptRoot 'IconMaker.cs')
if ($LASTEXITCODE -ne 0) { throw 'Icon generator compilation failed.' }

& $iconMaker $resourceDirectory
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }

$outputExe = Join-Path $OutputDirectory 'Portable2FA.exe'
$appIcon = Join-Path $resourceDirectory 'app.ico'
$trayIcon = Join-Path $resourceDirectory 'tray.ico'
$manifest = Join-Path $PSScriptRoot 'app.manifest'

& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:$appIcon `
    /win32manifest:$manifest /resource:$trayIcon,Portable2FA.TrayIcon `
    /resource:$zxingDll,Portable2FA.Dependencies.zxing.dll `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Security.dll /reference:System.Web.Extensions.dll `
    /reference:System.Windows.Forms.dll /reference:$zxingDll `
    /reference:$windowsMetadata /reference:$windowsFoundationMetadata `
    /reference:$windowsRuntime /reference:$windowsInteropRuntime `
    /reference:$systemRuntime `
    /out:$outputExe `
    (Join-Path $PSScriptRoot 'Program.cs') `
    (Join-Path $PSScriptRoot 'MainForm.cs') `
    (Join-Path $PSScriptRoot 'Controls.cs') `
    (Join-Path $PSScriptRoot 'Totp.cs') `
    (Join-Path $PSScriptRoot 'QrCodeDecoder.cs') `
    (Join-Path $PSScriptRoot 'SavedAccount.cs') `
    (Join-Path $PSScriptRoot 'VaultStore.cs') `
    (Join-Path $PSScriptRoot 'AppSettings.cs') `
    (Join-Path $PSScriptRoot 'WindowsCredentialSync.cs') `
    (Join-Path $PSScriptRoot 'AccountDialog.cs') `
    (Join-Path $PSScriptRoot 'SettingsForm.cs') `
    (Join-Path $PSScriptRoot 'HotkeyTextBox.cs') `
    (Join-Path $PSScriptRoot 'ScreenCaptureForm.cs') `
    $generatedVersion
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }

$testExe = Join-Path $toolsDirectory 'TestHarness.exe'
& $compiler /nologo /target:exe /optimize+ /reference:$outputExe `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Web.Extensions.dll /reference:$zxingDll /out:$testExe `
    (Join-Path $PSScriptRoot 'TestHarness.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test harness compilation failed.' }

Copy-Item -LiteralPath $outputExe -Destination (Join-Path $toolsDirectory 'Portable2FA.exe') -Force
Copy-Item -LiteralPath $zxingDll -Destination (Join-Path $toolsDirectory 'zxing.dll') -Force
Push-Location $toolsDirectory
try {
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'TOTP tests failed.' }
}
finally {
    Pop-Location
}

$item = Get-Item -LiteralPath $outputExe
Write-Host ('Built: {0} v{1}, updated {2} ({3:N0} bytes)' -f `
    $item.FullName, $version, $updatedAt, $item.Length)
