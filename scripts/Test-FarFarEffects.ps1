param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\diagnostics\pov-effects-20261030'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
& 'C:\BuildTools\VS2022\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
& cl.exe /nologo /EHsc /std:c++20 /W4 (Join-Path $PSScriptRoot '..\tests\farfar_effects.cpp') "/Fe:$OutputFolder\effects-tests.exe" "/Fo:$OutputFolder\effects-tests.obj"
if ($LASTEXITCODE -ne 0) { throw 'Effects FOV test build failed.' }
& (Join-Path $OutputFolder 'effects-tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Effects FOV checks failed.' }
