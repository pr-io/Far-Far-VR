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

foreach ($name in @('UEVRBackend.dll', 'LuaVR.dll', 'openxr_loader.dll', 'openvr_api.dll', 'BUILD-SOURCE.txt')) {
    $src = Join-Path $runtimeRoot $name
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $packageRoot 'UE5.8')
    }
}

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

Write-Output "Complete release package staged: $packageRoot"
Write-Output "Clean release zip created: $zipPath"
