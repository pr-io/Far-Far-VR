param([string]$RuntimeFolder = (Join-Path $PSScriptRoot '..\UE5.8'))
$ErrorActionPreference = 'Stop'
$version = '0.4.3'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runtimeRoot = [IO.Path]::GetFullPath($RuntimeFolder)

$packageRoot = Join-Path $projectRoot 'dist\FarFarVR'
if (Test-Path -LiteralPath $packageRoot) { Remove-Item -LiteralPath $packageRoot -Recurse -Force }

$publishRoot = Join-Path $projectRoot 'Launcher\publish'
dotnet publish (Join-Path $projectRoot 'Launcher\FarFarWestVR.csproj') -c Release -r win-x64 -o $publishRoot
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }

New-Item -ItemType Directory -Path (Join-Path $packageRoot 'UE5.8') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $publishRoot 'FarFarWestVR.exe') -Destination (Join-Path $packageRoot 'Far Far VR.exe')

$runtimeNotice = @"
Far Far VR Runtime
------------------
To respect upstream licensing and copyright boundaries, third-party UEVR runtime
binaries (UEVRBackend.dll, LuaVR.dll, openxr_loader.dll, openvr_api.dll) are not
bundled in this distribution archive.

When you run 'Far Far VR.exe' and click 'START IN VR', the launcher will
download and verify the pinned public UEVR base runtime.

The launcher applies the packaged Far Far VR delta locally using Windows'
built-in patching API, then verifies that the result exactly matches the
tested custom engine. No compiler or manual runtime copying is required.

A delta is not a license exemption. Upstream retains its rights; this package
must not be described as granting engine redistribution or commercial rights.

Far Far VR mod by PR_VR (https://www.youtube.com/@PR_VR).
Powered by UEVR created by praydog.
"@
Set-Content -LiteralPath (Join-Path $packageRoot 'UE5.8\README-RUNTIME.txt') -Value $runtimeNotice -Encoding utf8

$patchSource = Join-Path $projectRoot 'Runtime\backend.ffvrdelta'
if (!(Test-Path -LiteralPath $patchSource)) { throw 'The verified runtime patch is missing.' }
$patchHash = (Get-FileHash -LiteralPath $patchSource).Hash
if ($patchHash -ne 'EC35A59458BBD782DC625031D38B333ECA1CBA157DAD4251A1CEFDC36D7B7716') { throw 'The runtime patch does not match the tested engine.' }
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'Runtime') -Force | Out-Null
Copy-Item -LiteralPath $patchSource -Destination (Join-Path $packageRoot 'Runtime\backend.ffvrdelta')

$profileSource = Join-Path $projectRoot 'Profile'
$profileDest = Join-Path $packageRoot 'Profile'
New-Item -ItemType Directory -Path $profileDest -Force | Out-Null

Get-ChildItem -LiteralPath $profileSource -File -Recurse |
    Where-Object { $_.Extension -ne '.md' } |
    ForEach-Object {
        $relative = $_.FullName.Substring($profileSource.Length).TrimStart('\', '/')
        $destination = Join-Path $profileDest $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $destination
    }

foreach ($name in @('README.md', 'THIRD_PARTY.md', 'ROADMAP.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $packageRoot
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $packageRoot -Recurse
Copy-Item -LiteralPath (Join-Path $projectRoot 'Launcher\Apply-Update.ps1') -Destination (Join-Path $packageRoot 'Apply-Update.ps1')
Copy-Item -LiteralPath (Join-Path $packageRoot 'Far Far VR.exe') -Destination (Join-Path $projectRoot 'Far Far VR.exe') -Force
Set-Content -LiteralPath (Join-Path $packageRoot 'version.txt') -Value $version -Encoding ascii

# Match the documented upstream-download distribution route.
$excludedRuntime = @('UEVRBackend.dll','LuaVR.dll','openxr_loader.dll','openvr_api.dll')
$unexpectedRuntime = Get-ChildItem -LiteralPath $packageRoot -Recurse -File | Where-Object { $_.Name -in $excludedRuntime }
if ($unexpectedRuntime) { throw 'Player package contains excluded third-party runtime binaries.' }

$fileEntries = Get-ChildItem -LiteralPath $packageRoot -Recurse -File | ForEach-Object {
    [PSCustomObject]@{ File = $_.FullName.Substring($packageRoot.TrimEnd("\").Length + 1); SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
}

$manifest = [ordered]@{
    product = "Far Far VR"
    author = "PR_VR"
    company = "PR_VR"
    version = $version
    framework = "Powered by UEVR created by praydog"
    files = $fileEntries
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding utf8

$zipPath = Join-Path $projectRoot "dist\FarFarVR-v$version.zip"
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($packageRoot, $zipPath)

Write-Output "Release package staged without third-party DLLs: $packageRoot"
Write-Output "Clean release zip created: $zipPath"
