function Find-FileRecursively($filename, $initialPath) {
    $result = Get-ChildItem $initialPath -Recurse -Include $filename
    if ($result.Count -gt 0) {
        return $result[0].FullName
    } else {
        return $null
    }
}
