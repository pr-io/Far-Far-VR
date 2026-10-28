param([string]$ProjectFolder = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path -LiteralPath $ProjectFolder).Path
$taskLauncher = Join-Path $taskRoot 'Far Far West VR.exe'
if (!(Test-Path -LiteralPath $taskLauncher)) { throw 'Current game launcher is missing.' }
$taskLink = Join-Path $taskRoot 'UEVR for Far Far West.lnk'
if (!(Test-Path -LiteralPath $taskLink)) { throw 'Game-specific legacy shortcut not found.' }
$taskShell = New-Object -ComObject WScript.Shell
$taskShortcut = $taskShell.CreateShortcut($taskLink)
if ($taskShortcut.TargetPath -eq $taskLauncher) { Write-Output 'Game shortcut already uses the current launcher.'; return }
$taskExpectedInjector = Join-Path $taskRoot 'UE5.8\UEVRInjector.exe'
if ($taskShortcut.TargetPath -ne $taskExpectedInjector) { throw 'Unexpected shortcut target; left unchanged.' }
$taskBackup = Join-Path $taskRoot ('diagnostics\shortcut-before-repair-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ') + '.lnk')
New-Item -ItemType Directory -Path (Split-Path $taskBackup) -Force | Out-Null
Copy-Item -LiteralPath $taskLink -Destination $taskBackup
$taskShortcut.TargetPath = $taskLauncher
$taskShortcut.Arguments = '--start'
$taskShortcut.WorkingDirectory = $taskRoot
$taskShortcut.Description = 'Start Far Far West with the current VR fixes and recover saved comfort settings.'
$taskShortcut.Save()
$taskVerified = $taskShell.CreateShortcut($taskLink)
if ($taskVerified.TargetPath -ne $taskLauncher -or $taskVerified.Arguments -ne '--start') { throw 'Shortcut verification failed.' }
Write-Output ('Game shortcut now uses current launcher. Previous shortcut backup: ' + $taskBackup)
