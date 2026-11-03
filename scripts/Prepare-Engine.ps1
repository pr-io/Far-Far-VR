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
# The patch references these product headers; Git diffs omit untracked files.
$headerRoot = Join-Path $SourceFolder 'src\mods\vr'
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Engine') -Filter 'FarFarWest*.hpp' -File |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $headerRoot -Force }
function Apply-ProductPatch([string]$Root, [string]$Patch) {
    git -C $Root apply --reverse --check $Patch 2>$null
    if ($LASTEXITCODE -eq 0) { return }
    git -C $Root apply --check $Patch
    if ($LASTEXITCODE -ne 0) { throw "Product patch cannot be applied cleanly: $Patch. Existing changes were left untouched." }
    git -C $Root apply $Patch
    if ($LASTEXITCODE -ne 0) { throw "Product patch application failed: $Patch" }
    git -C $Root diff --check
    if ($LASTEXITCODE -ne 0) { throw "Patched source has whitespace errors: $Root" }
}
$sdkRoot = Join-Path $SourceFolder 'dependencies\submodules\UESDK'
$sdkRevision = git -C $sdkRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0 -or $sdkRevision -ne '811b01498b61e6a0246615b4f2b4fb4f4067fb64') { throw 'Initialize the pinned UESDK submodule before applying product patches.' }
Apply-ProductPatch $sdkRoot (Join-Path $projectRoot 'Engine\farfar-console.patch')
Apply-ProductPatch $SourceFolder (Join-Path $projectRoot 'Engine\cinema-comfort.patch')
Write-Output 'Pinned source and product headers prepared. No runtime DLL was built or replaced.'
