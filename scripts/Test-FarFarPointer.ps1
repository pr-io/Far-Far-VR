param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\diagnostics\menu-pointer-20261030\native-pointer'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
& 'C:\BuildTools\VS2022\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
$taskTest=Join-Path $PSScriptRoot '..\tests\farfar_pointer.cpp'
$taskExe=Join-Path $OutputFolder 'pointer.exe'
$taskObj=Join-Path $OutputFolder 'pointer.obj'
$glm=if(Test-Path(Join-Path $PSScriptRoot "..\vendor\UEVR\dependencies\submodules\glm")){Join-Path $PSScriptRoot "..\vendor\UEVR\dependencies\submodules\glm"}else{Join-Path $PSScriptRoot "..\..\UEVR\vendor\UEVR\dependencies\submodules\glm"}
& cl.exe /nologo /EHsc /std:c++20 /W4 ("/I" + $glm) $taskTest "/Fe:$taskExe" "/Fo:$taskObj"
if($LASTEXITCODE -ne 0){throw 'Pointer policy test build failed'}
& $taskExe
if($LASTEXITCODE -ne 0){throw 'Pointer policy tests failed'}
