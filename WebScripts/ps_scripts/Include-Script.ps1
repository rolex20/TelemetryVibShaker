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
