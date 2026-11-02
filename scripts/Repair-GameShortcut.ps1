param([string]$ProjectFolder = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath $ProjectFolder).Path
$taskLauncher = Join-Path $taskRoot 'Far Far West VR.exe'
if (!(Test-Path -LiteralPath $taskLauncher)) { throw 'Current game launcher is missing.' }
$taskLink = Join-Path $taskRoot 'UEVR for Far Far West.lnk'
if (!(Test-Path -LiteralPath $taskLink)) { throw 'Game-specific legacy shortcut not found.' }
$taskShell = New-Object -ComObject WScript.Shell
$taskShortcut = $taskShell.CreateShortcut($taskLink)
if ($taskShortcut.TargetPath -eq $taskLauncher -and [string]::IsNullOrWhiteSpace($taskShortcut.Arguments)) { Write-Output 'Game shortcut already opens the current launcher and waits for Start.'; return }
$taskExpectedInjector = Join-Path $taskRoot 'UE5.8\UEVRInjector.exe'
if ($taskShortcut.TargetPath -ne $taskExpectedInjector -and $taskShortcut.TargetPath -ne $taskLauncher) { throw 'Unexpected shortcut target; left unchanged.' }
$taskBackup = Join-Path $taskRoot ('diagnostics\shortcut-before-repair-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ') + '.lnk')
New-Item -ItemType Directory -Path (Split-Path $taskBackup) -Force | Out-Null
Copy-Item -LiteralPath $taskLink -Destination $taskBackup
$taskShortcut.TargetPath = $taskLauncher
$taskShortcut.Arguments = ''
$taskShortcut.WorkingDirectory = $taskRoot
$taskShortcut.Description = 'Open Far Far VR, then press Start in VR to play.'
$taskShortcut.Save()
$taskVerified = $taskShell.CreateShortcut($taskLink)
if ($taskVerified.TargetPath -ne $taskLauncher -or $taskVerified.Arguments -ne '') { throw 'Shortcut verification failed.' }
Write-Output ('Game shortcut now uses current launcher. Previous shortcut backup: ' + $taskBackup)
