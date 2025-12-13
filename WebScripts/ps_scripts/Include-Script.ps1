function Include-Script {
    param (
        [string]$FileName,
        [string[]]$Directories
    )

    foreach ($dir in $Directories) {
        $file = Get-ChildItem -Path $dir -Recurse -Filter $FileName -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($file) {
            return $file.FullName
        }
    }

    return "Include-File-Not-Found"
}

