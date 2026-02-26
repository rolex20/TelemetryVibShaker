function Resolve-LegacyScriptPath {
    param([string]$FileName,[string[]]$Directories)
    foreach ($directory in $Directories) {
        $filePath = Join-Path $directory $FileName
        if (Test-Path $filePath) { return $filePath }
    }
    throw "Script file not found: $FileName"
}
