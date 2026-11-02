param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\diagnostics\controls-head-20261028\native-controls'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
& 'C:\BuildTools\VS2022\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$taskTest=Join-Path $PSScriptRoot '..\tests\farfar_controls.cpp'
$taskExe=Join-Path $OutputFolder 'controls.exe'
$taskObj=Join-Path $OutputFolder 'controls.obj'
& cl.exe /nologo /EHsc /std:c++20 /W4 $taskTest "/Fe:$taskExe" "/Fo:$taskObj"
if($LASTEXITCODE -ne 0){throw 'Controller policy test build failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'Controller policy tests failed'}
