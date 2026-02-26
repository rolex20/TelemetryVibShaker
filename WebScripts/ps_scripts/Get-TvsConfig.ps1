function Merge-Hashtable {
    param(
        [hashtable]$Base,
        [hashtable]$Overlay
    )

    foreach ($key in $Overlay.Keys) {
        if ($Base.ContainsKey($key) -and $Base[$key] -is [hashtable] -and $Overlay[$key] -is [hashtable]) {
            Merge-Hashtable -Base $Base[$key] -Overlay $Overlay[$key]
        } else {
            $Base[$key] = $Overlay[$key]
        }
    }

    return $Base
}

function ConvertTo-Hashtable {
    param([Parameter(Mandatory)]$InputObject)

    if ($null -eq $InputObject) {
        return $null
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        $hash = @{}
        foreach ($k in $InputObject.Keys) {
            $hash[$k] = ConvertTo-Hashtable -InputObject $InputObject[$k]
        }
        return $hash
    }

    if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
        $arr = @()
        foreach ($item in $InputObject) {
            $arr += ,(ConvertTo-Hashtable -InputObject $item)
        }
        return $arr
    }

    if ($InputObject.PSObject.Properties.Count -gt 0) {
        $hash = @{}
        foreach ($prop in $InputObject.PSObject.Properties) {
            $hash[$prop.Name] = ConvertTo-Hashtable -InputObject $prop.Value
        }
        return $hash
    }

    return $InputObject
}

function Get-TvsConfig {
    $configRoot = Join-Path (Split-Path -Parent $PSScriptRoot) 'config'
    $defaultsPath = Join-Path $configRoot 'defaults.json'
    $localPath = Join-Path $configRoot 'local.json'

    $defaults = @{}
    if (Test-Path $defaultsPath) {
        $defaults = ConvertTo-Hashtable -InputObject ((Get-Content -Path $defaultsPath -Raw) | ConvertFrom-Json)
    }

    if (Test-Path $localPath) {
        $local = ConvertTo-Hashtable -InputObject ((Get-Content -Path $localPath -Raw) | ConvertFrom-Json)
        $merged = Merge-Hashtable -Base $defaults -Overlay $local
    } else {
        $merged = $defaults
    }

    return [PSCustomObject]$merged
}
