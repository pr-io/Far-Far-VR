param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\diagnostics\regression-repair-20261028'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
& 'C:\BuildTools\VS2022\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
& cl.exe /nologo /EHsc /std:c++20 /W4 (Join-Path $PSScriptRoot '..\tests\farfar_runtime.cpp') "/Fe:$OutputFolder\runtime-tests.exe" "/Fo:$OutputFolder\runtime-tests.obj"
if ($LASTEXITCODE -ne 0) { throw 'Runtime regression test build failed.' }
& (Join-Path $OutputFolder 'runtime-tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Runtime regression checks failed.' }
