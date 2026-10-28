param(
    [string]$SourceFolder = (Join-Path $PSScriptRoot '..\vendor\UEVR'),
    [switch]$IncludeFocusedMenuPreview
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$revision = '963b157573eaef20528b602d1d9790b8105819b3'
if (!(Test-Path -LiteralPath $SourceFolder)) {
    git clone --filter=blob:none --no-checkout https://github.com/joeyhodge/UEVR.git $SourceFolder
    if ($LASTEXITCODE -ne 0) { throw 'Engine source download failed.' }
    git -C $SourceFolder checkout --detach $revision
    if ($LASTEXITCODE -ne 0) { throw 'Could not check out the pinned engine revision.' }
}
$actual = git -C $SourceFolder rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $actual -ne $revision) { throw 'Engine source does not match the pinned working revision.' }
function Apply-Patch([string]$Name) {
    $patch = Join-Path $projectRoot "Engine\$Name"
    git -C $SourceFolder apply --reverse --check $patch 2>$null
    if ($LASTEXITCODE -eq 0) { Write-Output "$Name is already applied."; return }
    git -C $SourceFolder apply --check $patch
    if ($LASTEXITCODE -ne 0) { throw "$Name cannot be applied cleanly. Existing source changes were left untouched." }
    git -C $SourceFolder apply $patch
    if ($LASTEXITCODE -ne 0) { throw "Applying $Name failed." }
}
Apply-Patch 'cinema-comfort.patch'
if ($IncludeFocusedMenuPreview) { Apply-Patch 'focused-menu.patch' }
git -C $SourceFolder diff --check
if ($LASTEXITCODE -ne 0) { throw 'Patched source has whitespace errors.' }
Write-Output 'Pinned source prepared. No runtime DLL was built or replaced.'
