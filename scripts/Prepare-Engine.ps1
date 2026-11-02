param([string]$SourceFolder = (Join-Path $PSScriptRoot '..\vendor\UEVR'))
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
$patch = Join-Path $projectRoot 'Engine\cinema-comfort.patch'
git -C $SourceFolder apply --reverse --check $patch 2>$null
if ($LASTEXITCODE -eq 0) { Write-Output 'Far Far VR production patch already applied.'; exit 0 }
git -C $SourceFolder apply --check $patch
if ($LASTEXITCODE -ne 0) { throw 'Production patch cannot be applied cleanly. Existing source changes were left untouched.' }
git -C $SourceFolder apply $patch
if ($LASTEXITCODE -ne 0) { throw 'Production patch application failed.' }
git -C $SourceFolder diff --check
if ($LASTEXITCODE -ne 0) { throw 'Patched source has whitespace errors.' }
Write-Output 'Pinned source prepared. No runtime DLL was built or replaced.'
