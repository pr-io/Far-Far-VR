param([string]$ProfileFolder = (Join-Path $env:APPDATA 'UnrealVRMod\FarFarWest-Win64-Shipping'))
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'FarFarWest-Win64-Shipping' -ErrorAction SilentlyContinue) { throw 'Close the game before installing the menu module.' }
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!(Test-Path -LiteralPath (Join-Path $ProfileFolder 'config.txt'))) { throw 'No existing game profile found. Start the launcher once before installing the menu module.' }
$scriptsFolder = Join-Path $ProfileFolder 'scripts'
New-Item -ItemType Directory -Path $scriptsFolder -Force | Out-Null
foreach ($name in @('farfarvr_settings.lua','farfarvr_menu.lua','farfarvr_input.lua','farfarvr_hud.lua','farfarvr_objectives.lua','farfarvr_comfort.lua','farfarvr_mount.lua')) {
    $target = Join-Path $scriptsFolder $name
    if (Test-Path -LiteralPath $target) { Copy-Item -LiteralPath $target -Destination ($target + '.backup-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')) }
    Copy-Item -LiteralPath (Join-Path $projectRoot "Profile\scripts\$name") -Destination $target
}
Write-Output 'Menu module installed. On the original engine: open UEVR menu, enable Show Advanced Options, then choose LuaLoader > Far Far VR.'
