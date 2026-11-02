param([string]$RuntimeFolder = (Join-Path $PSScriptRoot '..\UE5.8'))
$ErrorActionPreference = 'Stop'
$version = '0.4.0'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$runtimeRoot = (Resolve-Path -LiteralPath $RuntimeFolder).Path
$expected = @(
    '0CFB68B928FFA5F5516BA866AFB676EB716F99A72B05B736CC5FB3053DA94029',
    '179BB4A6F61574AC16621E2C7C9E2E27F3AA328462243417D177527647315558',
    '2F30D9353B02435F0D34A6505D2FF494F4DACD20783F8D72CB408F9AEBE41558'
)
if ((Get-FileHash -LiteralPath (Join-Path $runtimeRoot 'UEVRBackend.dll')).Hash -notin $expected) { throw 'Unexpected backend. Use a pinned compatibility build.' }
$packageRoot = Join-Path $projectRoot 'dist\FarFarVR'
if (Test-Path -LiteralPath $packageRoot) { Remove-Item -LiteralPath $packageRoot -Recurse -Force }
foreach ($name in @('UEVRBackend.dll', 'LuaVR.dll', 'openxr_loader.dll', 'openvr_api.dll', 'BUILD-SOURCE.txt')) {
    if (!(Test-Path -LiteralPath (Join-Path $runtimeRoot $name))) { throw "Runtime file missing: $name" }
}
$publishRoot = Join-Path $projectRoot 'Launcher\publish'
dotnet publish (Join-Path $projectRoot 'Launcher\FarFarWestVR.csproj') -c Release -r win-x64 -o $publishRoot
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed.' }
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'UE5.8') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $publishRoot 'FarFarWestVR.exe') -Destination (Join-Path $packageRoot 'Far Far VR.exe')
foreach ($name in @('UEVRBackend.dll', 'LuaVR.dll', 'openxr_loader.dll', 'openvr_api.dll', 'BUILD-SOURCE.txt')) {
    Copy-Item -LiteralPath (Join-Path $runtimeRoot $name) -Destination (Join-Path $packageRoot 'UE5.8')
}
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'Profile') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Profile') -File -Recurse |
    Where-Object { $_.Extension -ne '.md' } |
    ForEach-Object {
        $relative = [IO.Path]::GetRelativePath((Join-Path $projectRoot 'Profile'), $_.FullName)
        $destination = Join-Path (Join-Path $packageRoot 'Profile') $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $destination
    }
foreach ($name in @('README.md', 'THIRD_PARTY.md', 'ROADMAP.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $packageRoot }
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $packageRoot -Recurse
Copy-Item -LiteralPath (Join-Path $projectRoot 'Launcher\Apply-Update.ps1') -Destination (Join-Path $packageRoot 'Apply-Update.ps1')
Copy-Item -LiteralPath (Join-Path $packageRoot 'Far Far VR.exe') -Destination (Join-Path $projectRoot 'Far Far VR.exe') -Force
Set-Content -LiteralPath (Join-Path $packageRoot 'version.txt') -Value $version -Encoding ascii
Get-ChildItem -LiteralPath $packageRoot -Recurse -File | ForEach-Object {
    [PSCustomObject]@{ File = $_.FullName.Substring($packageRoot.TrimEnd("\").Length + 1); SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding utf8
Write-Output "Local package staged: $packageRoot"
