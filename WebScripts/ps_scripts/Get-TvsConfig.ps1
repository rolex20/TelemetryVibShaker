function Merge-TvsHashtable {
    param([hashtable]$Base, [hashtable]$Overlay)
    foreach ($key in $Overlay.Keys) {
        if ($Base.ContainsKey($key) -and $Base[$key] -is [hashtable] -and $Overlay[$key] -is [hashtable]) {
            Merge-TvsHashtable -Base $Base[$key] -Overlay $Overlay[$key]
        } else {
            $Base[$key] = $Overlay[$key]
        }
    }
    return $Base
}

function Resolve-TvsPath {
    param([string]$BasePath, [string]$Candidate)
    if ([string]::IsNullOrWhiteSpace($Candidate)) { return $Candidate }
    if ([System.IO.Path]::IsPathRooted($Candidate)) { return $Candidate }
    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Candidate))
}

function Get-TvsConfig {
    param([string]$WebScriptsRoot = $PSScriptRoot)

    $configRoot = Join-Path $WebScriptsRoot 'config'
    $defaultsPath = Join-Path $configRoot 'defaults.json'
    $localPath = Join-Path $configRoot 'local.json'

    $defaults = (Get-Content -Path $defaultsPath -Raw | ConvertFrom-Json -AsHashtable)
    if (Test-Path $localPath) {
        $local = Get-Content -Path $localPath -Raw | ConvertFrom-Json -AsHashtable
        $defaults = Merge-TvsHashtable -Base $defaults -Overlay $local
    }

    $defaults.paths.commandJson = Resolve-TvsPath -BasePath $WebScriptsRoot -Candidate $defaults.paths.commandJson
    $defaults.paths.wtMissionJson = Resolve-TvsPath -BasePath $WebScriptsRoot -Candidate $defaults.paths.wtMissionJson
    $defaults.paths.wtConfigBlk = Resolve-TvsPath -BasePath $WebScriptsRoot -Candidate $defaults.paths.wtConfigBlk

    return $defaults
}
