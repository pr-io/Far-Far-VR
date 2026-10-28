param(
    [string]$RuntimeFolder = (Join-Path $PSScriptRoot '..\runtime\UE5.8'),
    [switch]$AllowRuntimeRedistribution
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runtimePath = [IO.Path]::GetFullPath($RuntimeFolder)
if (!(Test-Path -LiteralPath $runtimePath -PathType Container)) {
    throw "A complete, license-cleared runtime was not found at '$runtimePath'. Stock UEVR downloads are incompatible with this mod."
}
if (!$AllowRuntimeRedistribution) {
    throw 'Runtime redistribution is not authorized by the recorded license terms. Obtain written permission before building a player package with runtime binaries.'
}
$runtimeRoot = (Resolve-Path -LiteralPath $runtimePath).Path
$expected = '240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73'
$backendPath = Join-Path $runtimeRoot 'UEVRBackend.dll'
if (!(Test-Path -LiteralPath $backendPath)) { throw 'The required custom UEVRBackend.dll is missing.' }
$actual = (Get-FileHash -LiteralPath $backendPath -Algorithm SHA256).Hash
if ($actual -ne $expected) { throw "Unexpected backend hash '$actual'. Required: '$expected'. Stock or stale builds are rejected." }
$runtimeFiles = @('UEVRBackend.dll', 'LuaVR.dll', 'openxr_loader.dll', 'openvr_api.dll', 'BUILD-SOURCE.txt')
foreach ($name in $runtimeFiles) {
    if (!(Test-Path -LiteralPath (Join-Path $runtimeRoot $name) -PathType Leaf)) { throw "Runtime file missing: $name" }
}

$packageRoot = Join-Path $projectRoot 'dist\FarFarVR'
$archive = Join-Path $projectRoot 'dist\FarFarVR-standalone.zip'
if (Test-Path -LiteralPath $packageRoot) { throw 'Package output already exists. Move it aside before building a fresh package.' }
if (Test-Path -LiteralPath $archive) { throw 'Package archive already exists. Move it aside before building a fresh package.' }
$publishRoot = Join-Path $projectRoot 'Launcher\publish'
dotnet publish (Join-Path $projectRoot 'Launcher\FarFarWestVR.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishRoot
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }

New-Item -ItemType Directory -Path (Join-Path $packageRoot 'UE5.8') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $publishRoot 'FarFarWestVR.exe') -Destination (Join-Path $packageRoot 'Far Far VR.exe')
foreach ($name in $runtimeFiles) {
    Copy-Item -LiteralPath (Join-Path $runtimeRoot $name) -Destination (Join-Path $packageRoot 'UE5.8')
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Profile') -Destination $packageRoot -Recurse
foreach ($name in @('README.md', 'THIRD_PARTY.md', 'RECOVERY.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $packageRoot
}
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'Engine') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'Engine\VALIDATION.md') -Destination (Join-Path $packageRoot 'Engine')
Get-ChildItem -LiteralPath $packageRoot -Recurse -File | ForEach-Object {
    [PSCustomObject]@{ File = [IO.Path]::GetRelativePath($packageRoot, $_.FullName); SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding utf8
& (Join-Path $PSScriptRoot 'Test-Package.ps1') -PackageFolder $packageRoot
Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archive -CompressionLevel Optimal
if (!(Test-Path -LiteralPath $archive)) { throw 'Package archive was not created.' }
Write-Output "Player package staged: $packageRoot"
Write-Output "Release archive: $archive"
