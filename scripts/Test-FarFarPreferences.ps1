param([string]$OutputFolder=(Join-Path $PSScriptRoot '..\diagnostics\preferences-20261103'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputFolder -Force | Out-Null
& 'C:\BuildTools\VS2022\Common7\Tools\Launch-VsDevShell.ps1' -Arch amd64 -HostArch amd64 -SkipAutomaticLocation
& cl.exe /nologo /EHsc /std:c++20 /W4 (Join-Path $PSScriptRoot '..\tests\farfar_preferences.cpp') "/Fe:$OutputFolder\preferences-tests.exe" "/Fo:$OutputFolder\preferences-tests.obj"
if ($LASTEXITCODE -ne 0) { throw 'Preference serialization test build failed.' }
& (Join-Path $OutputFolder 'preferences-tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Preference serialization checks failed.' }
