
# PowerShell 5.1 compatible: PSCustomObject -> Hashtable (recursive)
function ConvertTo-Hashtable {
    param([Parameter(ValueFromPipeline=$true)]$InputObject)

    if ($null -eq $InputObject) { return $null }

    # IDictionary (already a map)
    if ($InputObject -is [System.Collections.IDictionary]) {
        $ht = @{}
        foreach ($k in $InputObject.Keys) {
            $ht[$k] = ConvertTo-Hashtable $InputObject[$k]
        }
        return $ht
    }

    # PSCustomObject (JSON objects land here in PS 5.1)
    if ($InputObject -is [pscustomobject]) {
        $ht = @{}
        foreach ($p in $InputObject.PSObject.Properties) {
            $ht[$p.Name] = ConvertTo-Hashtable $p.Value
        }
        return $ht
    }

    # Enumerable (arrays/lists), but not strings
    if (($InputObject -is [System.Collections.IEnumerable]) -and -not ($InputObject -is [string])) {
        $arr = @()
        foreach ($item in $InputObject) {
            $arr += ,(ConvertTo-Hashtable $item)
        }
        return $arr
    }

    # Primitive
    return $InputObject
}

function Merge-Hashtable {
    param([hashtable]$Base, [hashtable]$Override)

    $out = @{}
    foreach ($k in $Base.Keys) { $out[$k] = $Base[$k] }

    foreach ($k in $Override.Keys) {
        if ($out[$k] -is [hashtable] -and $Override[$k] -is [hashtable]) {
            $out[$k] = Merge-Hashtable $out[$k] $Override[$k]
        } else {
            $out[$k] = $Override[$k]
        }
    }
    return $out
}

function Get-WebScriptsConfig {
    $webScriptsRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    $cfgPath = Join-Path $PSScriptRoot "config\scripts.hosts.json"


    $rawObj   = Get-Content $cfgPath -Raw | ConvertFrom-Json
    $raw      = ConvertTo-Hashtable $rawObj

    $defaults = @{}
    if ($raw.ContainsKey("defaults")) { $defaults = $raw["defaults"] }

    $machines = @{}
    if ($raw.ContainsKey("machines")) { $machines = $raw["machines"] }

    $myhostname = $env:COMPUTERNAME.ToUpperInvariant()

    $hostCfg = @{}
    if ($machines.ContainsKey($myhostname)) { $hostCfg = $machines[$myhostname] }

    $final = Merge-Hashtable $defaults $hostCfg
    $final["webScriptsRoot"] = $webScriptsRoot.Path
    $final["host"] = $myhostname

    return $final
}

function Bootstrap-Config {
	$Global:WebScriptsConfig = Get-WebScriptsConfig
}


