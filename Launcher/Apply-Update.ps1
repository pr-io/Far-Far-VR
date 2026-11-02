param(
    [Parameter(Mandatory = $true)][string]$InstallRoot,
    [Parameter(Mandatory = $true)][string]$StagingRoot,
    [Parameter(Mandatory = $true)][int]$WaitForProcessId
)
$ErrorActionPreference = 'Stop'
$install = [IO.Path]::GetFullPath($InstallRoot)
$staging = [IO.Path]::GetFullPath($StagingRoot)
$pending = [IO.Path]::GetFullPath((Join-Path $staging '..'))
$marker = Join-Path $pending 'prepared.txt'
$version = [IO.File]::ReadAllText($marker).Trim()
if (!$version) { $version = 'pending' }
$profile = Join-Path $env:APPDATA 'UnrealVRMod\FarFarWest-Win64-Shipping'
$statusPath = Join-Path $profile 'farfarvr-updates.txt'
$changes = [Collections.Generic.List[object]]::new()

function Write-UpdateStatus([string]$State, [string]$Message) {
    New-Item -ItemType Directory -Path $profile -Force | Out-Null
    $temporary = $statusPath + '.pending'
    [IO.File]::WriteAllText($temporary, "state=$State`nversion=$version`nnotes:`n$Message")
    Move-Item -LiteralPath $temporary -Destination $statusPath -Force
}

if (!(Test-Path -LiteralPath $marker -PathType Leaf) -or !(Test-Path -LiteralPath (Join-Path $staging 'Far Far VR.exe') -PathType Leaf)) {
    throw 'The staged Far Far VR package is incomplete.'
}
while (Get-Process -Id $WaitForProcessId -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 500 }
while (Get-Process -Name 'FarFarWest-Win64-Shipping' -ErrorAction SilentlyContinue) { Start-Sleep -Seconds 2 }

$sourceRoot = $staging.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$targetRoot = $install.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
try {
    foreach ($file in Get-ChildItem -LiteralPath $staging -File -Recurse) {
        $relative = $file.FullName.Substring($sourceRoot.Length)
        $source = [IO.Path]::GetFullPath($file.FullName)
        $target = [IO.Path]::GetFullPath((Join-Path $install $relative))
        if (!$source.StartsWith($sourceRoot, [StringComparison]::OrdinalIgnoreCase) -or
            !$target.StartsWith($targetRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'The staged package contains an invalid file path.'
        }
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
        $temporary = $target + '.farfarvr-new-' + [Guid]::NewGuid().ToString('N')
        $backup = $target + '.farfarvr-backup-' + [Guid]::NewGuid().ToString('N')
        [IO.File]::Copy($source, $temporary, $false)
        if ([IO.File]::Exists($target)) {
            [IO.File]::Replace($temporary, $target, $backup)
            $changes.Add([pscustomobject]@{ Target = $target; Backup = $backup; Created = $false })
        } else {
            [IO.File]::Move($temporary, $target)
            $changes.Add([pscustomobject]@{ Target = $target; Backup = ''; Created = $true })
        }
    }
    Write-UpdateStatus 'applied' 'Update installed. Far Far VR is ready.'
    [IO.File]::Delete($marker)
} catch {
    for ($index = $changes.Count - 1; $index -ge 0; $index--) {
        $change = $changes[$index]
        if ($change.Created) {
            if ([IO.File]::Exists($change.Target)) { [IO.File]::Delete($change.Target) }
        } elseif ([IO.File]::Exists($change.Backup)) {
            [IO.File]::Copy($change.Backup, $change.Target, $true)
            [IO.File]::Delete($change.Backup)
        }
    }
    Write-UpdateStatus 'error' ("Update could not be installed: " + $_.Exception.Message)
    throw
} finally {
    Get-ChildItem -LiteralPath $install -Filter '*.farfarvr-new-*' -File -Recurse -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue
}

$backupErrors = [Collections.Generic.List[string]]::new()
foreach ($change in $changes) {
    if (!$change.Created -and [IO.File]::Exists($change.Backup)) {
        try { [IO.File]::Delete($change.Backup) }
        catch { $backupErrors.Add($change.Target) }
    }
}
if ($backupErrors.Count -gt 0) {
    Write-UpdateStatus 'applied' 'Update installed. Some rollback backups could not be removed.'
}
try { Start-Process -FilePath (Join-Path $install 'Far Far VR.exe') }
catch {
    Write-UpdateStatus 'applied' 'Update installed. Start Far Far VR.exe manually to continue.'
    throw
}
