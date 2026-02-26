function Get-RenamesWatcher($WatchFile, $Action)
{
    $watchDirectory = [System.IO.Path]::GetDirectoryName($WatchFile)
    if ([string]::IsNullOrWhiteSpace($watchDirectory) -or -not (Test-Path -LiteralPath $watchDirectory)) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "WARNING" -Message "Watcher path does not exist: $watchDirectory" -ForegroundColor "Yellow"
        return $null
    }

    $filewatcher = New-Object System.IO.FileSystemWatcher
    $filewatcher.Path = $watchDirectory
    $filewatcher.Filter = [System.IO.Path]::GetFileName($WatchFile)
    $filewatcher.IncludeSubdirectories = $false
    $filewatcher.EnableRaisingEvents = $true

    $event = Register-ObjectEvent $filewatcher "Renamed" -Action $Action

    return ($event, $filewatcher)
}

function Get-ModificationWatcher($WatchFile, $Action)
{
    $watchDirectory = [System.IO.Path]::GetDirectoryName($WatchFile)
    if ([string]::IsNullOrWhiteSpace($watchDirectory) -or -not (Test-Path -LiteralPath $watchDirectory)) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "WARNING" -Message "Watcher path does not exist: $watchDirectory" -ForegroundColor "Yellow"
        return $null
    }

    $filewatcher = New-Object System.IO.FileSystemWatcher
    $filewatcher.Path = $watchDirectory
    $filewatcher.Filter = [System.IO.Path]::GetFileName($WatchFile)
    $filewatcher.IncludeSubdirectories = $false
    $filewatcher.EnableRaisingEvents = $true

    $event = Register-ObjectEvent $filewatcher "Changed" -Action $Action

    return ($event, $filewatcher)
}
