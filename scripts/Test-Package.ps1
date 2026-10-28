param([Parameter(Mandatory = $true)][string]$PackageFolder)
$ErrorActionPreference = 'Stop'
$packageRoot = (Resolve-Path -LiteralPath $PackageFolder).Path
$required = @(
    'Far Far VR.exe',
    'README.md',
    'THIRD_PARTY.md',
    'RECOVERY.md',
    'manifest.json',
    'UE5.8\UEVRBackend.dll',
    'UE5.8\LuaVR.dll',
    'UE5.8\openxr_loader.dll',
    'UE5.8\openvr_api.dll',
    'UE5.8\BUILD-SOURCE.txt',
    'Profile\config.txt',
    'Profile\cameras.txt',
    'Profile\VRSettings.json',
    'Profile\_interaction_profiles_oculus_touch_controller.json',
    'Profile\scripts\farfarvr_settings.lua',
    'Profile\scripts\farfarvr_menu.lua',
    'Profile\scripts\farfarvr_input.lua',
    'Profile\scripts\farfarvr_hud.lua',
    'Profile\scripts\farfarvr_objectives.lua',
    'Profile\scripts\farfarvr_comfort.lua',
    'Profile\scripts\farfarvr_mount.lua',
    'Profile\scripts\farfarvr_render_fix.lua'
)
foreach ($relative in $required) {
    if (!(Test-Path -LiteralPath (Join-Path $packageRoot $relative) -PathType Leaf)) {
        throw "Package file missing: $relative"
    }
}

$backendHash = (Get-FileHash -LiteralPath (Join-Path $packageRoot 'UE5.8\UEVRBackend.dll') -Algorithm SHA256).Hash
if ($backendHash -ne '240C5ACCB5A1E02862101A5809B10C22D9403A1B5ED1A3095DDF4CB29BA5EC73') {
    throw "Package backend hash is incompatible: $backendHash"
}

$files = Get-ChildItem -LiteralPath $packageRoot -Recurse -File
$unexpected = $files | Where-Object {
    $_.Extension -in @('.log', '.sav', '.pdb', '.pyc', '.cache', '.tmp') -or
    $_.Name -match '(?i)(UEVRInjector|FarFarWest\.exe|clean_backup)'
}
if ($unexpected) { throw "Package contains excluded or unrelated files: $($unexpected.FullName -join ', ')" }
$badDirectories = Get-ChildItem -LiteralPath $packageRoot -Recurse -Directory | Where-Object {
    $_.Name -in @('.git', 'bin', 'obj', '__pycache__', 'node_modules', 'Paks', 'Saved')
}
if ($badDirectories) { throw "Package contains excluded directories: $($badDirectories.FullName -join ', ')" }

$exe = Join-Path $packageRoot 'Far Far VR.exe'
$process = Start-Process -FilePath $exe -ArgumentList '--verify-package' -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Packaged launcher validation failed (exit code $($process.ExitCode))." }
Write-Output "Package content, runtime hash and clean-launcher verification passed: $packageRoot"
