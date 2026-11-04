param(
    [Parameter(Mandatory=$true)][string]$InstallRoot,
    [string]$StagingRoot,
    [Parameter(Mandatory=$true)][int]$WaitForProcessId,
    [switch]$RecoverOnly,
    [switch]$NoRelaunch
)
$ErrorActionPreference='Stop'
$install=[IO.Path]::GetFullPath($InstallRoot).TrimEnd('\','/')
$prefix=$install+[IO.Path]::DirectorySeparatorChar
$journal=Join-Path $install '.farfarvr-update-transaction.json'
$profile=Join-Path $env:APPDATA 'UnrealVRMod\FarFarWest-Win64-Shipping\data'
$version='pending'
function Resolve-InstallFile([string]$Relative) {
    $full=[IO.Path]::GetFullPath((Join-Path $install $Relative))
    if(!$full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid update recovery path.'}
    return $full
}
function Get-Sha256([string]$Path) {
    $sha=[Security.Cryptography.SHA256]::Create();$stream=[IO.File]::OpenRead($Path)
    try{return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
}
function Write-DurableJson($Value,[string]$Path) {
    $bytes=[Text.Encoding]::UTF8.GetBytes(($Value|ConvertTo-Json -Depth 8))
    $stream=[IO.FileStream]::new($Path+'.pending',[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
    Move-Item -LiteralPath ($Path+'.pending') -Destination $Path -Force
}
function Write-Status([string]$State,[string]$Message) {
    New-Item -ItemType Directory -Path $profile -Force|Out-Null
    $path=Join-Path $profile 'farfarvr-updates.txt'
    [IO.File]::WriteAllText($path+'.pending',"state=$State`nversion=$version`nresponseId=$([Guid]::NewGuid().ToString('N'))`nnotes:`n$Message")
    Move-Item -LiteralPath ($path+'.pending') -Destination $path -Force
}
function Restore-Transaction($Transaction) {
    $errors=[Collections.Generic.List[string]]::new()
    foreach($entry in $Transaction.Entries){
        try{
            $target=Resolve-InstallFile $entry.Relative
            if($entry.Existed){
                $backup=Resolve-InstallFile $entry.Backup
                if(!(Test-Path -LiteralPath $backup -PathType Leaf)){throw "Recovery backup missing: $($entry.Relative)"}
                [IO.File]::Copy($backup,$target,$true)
            }elseif([IO.File]::Exists($target)){[IO.File]::Delete($target)}
        }catch{$errors.Add($_.Exception.Message)}
    }
    if($errors.Count){throw ('Recovery incomplete; backups retained. '+($errors -join '; '))}
    Clear-Transaction $Transaction
}
function Clear-Transaction($Transaction) {
    # Clear the transaction only after all changes or all restorations succeed.
    [IO.File]::Delete($journal)
    $backupRoot=Resolve-InstallFile $Transaction.BackupFolder
    if([IO.Path]::GetFileName($backupRoot) -notlike '.farfarvr-update-rollback-*'){throw 'Invalid rollback directory.'}
    if(Test-Path -LiteralPath $backupRoot){try{Remove-Item -LiteralPath $backupRoot -Recurse -Force}catch{Write-Warning 'Update completed; some recovery backups could not be removed.'}}
}
while(Get-Process -Id $WaitForProcessId -ErrorAction SilentlyContinue){Start-Sleep -Milliseconds 500}
while(Get-Process -Name 'FarFarWest-Win64-Shipping' -ErrorAction SilentlyContinue){Start-Sleep -Seconds 2}
if(Test-Path -LiteralPath $journal){
    $previous=Get-Content -LiteralPath $journal -Raw|ConvertFrom-Json
    $version=$previous.Version
    try{Restore-Transaction $previous;Write-Status 'error' 'An interrupted update was rolled back. Check for updates to try again.'}
    catch{Write-Status 'error' $_.Exception.Message;throw}
}
if(!$RecoverOnly){
    $staging=[IO.Path]::GetFullPath($StagingRoot)
    $marker=Join-Path ([IO.Path]::GetDirectoryName($staging)) 'prepared.txt'
    if(!(Test-Path -LiteralPath $marker -PathType Leaf)){throw 'The prepared update marker is missing.'}
    $version=[IO.File]::ReadAllText($marker).Trim()
    $manifest=Get-Content -LiteralPath (Join-Path $staging 'manifest.json') -Raw|ConvertFrom-Json
    if($manifest.version -ne $version.TrimStart('v')){throw 'The prepared package version does not match its marker.'}
    $newFiles=@{}
    foreach($file in $manifest.files){
        $relative=$file.File
        $target=Resolve-InstallFile $relative
        $source=[IO.Path]::GetFullPath((Join-Path $staging $relative))
        if(!$source.StartsWith($staging.TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid staged path.'}
        if($newFiles.ContainsKey($relative)){throw 'Duplicate package file.'}
        if((Get-Sha256 $source) -ne $file.SHA256){throw "Package integrity failed: $relative"}
        $newFiles[$relative]=$source
    }
    foreach($required in @('Far Far VR.exe','Apply-Update.ps1','Profile\config.txt','Runtime\backend.ffvrdelta')){
        if(!$newFiles.ContainsKey($required)){throw "Incomplete update: $required"}
    }
    $newFiles['manifest.json']=Join-Path $staging 'manifest.json'
    $removals=@()
    $oldManifest=Join-Path $install 'manifest.json'
    if(Test-Path -LiteralPath $oldManifest){
        foreach($file in (Get-Content -LiteralPath $oldManifest -Raw|ConvertFrom-Json).files){
            if(!$newFiles.ContainsKey($file.File)){
                $old=Resolve-InstallFile $file.File
                if((Test-Path -LiteralPath $old -PathType Leaf) -and (Get-Sha256 $old) -eq $file.SHA256){$removals+=,$file.File}
            }
        }
    }
    $folder='.farfarvr-update-rollback-'+[Guid]::NewGuid().ToString('N')
    New-Item -ItemType Directory -Path (Resolve-InstallFile $folder)|Out-Null
    [IO.File]::Copy($PSCommandPath,(Resolve-InstallFile (Join-Path $folder 'recover.ps1')),$false)
    $entries=@();$index=0
    foreach($relative in @($newFiles.Keys)+$removals){
        $target=Resolve-InstallFile $relative
        $existed=[IO.File]::Exists($target)
        $backup=Join-Path $folder ($index.ToString()+'.bak');$index++
        if($existed){[IO.File]::Copy($target,(Resolve-InstallFile $backup),$false)}
        $entries+=,[pscustomobject]@{Relative=$relative;Existed=$existed;Backup=$backup}
    }
    $transaction=[pscustomobject]@{Version=$version;BackupFolder=$folder;Entries=$entries}
    Write-DurableJson $transaction $journal
    try{
        foreach($relative in $newFiles.Keys){
            $target=Resolve-InstallFile $relative
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force|Out-Null
            $temp=$target+'.farfarvr-new'
            [IO.File]::Copy($newFiles[$relative],$temp,$true)
            if([IO.File]::Exists($target)){[IO.File]::Replace($temp,$target,[NullString]::Value)}else{[IO.File]::Move($temp,$target)}
        }
        foreach($relative in $removals){[IO.File]::Delete((Resolve-InstallFile $relative))}
        # Deleting the marker precedes commit: a crash still has a complete rollback journal.
        [IO.File]::Delete($marker)
        Clear-Transaction $transaction
        Write-Status 'applied' 'Update installed. Far Far VR is ready.'
    }catch{
        $failure=$_.Exception.Message
        if(Test-Path -LiteralPath $journal){Restore-Transaction $transaction}
        Write-Status 'error' ('Update rolled back: '+$failure)
        throw
    }
}
if(!$NoRelaunch){Start-Process -FilePath (Join-Path $install 'Far Far VR.exe')}
