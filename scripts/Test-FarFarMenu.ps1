param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\diagnostics\unified-menu-20261030'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
& 'C:\BuildTools\VS2022\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
& cl.exe /nologo /EHsc /std:c++20 /W4 (Join-Path $PSScriptRoot '..\tests\farfar_menu.cpp') "/Fe:$OutputFolder\menu-tests.exe" "/Fo:$OutputFolder\menu-tests.obj"
if ($LASTEXITCODE -ne 0) { throw 'Menu lifecycle test build failed.' }
& (Join-Path $OutputFolder 'menu-tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Menu lifecycle checks failed.' }
