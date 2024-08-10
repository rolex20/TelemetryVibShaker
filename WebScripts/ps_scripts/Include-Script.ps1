<#
function Include-Script($filename, $SearchPath1, $SearchPath2) {
    $result = Get-ChildItem $SearchPath1 -Recurse -Include $filename
    if ($result.Count -gt 0) {
        return $result[0].FullName
    } else {
        $result = Get-ChildItem $SearchPath2 -Recurse -Include $filename
        if ($result.Count -gt 0) {
            return $result[0].FullName
        } else {
            return "Include-File-Not-Found"
            Exit
        }
    }
}
#>

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

<#
# Example usage:
$directories = @("C:\Folder1", "C:\Folder2", "C:\Folder3")
$fileName = "example.txt"
$result = Find-FirstFile -FileName $fileName -Directories $directories

if ($result) {
    Write-Output "File found at: $result"
} else {
    Write-Output "File not found in the specified directories."
}
#>