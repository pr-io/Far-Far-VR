param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\diagnostics\memory-probe-tests'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
& 'C:\BuildTools\VS2022\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$source=Join-Path $PSScriptRoot '..\tests\farfar_memory_probe.cpp'
$target=Join-Path $OutputFolder 'memory-probe.exe'
& cl.exe /nologo /EHsc /std:c++20 $source "/Fe:$target" ("/Fo:"+(Join-Path $OutputFolder 'memory-probe.obj'))
if($LASTEXITCODE -ne 0){throw 'Memory validation test build failed'}
& $target
if($LASTEXITCODE -ne 0){throw 'Memory validation tests failed'}
