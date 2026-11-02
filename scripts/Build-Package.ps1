param([string]$RuntimeFolder = (Join-Path $PSScriptRoot '..\UE5.8'))
$ErrorActionPreference = 'Stop'
$version = '0.4.1'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runtimeRoot = (Resolve-Path -LiteralPath $RuntimeFolder).Path

$packageRoot = Join-Path $projectRoot 'dist\FarFarVR'
if (Test-Path -LiteralPath $packageRoot) { Remove-Item -LiteralPath $packageRoot -Recurse -Force }

$publishRoot = Join-Path $projectRoot 'Launcher\publish'
dotnet publish (Join-Path $projectRoot 'Launcher\FarFarWestVR.csproj') -c Release -r win-x64 -o $publishRoot
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }

New-Item -ItemType Directory -Path (Join-Path $packageRoot 'UE5.8') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $publishRoot 'FarFarWestVR.exe') -Destination (Join-Path $packageRoot 'Far Far VR.exe')

if (Test-Path -LiteralPath (Join-Path $runtimeRoot 'BUILD-SOURCE.txt')) {
    Copy-Item -LiteralPath (Join-Path $runtimeRoot 'BUILD-SOURCE.txt') -Destination (Join-Path $packageRoot 'UE5.8')
}

$runtimeNotice = @"
Far Far VR Runtime
------------------
To respect upstream licensing and copyright boundaries, third-party UEVR runtime
binaries (UEVRBackend.dll, LuaVR.dll, openxr_loader.dll, openvr_api.dll) are not
bundled in this distribution archive.

When you run 'Far Far VR.exe' and click 'START IN VR', the launcher will
automatically download and verify the compatible UEVR runtime package from the
public repository on first setup.

Far Far VR mod by PR_VR (https://www.youtube.com/@PR_VR).
Powered by UEVR created by praydog.
"@
Set-Content -LiteralPath (Join-Path $packageRoot 'UE5.8\README-RUNTIME.txt') -Value $runtimeNotice -Encoding utf8

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
