param(
    [string]$BuildFolder = (Join-Path $PSScriptRoot '..\vendor\UEVR-build'),
    [string]$BuildToolsFolder = 'C:\BuildTools\VS2022'
)
$ErrorActionPreference = 'Stop'
& (Join-Path $BuildToolsFolder 'Common7\Tools\Launch-VsDevShell.ps1') -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$cmake = Join-Path $BuildToolsFolder 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
$ctest = Join-Path $BuildToolsFolder 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\ctest.exe'
$targets = @(
    'compatibility-policy-tests', 'steam-frame-input-tests', 'stock-frame-migration-tests',
    'gamepad-recovery-tests', 'kananlib-decode-window-tests', 'diagnostic-safety-tests',
    'hifi-hook-memory-tests', 'gears-eday-aim-tests', 'nascar-hook-compatibility-tests',
    'input-hook-installation-tests', 'lua-fname-binding-tests', 'lua-context-dispatch-tests',
    'lua-controller-input-tests', 'stalker2-native-policy-tests', 'stalker2-native-d3d12-tests',
    'galactic-racer-letterboxing-tests'
)
& $cmake --build $BuildFolder --config Release --target @targets --parallel 2
if ($LASTEXITCODE -ne 0) { throw 'Upstream offline test build failed.' }
& $ctest --test-dir $BuildFolder -C Release --output-on-failure --no-tests=error
if ($LASTEXITCODE -ne 0) { throw 'Upstream offline checks failed. Do not promote the experimental runtime.' }
