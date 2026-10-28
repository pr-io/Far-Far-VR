param([string]$BuildToolsFolder = '')
$ErrorActionPreference = 'Stop'
$BuildToolsFolder = & (Join-Path $PSScriptRoot 'Find-VS2022.ps1') -Path $BuildToolsFolder
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& (Join-Path $BuildToolsFolder 'Common7\Tools\Launch-VsDevShell.ps1') -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$cmake = Join-Path $BuildToolsFolder 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
if (!(Test-Path -LiteralPath $cmake)) { throw 'CMake component is not installed yet.' }
& $cmake --version
if ($LASTEXITCODE -ne 0) { throw 'CMake failed its version check.' }
$outputFolder = Join-Path $projectRoot 'diagnostics\toolchain'
New-Item -ItemType Directory -Path $outputFolder -Force | Out-Null
$executable = Join-Path $outputFolder 'ToolchainSmoke.exe'
& cl.exe /nologo /EHsc /std:c++latest ("/Fe:" + $executable) ("/Fo:" + (Join-Path $outputFolder 'ToolchainSmoke.obj')) (Join-Path $projectRoot 'tests\ToolchainSmoke.cpp')
if ($LASTEXITCODE -ne 0) { throw 'C++ compiler or Windows SDK compile/link check failed.' }
& $executable
if ($LASTEXITCODE -ne 0) { throw 'Compiled toolchain check failed to run.' }
