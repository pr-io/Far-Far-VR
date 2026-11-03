param(
    [string]$SourceFolder = (Join-Path $PSScriptRoot '..\vendor\UEVR'),
    [string]$BuildFolder = (Join-Path $PSScriptRoot '..\vendor\UEVR-build'),
    [string]$BuildToolsFolder = 'C:\BuildTools\VS2022'
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $SourceFolder).Path
$expected = '963b157573eaef20528b602d1d9790b8105819b3'
$revision = git -C $sourceRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $revision -ne $expected) { throw 'Source must match the pinned Far Far West compatibility revision.' }
$sdkRoot = Join-Path $sourceRoot 'dependencies\submodules\UESDK'
if (!(Test-Path -LiteralPath (Join-Path $sdkRoot CMakeLists.txt))) {
    throw 'UESDK source is unavailable. Link GitHub to Epic Games, accept the organization invitation, authenticate local Git, then run git submodule update --init --recursive inside vendor/UEVR. See Engine/README.md.'
}
$sdkRevision = git -C $sdkRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $sdkRevision -ne '811b01498b61e6a0246615b4f2b4fb4f4067fb64') { throw 'UESDK is not at the pinned compatible revision.' }
# Use the exact pinned Tracy source archive rather than fetching its entire history.
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$depsFolder = Join-Path $projectRoot 'tools\deps'
$tracyRevision = '897aec5b062664d2485f4f9a213715d2e527e0ca'
$tracySource = Join-Path $depsFolder ("tracy-" + $tracyRevision)
$tracyArchive = Join-Path $depsFolder 'tracy-897aec5.zip'
New-Item -ItemType Directory -Path $depsFolder -Force | Out-Null
if (!(Test-Path -LiteralPath $tracyArchive)) { Invoke-WebRequest -Uri "https://codeload.github.com/wolfpld/tracy/zip/$tracyRevision" -OutFile $tracyArchive }
if ((Get-FileHash -LiteralPath $tracyArchive).Hash -ne '386C60CBD00518C7B8123377F8D801C096817E096A4675F8FB52DB28B337053D') { throw 'Pinned Tracy archive checksum mismatch.' }
if (!(Test-Path -LiteralPath (Join-Path $tracySource 'CMakeLists.txt'))) { Expand-Archive -LiteralPath $tracyArchive -DestinationPath $depsFolder }
# Avoid cloning the JSON repository's complete test-data history.
$jsonRevision = 'bc889afb4c5bf1c0d8ee29ef35eaaf4c8bef8a5d'
$jsonSource = Join-Path $depsFolder ('json-' + $jsonRevision)
$jsonArchive = Join-Path $depsFolder 'json-bc889afb.zip'
if (!(Test-Path -LiteralPath $jsonArchive)) { Invoke-WebRequest -Uri "https://codeload.github.com/nlohmann/json/zip/$jsonRevision" -OutFile $jsonArchive }
if ((Get-FileHash -LiteralPath $jsonArchive).Hash -ne '6CF18437941925B6346566AD832A1CA6ADB5CDE4A851B3E77D946065375093BD') { throw 'Pinned JSON archive checksum mismatch.' }
if (!(Test-Path -LiteralPath (Join-Path $jsonSource 'CMakeLists.txt'))) { Expand-Archive -LiteralPath $jsonArchive -DestinationPath $depsFolder }
$shell = Join-Path $BuildToolsFolder 'Common7\Tools\Launch-VsDevShell.ps1'
if (!(Test-Path -LiteralPath $shell)) { throw 'Visual Studio 2022 C++ Build Tools were not found.' }
& $shell -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$cmake = Join-Path $BuildToolsFolder 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
if (!(Test-Path -LiteralPath $cmake)) { throw 'CMake component was not found in Build Tools.' }
& $cmake -S $sourceRoot -B $BuildFolder -G 'Visual Studio 17 2022' -A x64 -DCMAKE_BUILD_TYPE=Release -DUEVR_BUILD_COMPATIBILITY_TESTS=ON ("-DFETCHCONTENT_SOURCE_DIR_TRACY=" + $tracySource) ("-DFETCHCONTENT_SOURCE_DIR_JSON=" + $jsonSource)
if ($LASTEXITCODE -ne 0) { throw 'CMake configuration failed; existing runtime remains untouched.' }
# Validate shader compilation in the build directory, while retaining the pinned
# checked-in shader bytecode used by the accepted rendering source.
$env:CompileShadersOutput = [IO.Path]::GetFullPath((Join-Path $BuildFolder 'shader-validation'))
& $cmake --build $BuildFolder --config Release --target uevr LuaVR
if ($LASTEXITCODE -ne 0) { throw 'Backend build failed; existing runtime remains untouched.' }
Write-Output "Experimental build created under $BuildFolder. Runtime promotion requires separate validation."
