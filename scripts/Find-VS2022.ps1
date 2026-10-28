param([string]$Path = '')
$ErrorActionPreference = 'Stop'
if ($Path) {
    if (!(Test-Path -LiteralPath (Join-Path $Path 'Common7\Tools\Launch-VsDevShell.ps1'))) {
        throw "Visual Studio development shell not found under '$Path'."
    }
    Write-Output ([IO.Path]::GetFullPath($Path))
    exit 0
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (Test-Path -LiteralPath $vswhere) {
    $found = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if ($LASTEXITCODE -eq 0 -and $found -and (Test-Path -LiteralPath (Join-Path $found 'Common7\Tools\Launch-VsDevShell.ps1'))) {
        Write-Output ([IO.Path]::GetFullPath($found))
        exit 0
    }
}
throw 'Visual Studio 2022 with the x64 C++ toolchain was not found. Install the native build prerequisites or pass -BuildToolsFolder.'
