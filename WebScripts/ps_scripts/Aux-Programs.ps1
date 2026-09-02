<#
.SYNOPSIS
    Normalizes, launches, matches, tracks, and safely stops configured game auxiliary programs.

.DESCRIPTION
    This file intentionally keeps configuration parsing separate from lifecycle execution.
    All code is compatible with Windows PowerShell 5.1.

    StopMode=OwnedOnly is conservative by design: a process is stopped only when its exact
    PID and creation time were observed after a launch performed by this watcher instance.
#>

function Write-AuxProgramLog {
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [string]$ForegroundColor = 'Gray',

        [string]$Title = 'AUX'
    )

    Write-VerboseDebug -Timestamp (Get-Date) -Title $Title -Message $Message -ForegroundColor $ForegroundColor -Speak $false
}

function New-AuxProgramLifecycleState {
    return @{
        SyncRoot             = New-Object object
        DefinitionCatalog    = @{}
        SingletonByIdentity  = @{}
        AlwaysByParentPid    = @{}
        PendingIfNotRunning  = @{}
    }
}

function Get-NormalizedAuxWindowStyle {
    param(
        [string]$WindowStyle = 'Minimized'
    )

    $canonicalWindowStyles = @{
        'normal'    = 'Normal'
        'hidden'    = 'Hidden'
        'minimized' = 'Minimized'
        'maximized' = 'Maximized'
    }

    if ([string]::IsNullOrWhiteSpace($WindowStyle)) {
        return 'Minimized'
    }

    $normalized = $WindowStyle.Trim().ToLowerInvariant()
    if ($canonicalWindowStyles.ContainsKey($normalized)) {
        return $canonicalWindowStyles[$normalized]
    }

    Write-AuxProgramLog -Title 'AUX START' -ForegroundColor 'DarkYellow' -Message "Invalid AuxPrograms WindowStyle '$WindowStyle'. Valid values: Normal, Hidden, Minimized, Maximized. Falling back to Minimized."
    return 'Minimized'
}

function Remove-AuxOuterQuotes {
    param(
        [string]$Value
    )

    if ($null -eq $Value) {
        return $null
    }

    $trimmed = $Value.Trim()
    if ($trimmed.Length -ge 2) {
        $first = $trimmed.Substring(0, 1)
        $last = $trimmed.Substring($trimmed.Length - 1, 1)
        if (($first -eq '"' -and $last -eq '"') -or ($first -eq "'" -and $last -eq "'")) {
            return $trimmed.Substring(1, $trimmed.Length - 2).Trim()
        }
    }

    return $trimmed
}

function ConvertTo-NormalizedAuxProcessName {
    param(
        [string]$ProcessName
    )

    $normalized = Remove-AuxOuterQuotes -Value $ProcessName
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $null
    }

    # Process names are used in WQL filters. Restrict them to a Windows filename leaf
    # and reject wildcards, path separators, and quotes instead of attempting to escape
    # arbitrary input into a process query.
    if ($normalized -match '[\\/:*?"<>|]' -or $normalized -match "'") {
        return $null
    }

    $normalized = $normalized -replace '(?i)\.exe$', ''
    $normalized = $normalized.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $null
    }

    return $normalized
}

function ConvertTo-NormalizedAuxHostProcessName {
    param(
        [string]$ProcessName
    )

    $baseName = ConvertTo-NormalizedAuxProcessName -ProcessName $ProcessName
    if ([string]::IsNullOrWhiteSpace($baseName)) {
        return $null
    }

    return "$baseName.exe"
}

function ConvertTo-NormalizedAuxScriptPath {
    param(
        [string]$ScriptPath
    )

    $normalized = Remove-AuxOuterQuotes -Value $ScriptPath
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $null
    }

    $normalized = $normalized.Replace('/', '\')
    if (-not [System.IO.Path]::IsPathRooted($normalized)) {
        return $null
    }

    try {
        $normalized = [System.IO.Path]::GetFullPath($normalized)
    }
    catch {
        return $null
    }

    if (-not $normalized.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    return $normalized
}

function ConvertTo-NormalizedAuxScriptName {
    param(
        [string]$ScriptName
    )

    $normalized = Remove-AuxOuterQuotes -Value $ScriptName
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $null
    }

    if ($normalized.IndexOf('\') -ge 0 -or $normalized.IndexOf('/') -ge 0) {
        return $null
    }

    if ([System.IO.Path]::GetFileName($normalized) -ne $normalized) {
        return $null
    }

    if (-not $normalized.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    return $normalized
}

function Get-AuxDefinitionIdentity {
    param(
        [hashtable]$Definition,
        [int]$EntryIndex
    )

    if (-not [string]::IsNullOrWhiteSpace([string]$Definition.Id)) {
        return ('id:{0}' -f $Definition.Id.Trim().ToLowerInvariant())
    }

    switch ($Definition.MatchType) {
        'ProcessName' {
            return ('process:{0}' -f $Definition.ProcessName.ToLowerInvariant())
        }
        'PowerShellScript' {
            $hostName = $Definition.PowerShellHostProcessName.ToLowerInvariant()
            if (-not [string]::IsNullOrWhiteSpace([string]$Definition.ScriptPath)) {
                return ('ps1:{0}|path:{1}' -f $hostName, $Definition.ScriptPath.ToLowerInvariant())
            }

            return ('ps1:{0}|name:{1}' -f $hostName, $Definition.ScriptName.ToLowerInvariant())
        }
        default {
            # Legacy strings have no matcher and never enter lifecycle ownership state.
            # The index keeps duplicate legacy entries distinct so launch-every-entry behavior
            # remains exactly compatible with the existing implementation.
            return ('legacy:{0}|index:{1}' -f $Definition.Path.ToLowerInvariant(), $EntryIndex)
        }
    }
}

function Get-AuxDefinitionSignature {
    param(
        [hashtable]$Definition
    )

    $parts = @(
        [string]$Definition.MatchType
        [string]$Definition.ProcessName
        [string]$Definition.ScriptPath
        [string]$Definition.ScriptName
        [string]$Definition.PowerShellHostProcessName
        [string]$Definition.Path
        [string]$Definition.LaunchMode
        [string]$Definition.StopMode
    )

    return (($parts | ForEach-Object { $_.Trim().ToLowerInvariant() }) -join '|')
}

function New-AuxProgramDefinition {
    param(
        [string]$Id,
        [Parameter(Mandatory)][string]$Path,
        [string]$MatchType,
        [string]$ProcessName,
        [string]$ScriptPath,
        [string]$ScriptName,
        [string]$PowerShellHostProcessName,
        [Parameter(Mandatory)][string]$LaunchMode,
        [Parameter(Mandatory)][string]$StopMode,
        [Parameter(Mandatory)][int]$StartupTimeoutSeconds,
        [Parameter(Mandatory)][string]$WindowStyle,
        [Parameter(Mandatory)][string]$SourceFormat,
        [Parameter(Mandatory)][int]$EntryIndex
    )

    $definition = @{
        Id                        = $Id
        Path                      = $Path
        MatchType                 = $MatchType
        ProcessName               = $ProcessName
        ProcessImageName          = if ($ProcessName) { "$ProcessName.exe" } else { $null }
        ScriptPath                = $ScriptPath
        ScriptName                = $ScriptName
        PowerShellHostProcessName = $PowerShellHostProcessName
        LaunchMode                = $LaunchMode
        StopMode                  = $StopMode
        StartupTimeoutSeconds     = $StartupTimeoutSeconds
        WindowStyle               = $WindowStyle
        SourceFormat              = $SourceFormat
    }

    $definition.IdentityKey = Get-AuxDefinitionIdentity -Definition $definition -EntryIndex $EntryIndex
    $definition.DefinitionSignature = Get-AuxDefinitionSignature -Definition $definition
    return $definition
}

function ConvertTo-AuxProgramDefinition {
    param(
        [Parameter(Mandatory)]
        $Entry,

        [Parameter(Mandatory)]
        [string]$WindowStyle,

        [Parameter(Mandatory)]
        [int]$EntryIndex
    )

    if ($Entry -is [string]) {
        $rawEntry = [string]$Entry
        if ([string]::IsNullOrWhiteSpace($rawEntry)) {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Skipping blank AuxPrograms entry at index $EntryIndex."
            return $null
        }

        $trimmedEntry = $rawEntry.Trim()
        if ($trimmedEntry.StartsWith('[')) {
            $match = [regex]::Match($trimmedEntry, '^\[(?<matcher>[^\]]+)\](?<path>.+)$')
            if (-not $match.Success) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Malformed auxiliary shorthand at index $EntryIndex. The entry was skipped instead of treated as a legacy path."
                return $null
            }

            $matcher = $match.Groups['matcher'].Value.Trim()
            $launchPath = $match.Groups['path'].Value.Trim()
            if ([string]::IsNullOrWhiteSpace($matcher) -or [string]::IsNullOrWhiteSpace($launchPath)) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Malformed auxiliary shorthand at index ${EntryIndex}: matcher and launch path are both required."
                return $null
            }

            # Parse shorthand prefix for mode overrides:
            # - [killall:...] -> Kill pre-existing on start AND kill all matching on exit.
            # - [kill:...]    -> Kill pre-existing on start AND track/stop owned on exit.
            # - standard      -> Launch if not running AND never stop on exit.
            $shorthandLaunchMode = 'IfNotRunning'
            $shorthandStopMode = 'Never'

            if ($matcher.StartsWith('killall:', [System.StringComparison]::OrdinalIgnoreCase) -or
                $matcher.StartsWith('kill-all:', [System.StringComparison]::OrdinalIgnoreCase) -or
                $matcher.StartsWith('forceall:', [System.StringComparison]::OrdinalIgnoreCase)) {
                $shorthandLaunchMode = 'KillExistingAndLaunch'
                $shorthandStopMode = 'ForceAll'
                $matcher = $matcher.Substring($matcher.IndexOf(':') + 1).Trim()
            }
            elseif ($matcher.StartsWith('kill:', [System.StringComparison]::OrdinalIgnoreCase)) {
                $shorthandLaunchMode = 'KillExistingAndLaunch'
                $shorthandStopMode = 'OwnedOnly'
                $matcher = $matcher.Substring(5).Trim()
            }

            if ($matcher.StartsWith('ps1:', [System.StringComparison]::OrdinalIgnoreCase)) {
                $scriptMatcher = Remove-AuxOuterQuotes -Value $matcher.Substring(4)
                if ([string]::IsNullOrWhiteSpace($scriptMatcher)) {
                    Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Malformed PowerShell shorthand at index ${EntryIndex}: the ps1 matcher is empty."
                    return $null
                }

                $scriptPath = $null
                $scriptName = $null
                $slashNormalizedMatcher = $scriptMatcher.Replace('/', '\')
                if ([System.IO.Path]::IsPathRooted($slashNormalizedMatcher)) {
                    $scriptPath = ConvertTo-NormalizedAuxScriptPath -ScriptPath $scriptMatcher
                    if (-not $scriptPath) {
                        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid PowerShell script path matcher '$scriptMatcher' at index $EntryIndex. Use an absolute .ps1 path."
                        return $null
                    }
                }
                elseif ($scriptMatcher.IndexOf('\') -ge 0 -or $scriptMatcher.IndexOf('/') -ge 0) {
                    Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Relative PowerShell script path matcher '$scriptMatcher' at index $EntryIndex is not supported. Use a full path or filename."
                    return $null
                }
                else {
                    $scriptName = ConvertTo-NormalizedAuxScriptName -ScriptName $scriptMatcher
                    if (-not $scriptName) {
                        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid PowerShell script filename matcher '$scriptMatcher' at index $EntryIndex."
                        return $null
                    }
                }

                return New-AuxProgramDefinition `
                    -Path $launchPath `
                    -MatchType 'PowerShellScript' `
                    -ScriptPath $scriptPath `
                    -ScriptName $scriptName `
                    -PowerShellHostProcessName 'powershell.exe' `
                    -LaunchMode $shorthandLaunchMode `
                    -StopMode $shorthandStopMode `
                    -StartupTimeoutSeconds 10 `
                    -WindowStyle $WindowStyle `
                    -SourceFormat 'PowerShellShorthand' `
                    -EntryIndex $EntryIndex
            }

            $processName = ConvertTo-NormalizedAuxProcessName -ProcessName $matcher
            if (-not $processName) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid executable matcher '$matcher' at index $EntryIndex."
                return $null
            }

            return New-AuxProgramDefinition `
                -Path $launchPath `
                -MatchType 'ProcessName' `
                -ProcessName $processName `
                -LaunchMode $shorthandLaunchMode `
                -StopMode $shorthandStopMode `
                -StartupTimeoutSeconds 10 `
                -WindowStyle $WindowStyle `
                -SourceFormat 'ProcessShorthand' `
                -EntryIndex $EntryIndex
        }

        return New-AuxProgramDefinition `
            -Path $trimmedEntry `
            -LaunchMode 'Always' `
            -StopMode 'Never' `
            -StartupTimeoutSeconds 10 `
            -WindowStyle $WindowStyle `
            -SourceFormat 'LegacyString' `
            -EntryIndex $EntryIndex
    }

    if (-not ($Entry -is [System.Collections.IDictionary])) {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Malformed AuxPrograms entry at index $EntryIndex. Expected a path string or JSON object."
        return $null
    }

    $path = [string]$Entry['Path']
    if ([string]::IsNullOrWhiteSpace($path)) {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Structured AuxPrograms entry at index $EntryIndex is missing required Path."
        return $null
    }
    $path = $path.Trim()

    $id = $null
    if ($Entry.Contains('Id') -and -not [string]::IsNullOrWhiteSpace([string]$Entry['Id'])) {
        $id = ([string]$Entry['Id']).Trim()
    }

    $matchType = ([string]$Entry['MatchType']).Trim()
    if ($matchType.Equals('ProcessName', [System.StringComparison]::OrdinalIgnoreCase)) {
        $matchType = 'ProcessName'
    }
    elseif ($matchType.Equals('PowerShellScript', [System.StringComparison]::OrdinalIgnoreCase)) {
        $matchType = 'PowerShellScript'
    }
    else {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid MatchType '$matchType' at AuxPrograms index $EntryIndex. Valid values: ProcessName, PowerShellScript."
        return $null
    }

    $launchMode = 'Always'
    if ($Entry.Contains('LaunchMode') -and -not [string]::IsNullOrWhiteSpace([string]$Entry['LaunchMode'])) {
        $launchMode = ([string]$Entry['LaunchMode']).Trim()
    }
    if ($launchMode.Equals('Always', [System.StringComparison]::OrdinalIgnoreCase)) {
        $launchMode = 'Always'
    }
    elseif ($launchMode.Equals('IfNotRunning', [System.StringComparison]::OrdinalIgnoreCase)) {
        $launchMode = 'IfNotRunning'
    }
    elseif ($launchMode.Equals('KillExistingAndLaunch', [System.StringComparison]::OrdinalIgnoreCase) -or
            $launchMode.Equals('KillExisting', [System.StringComparison]::OrdinalIgnoreCase) -or
            $launchMode.Equals('KillExistingBeforeLaunch', [System.StringComparison]::OrdinalIgnoreCase)) {
        $launchMode = 'KillExistingAndLaunch'
    }
    else {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid LaunchMode '$launchMode' at AuxPrograms index $EntryIndex. Valid values: Always, IfNotRunning, KillExistingAndLaunch."
        return $null
    }

    $stopMode = 'Never'
    if ($Entry.Contains('StopMode') -and -not [string]::IsNullOrWhiteSpace([string]$Entry['StopMode'])) {
        $stopMode = ([string]$Entry['StopMode']).Trim()
    }
    if ($stopMode.Equals('Never', [System.StringComparison]::OrdinalIgnoreCase)) {
        $stopMode = 'Never'
    }
    elseif ($stopMode.Equals('OwnedOnly', [System.StringComparison]::OrdinalIgnoreCase)) {
        $stopMode = 'OwnedOnly'
    }
    elseif ($stopMode.Equals('ForceAll', [System.StringComparison]::OrdinalIgnoreCase) -or
            $stopMode.Equals('KillAll', [System.StringComparison]::OrdinalIgnoreCase) -or
            $stopMode.Equals('AllMatching', [System.StringComparison]::OrdinalIgnoreCase)) {
        $stopMode = 'ForceAll'
    }
    else {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid StopMode '$stopMode' at AuxPrograms index $EntryIndex. Valid values: Never, OwnedOnly, ForceAll."
        return $null
    }

    $startupTimeoutSeconds = 10
    if ($Entry.Contains('StartupTimeoutSeconds') -and $null -ne $Entry['StartupTimeoutSeconds']) {
        $parsedTimeout = 0
        if (-not [int]::TryParse([string]$Entry['StartupTimeoutSeconds'], [ref]$parsedTimeout) -or $parsedTimeout -lt 0) {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid StartupTimeoutSeconds '$($Entry['StartupTimeoutSeconds'])' at AuxPrograms index $EntryIndex. Falling back to 10."
        }
        else {
            $startupTimeoutSeconds = $parsedTimeout
        }
    }

    $processName = $null
    $scriptPath = $null
    $scriptName = $null
    $hostProcessName = $null

    if ($matchType -eq 'ProcessName') {
        $processName = ConvertTo-NormalizedAuxProcessName -ProcessName ([string]$Entry['ProcessName'])
        if (-not $processName) {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Structured ProcessName auxiliary at index $EntryIndex is missing a valid ProcessName."
            return $null
        }
    }
    else {
        $hostProcessName = 'powershell.exe'
        if ($Entry.Contains('PowerShellHostProcessName') -and -not [string]::IsNullOrWhiteSpace([string]$Entry['PowerShellHostProcessName'])) {
            $hostProcessName = ConvertTo-NormalizedAuxHostProcessName -ProcessName ([string]$Entry['PowerShellHostProcessName'])
            if (-not $hostProcessName) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid PowerShellHostProcessName at AuxPrograms index $EntryIndex."
                return $null
            }
        }

        $hasScriptPath = $Entry.Contains('ScriptPath') -and -not [string]::IsNullOrWhiteSpace([string]$Entry['ScriptPath'])
        $hasScriptName = $Entry.Contains('ScriptName') -and -not [string]::IsNullOrWhiteSpace([string]$Entry['ScriptName'])

        if ($hasScriptPath) {
            $scriptPath = ConvertTo-NormalizedAuxScriptPath -ScriptPath ([string]$Entry['ScriptPath'])
            if (-not $scriptPath) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid ScriptPath at AuxPrograms index $EntryIndex. Use an absolute .ps1 path."
                return $null
            }

            if ($hasScriptName) {
                Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "AuxPrograms index $EntryIndex supplies ScriptPath and ScriptName; ScriptPath takes precedence."
            }
        }
        elseif ($hasScriptName) {
            $scriptName = ConvertTo-NormalizedAuxScriptName -ScriptName ([string]$Entry['ScriptName'])
            if (-not $scriptName) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Invalid ScriptName at AuxPrograms index $EntryIndex. Use a .ps1 filename without directories."
                return $null
            }
        }
        else {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Structured PowerShellScript auxiliary at index $EntryIndex requires ScriptPath or ScriptName."
            return $null
        }
    }

    return New-AuxProgramDefinition `
        -Id $id `
        -Path $path `
        -MatchType $matchType `
        -ProcessName $processName `
        -ScriptPath $scriptPath `
        -ScriptName $scriptName `
        -PowerShellHostProcessName $hostProcessName `
        -LaunchMode $launchMode `
        -StopMode $stopMode `
        -StartupTimeoutSeconds $startupTimeoutSeconds `
        -WindowStyle $WindowStyle `
        -SourceFormat 'Structured' `
        -EntryIndex $EntryIndex
}

function ConvertTo-AuxProgramDefinitions {
    param(
        [object[]]$AuxPrograms,
        [string]$WindowStyle = 'Minimized'
    )

    if (@($AuxPrograms).Count -eq 0) {
        return
    }

    $resolvedWindowStyle = Get-NormalizedAuxWindowStyle -WindowStyle $WindowStyle
    $definitions = @()
    $definitionsByIdentity = @{}
    $index = 0

    foreach ($entry in @($AuxPrograms)) {
        try {
            $definition = ConvertTo-AuxProgramDefinition -Entry $entry -WindowStyle $resolvedWindowStyle -EntryIndex $index
            if ($definition) {
                if ($definition.SourceFormat -ne 'LegacyString' -and $definitionsByIdentity.ContainsKey($definition.IdentityKey)) {
                    $existingDefinition = $definitionsByIdentity[$definition.IdentityKey]
                    if ($existingDefinition.DefinitionSignature.Equals($definition.DefinitionSignature, [System.StringComparison]::OrdinalIgnoreCase)) {
                        Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Duplicate auxiliary definition '$($definition.IdentityKey)' at index $index was skipped."
                    }
                    else {
                        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Incompatible auxiliary definitions resolve to '$($definition.IdentityKey)' in the same AuxPrograms array. The entry at index $index was skipped."
                    }
                    $index++
                    continue
                }

                if ($definition.SourceFormat -ne 'LegacyString') {
                    $definitionsByIdentity[$definition.IdentityKey] = $definition
                }
                $definitions += ,$definition
            }
        }
        catch {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Failed to normalize AuxPrograms entry at index ${index}: $($_.Exception.Message)"
        }
        $index++
    }

    return $definitions
}

function ConvertTo-AuxCreationTimeUtc {
    param(
        $CreationDate
    )

    if ($null -eq $CreationDate) {
        return $null
    }

    try {
        if ($CreationDate -is [datetime]) {
            return ([datetime]$CreationDate).ToUniversalTime()
        }

        $asText = [string]$CreationDate
        if ([string]::IsNullOrWhiteSpace($asText)) {
            return $null
        }

        return [System.Management.ManagementDateTimeConverter]::ToDateTime($asText).ToUniversalTime()
    }
    catch {
        return $null
    }
}

function Test-AuxPowerShellCommandLine {
    param(
        [Parameter(Mandatory)]
        [string]$CommandLine,

        [Parameter(Mandatory)]
        [hashtable]$Definition
    )

    if ([string]::IsNullOrWhiteSpace($CommandLine)) {
        return $false
    }

    $normalizedCommandLine = $CommandLine.Replace('/', '\')
    $regexOptions = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase

    if (-not [string]::IsNullOrWhiteSpace([string]$Definition.ScriptPath)) {
        $escapedPath = [regex]::Escape($Definition.ScriptPath)
        $pattern = '(?:^|[\s"''=])' + $escapedPath + '(?=$|[\s"''])'
        return [regex]::IsMatch($normalizedCommandLine, $pattern, $regexOptions)
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$Definition.ScriptName)) {
        $escapedName = [regex]::Escape($Definition.ScriptName)
        $pattern = '(?:^|[\\\s"''=])' + $escapedName + '(?=$|[\s"''])'
        return [regex]::IsMatch($normalizedCommandLine, $pattern, $regexOptions)
    }

    return $false
}

function Get-MatchingAuxProcesses {
    param(
        [Parameter(Mandatory)]
        [hashtable]$Definition
    )

    $result = @{
        Succeeded = $false
        Records   = @()
        Error     = $null
    }

    if ([string]::IsNullOrWhiteSpace([string]$Definition.MatchType)) {
        $result.Error = 'The auxiliary definition has no process matcher.'
        return $result
    }

    $imageName = if ($Definition.MatchType -eq 'ProcessName') {
        $Definition.ProcessImageName
    }
    else {
        $Definition.PowerShellHostProcessName
    }

    try {
        $rows = @(Get-CimInstance -ClassName Win32_Process `
            -Filter ("Name = '{0}'" -f $imageName) `
            -Property Name, ProcessId, CommandLine, CreationDate `
            -ErrorAction Stop)

        $records = @()
        foreach ($row in $rows) {
            if ($Definition.MatchType -eq 'PowerShellScript') {
                if ([string]::IsNullOrWhiteSpace([string]$row.CommandLine)) {
                    continue
                }
                if (-not (Test-AuxPowerShellCommandLine -CommandLine ([string]$row.CommandLine) -Definition $Definition)) {
                    continue
                }
            }

            $records += ,@{
                Name            = [string]$row.Name
                ProcessId       = [int]$row.ProcessId
                CommandLine     = [string]$row.CommandLine
                CreationTimeUtc = ConvertTo-AuxCreationTimeUtc -CreationDate $row.CreationDate
            }
        }

        $result.Succeeded = $true
        $result.Records = @($records)
        return $result
    }
    catch {
        $result.Error = $_.Exception.Message
        return $result
    }
}

function Select-NewAuxProcessRecords {
    param(
        [object[]]$BeforeRecords,
        [object[]]$AfterRecords,
        [Parameter(Mandatory)][datetime]$LaunchTimeUtc
    )

    $beforePids = @{}
    foreach ($record in @($BeforeRecords)) {
        $beforePids[[string]$record.ProcessId] = $true
    }

    $newRecords = @()
    foreach ($record in @($AfterRecords)) {
        if ($beforePids.ContainsKey([string]$record.ProcessId)) {
            continue
        }

        # A PID that appeared after the before-query but was created before our launch
        # timestamp could be an unrelated concurrent start. Never claim it.
        if ($record.CreationTimeUtc -and ([datetime]$record.CreationTimeUtc -lt $LaunchTimeUtc)) {
            continue
        }

        $newRecords += ,@{
            Name              = [string]$record.Name
            ProcessId         = [int]$record.ProcessId
            CommandLine       = [string]$record.CommandLine
            CreationTimeUtc   = $record.CreationTimeUtc
            SafeCreationProof = ($null -ne $record.CreationTimeUtc)
        }
    }

    return $newRecords
}

function Invoke-AuxScheduledActionIfDue {
    param(
        [hashtable]$ScheduledActionState
    )

    if (-not $ScheduledActionState -or
        $ScheduledActionState.Invoked -or
        $null -eq $ScheduledActionState.Action -or
        $null -eq $ScheduledActionState.DueAt) {
        return
    }

    if ((Get-Date) -lt ([datetime]$ScheduledActionState.DueAt)) {
        return
    }

    # Mark first so a failing action is never executed twice.
    $ScheduledActionState.Invoked = $true
    try {
        $null = & $ScheduledActionState.Action
    }
    catch {
        $ScheduledActionState.Error = $_.Exception
    }
}

function Wait-ForNewAuxProcesses {
    param(
        [Parameter(Mandatory)][hashtable]$Definition,
        [object[]]$BeforeRecords,
        [Parameter(Mandatory)][datetime]$LaunchTimeUtc,
        [hashtable]$ScheduledActionState
    )

    $deadline = $LaunchTimeUtc.AddSeconds([int]$Definition.StartupTimeoutSeconds)
    $latestNewRecords = @()
    $lastPidSignature = $null
    $stableSince = $null
    $queryFailureLogged = $false

    do {
        Invoke-AuxScheduledActionIfDue -ScheduledActionState $ScheduledActionState
        if ($ScheduledActionState -and $ScheduledActionState.Error) {
            break
        }

        $queryResult = Get-MatchingAuxProcesses -Definition $Definition
        if ($queryResult.Succeeded) {
            $latestNewRecords = @(Select-NewAuxProcessRecords `
                -BeforeRecords $BeforeRecords `
                -AfterRecords $queryResult.Records `
                -LaunchTimeUtc $LaunchTimeUtc)

            if ($latestNewRecords.Count -gt 0) {
                $currentPidSignature = ((@($latestNewRecords.ProcessId) | Sort-Object) -join ',')
                if ($currentPidSignature -ne $lastPidSignature) {
                    $lastPidSignature = $currentPidSignature
                    $stableSince = [datetime]::UtcNow
                }
                elseif ($stableSince -and (([datetime]::UtcNow - $stableSince).TotalMilliseconds -ge 500)) {
                    break
                }
            }
            else {
                $lastPidSignature = $null
                $stableSince = $null
            }
        }
        elseif (-not $queryFailureLogged) {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Process query failed while discovering '$($Definition.IdentityKey)': $($queryResult.Error)"
            $queryFailureLogged = $true
        }

        if ([datetime]::UtcNow -ge $deadline) {
            break
        }

        $sleepMilliseconds = 250
        if ($ScheduledActionState -and
            -not $ScheduledActionState.Invoked -and
            $null -ne $ScheduledActionState.DueAt) {
            $millisecondsUntilAction = [Math]::Ceiling((([datetime]$ScheduledActionState.DueAt) - (Get-Date)).TotalMilliseconds)
            if ($millisecondsUntilAction -le 0) {
                $sleepMilliseconds = 0 #flag to immediately invoke the action on the next loop iteration
            }
            elseif ($millisecondsUntilAction -lt $sleepMilliseconds) {
                $sleepMilliseconds = [int]$millisecondsUntilAction
            }
        }

        if ($sleepMilliseconds -gt 0) {
            Start-Sleep -Milliseconds $sleepMilliseconds
        }

    }
    while ($true)

    Invoke-AuxScheduledActionIfDue -ScheduledActionState $ScheduledActionState
    return $latestNewRecords
}

function Test-AuxDefinitionActive {
    param(
        [Parameter(Mandatory)][hashtable]$LifecycleState,
        [Parameter(Mandatory)][string]$IdentityKey
    )

    if ($LifecycleState.SingletonByIdentity.ContainsKey($IdentityKey)) {
        return $true
    }

    foreach ($parentState in @($LifecycleState.AlwaysByParentPid.Values)) {
        if ($parentState.Launches.ContainsKey($IdentityKey)) {
            return $true
        }
    }

    return $false
}

function Confirm-AuxDefinitionCompatibility {
    param(
        [Parameter(Mandatory)][hashtable]$LifecycleState,
        [Parameter(Mandatory)][hashtable]$Definition
    )

    if ($Definition.SourceFormat -eq 'LegacyString') {
        return $true
    }

    $identity = $Definition.IdentityKey
    if (-not $LifecycleState.DefinitionCatalog.ContainsKey($identity)) {
        $LifecycleState.DefinitionCatalog[$identity] = $Definition
        return $true
    }

    $existing = $LifecycleState.DefinitionCatalog[$identity]
    if ($existing.DefinitionSignature.Equals($Definition.DefinitionSignature, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    if (Test-AuxDefinitionActive -LifecycleState $LifecycleState -IdentityKey $identity) {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Incompatible active auxiliary definitions resolve to identity '$identity'. The newer definition was skipped."
        return $false
    }

    Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Auxiliary definition '$identity' changed while inactive; future launches will use the refreshed definition."
    $LifecycleState.DefinitionCatalog[$identity] = $Definition
    return $true
}

function Add-AuxProgramConsumer {
    param(
        [Parameter(Mandatory)][hashtable]$SingletonState,
        [Parameter(Mandatory)][int]$ParentProcessId,
        [Parameter(Mandatory)][string]$ParentProgramName
    )

    $consumerKey = [string]$ParentProcessId
    if ($SingletonState.Consumers.ContainsKey($consumerKey)) {
        return $false
    }

    $SingletonState.Consumers[$consumerKey] = @{
        ProcessId   = $ParentProcessId
        ProgramName = $ParentProgramName
    }
    return $true
}

function Start-AuxProgramPath {
    param(
        [Parameter(Mandatory)][hashtable]$Definition,
        [Parameter(Mandatory)][string]$Reason
    )

    try {
        Write-AuxProgramLog -Title 'AUX START' -ForegroundColor 'Green' -Message "$Reason Launching '$($Definition.Path)' (WindowStyle=$($Definition.WindowStyle), Source=$($Definition.SourceFormat))."
        Start-Process -FilePath $Definition.Path -WindowStyle $Definition.WindowStyle -ErrorAction Stop
        return $true
    }
    catch {
        Write-AuxProgramLog -Title 'AUX START' -ForegroundColor 'DarkYellow' -Message "Failed to launch '$($Definition.Path)': $($_.Exception.Message)"
        return $false
    }
}

function Add-OwnedAuxProcessRecords {
    param(
        [Parameter(Mandatory)][hashtable]$OwnedProcesses,
        [object[]]$Records,
        [Parameter(Mandatory)][string]$IdentityKey
    )

    foreach ($record in @($Records)) {
        $OwnedProcesses[[string]$record.ProcessId] = $record
        if ($record.SafeCreationProof) {
            Write-AuxProgramLog -ForegroundColor 'Green' -Message "Ownership PID discovered for '$IdentityKey': PID $($record.ProcessId), created $($record.CreationTimeUtc)."
        }
        else {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "PID $($record.ProcessId) appeared after launching '$IdentityKey', but creation time was unavailable. It will not be stopped later."
        }
    }
}

function Stop-AllMatchingAuxProcesses {
    <#
    .SYNOPSIS
        Unconditionally terminates all active processes matching an auxiliary definition,
        regardless of PID creation time, ownership tracking, or pre-existence.
    .DESCRIPTION
        Used when LaunchMode=KillExistingAndLaunch (pre-launch cleanup) or
        StopMode=ForceAll (unconditional exit cleanup).
    #>
    param(
        [Parameter(Mandatory)][hashtable]$Definition,
        [Parameter(Mandatory)][string]$Reason
    )

    $matchingQuery = Get-MatchingAuxProcesses -Definition $Definition
    if (-not $matchingQuery.Succeeded) {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Process query failed while attempting forced kill for '$($Definition.IdentityKey)': $($matchingQuery.Error)"
        return
    }

    if ($matchingQuery.Records.Count -eq 0) {
        return
    }

    Write-AuxProgramLog -Title 'AUX KILL' -ForegroundColor 'Magenta' -Message "$Reason Terminating $($matchingQuery.Records.Count) matching process(es) for '$($Definition.IdentityKey)'."

    foreach ($record in @($matchingQuery.Records)) {
        try {
            Stop-Process -Id ([int]$record.ProcessId) -Force -ErrorAction Stop
            Write-AuxProgramLog -Title 'AUX KILL' -ForegroundColor 'Magenta' -Message "Killed matching process '$($Definition.IdentityKey)' [PID: $($record.ProcessId)]."
        }
        catch {
            Write-AuxProgramLog -Title 'AUX KILL' -ForegroundColor 'DarkYellow' -Message "Failed to kill matching PID $($record.ProcessId) for '$($Definition.IdentityKey)': $($_.Exception.Message)"
        }
    }
}

function Start-ConfiguredAuxPrograms {
    param(
        [object[]]$Definitions,
        [Parameter(Mandatory)][hashtable]$LifecycleState,
        [Parameter(Mandatory)][string]$ParentProgramName,
        [Parameter(Mandatory)][int]$ParentProcessId,
        [hashtable]$ScheduledActionState
    )

    foreach ($definition in @($Definitions)) {
        [System.Threading.Monitor]::Enter($LifecycleState.SyncRoot)
        try {
            if (-not (Test-Path -LiteralPath $definition.Path)) {
                Write-AuxProgramLog -Title 'AUX START' -ForegroundColor 'DarkYellow' -Message "Auxiliary launch path not found: '$($definition.Path)'."
                continue
            }

            if (-not (Confirm-AuxDefinitionCompatibility -LifecycleState $LifecycleState -Definition $definition)) {
                continue
            }

            if ($definition.LaunchMode -eq 'Always' -and $definition.StopMode -eq 'Never') {
                $reason = if ($definition.SourceFormat -eq 'LegacyString') {
                    'Legacy launch mode requires launch on every game start.'
                }
                else {
                    'LaunchMode=Always.'
                }
                [void](Start-AuxProgramPath -Definition $definition -Reason $reason)
                continue
            }

            # Handle LaunchMode=KillExistingAndLaunch:
            # First forcibly kill any pre-existing instances of this auxiliary tool,
            # then start a fresh copy.
            if ($definition.LaunchMode -eq 'KillExistingAndLaunch') {
                Stop-AllMatchingAuxProcesses -Definition $definition -Reason 'LaunchMode=KillExistingAndLaunch requested pre-launch cleanup.'

                if ($LifecycleState.PendingIfNotRunning.ContainsKey($definition.IdentityKey)) {
                    $LifecycleState.PendingIfNotRunning.Remove($definition.IdentityKey)
                }

                $singletonState = $null
                if ($definition.StopMode -in @('OwnedOnly', 'ForceAll', 'KillAll')) {
                    if ($LifecycleState.SingletonByIdentity.ContainsKey($definition.IdentityKey)) {
                        $singletonState = $LifecycleState.SingletonByIdentity[$definition.IdentityKey]
                    }
                    else {
                        $singletonState = @{
                            Definition     = $definition
                            Consumers      = @{}
                            OwnedProcesses = @{}
                        }
                        $LifecycleState.SingletonByIdentity[$definition.IdentityKey] = $singletonState
                    }

                    if ($singletonState.Consumers.ContainsKey([string]$ParentProcessId)) {
                        Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Duplicate start event ignored for '$($definition.IdentityKey)' and game PID $ParentProcessId."
                        continue
                    }

                    [void](Add-AuxProgramConsumer -SingletonState $singletonState -ParentProcessId $ParentProcessId -ParentProgramName $ParentProgramName)
                }

                $launchTimeUtc = [datetime]::UtcNow
                if (-not (Start-AuxProgramPath -Definition $definition -Reason 'LaunchMode=KillExistingAndLaunch.')) {
                    if ($singletonState) {
                        $singletonState.Consumers.Remove([string]$ParentProcessId)
                        if ($singletonState.Consumers.Count -eq 0 -and $singletonState.OwnedProcesses.Count -eq 0) {
                            $LifecycleState.SingletonByIdentity.Remove($definition.IdentityKey)
                        }
                    }
                    continue
                }

                if ($definition.StopMode -eq 'OwnedOnly') {
                    # Perform ownership discovery so newly spawned PID is tracked for game-exit cleanup
                    $newRecords = @(Wait-ForNewAuxProcesses `
                        -Definition $definition `
                        -BeforeRecords @() `
                        -LaunchTimeUtc $launchTimeUtc `
                        -ScheduledActionState $ScheduledActionState)

                    if ($newRecords.Count -gt 0) {
                        Add-OwnedAuxProcessRecords -OwnedProcesses $singletonState.OwnedProcesses -Records $newRecords -IdentityKey $definition.IdentityKey
                    }
                    elseif (-not ($ScheduledActionState -and $ScheduledActionState.Error)) {
                        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Ownership PID discovery timed out for '$($definition.IdentityKey)'. No process will be stopped later."
                    }
                }
                else {
                    $LifecycleState.PendingIfNotRunning[$definition.IdentityKey] = [datetime]::UtcNow.AddSeconds([int]$definition.StartupTimeoutSeconds)
                }
                continue
            }

            if ($definition.LaunchMode -eq 'IfNotRunning') {
                $singletonState = $null
                if ($definition.StopMode -in @('OwnedOnly', 'ForceAll', 'KillAll')) {
                    if ($LifecycleState.SingletonByIdentity.ContainsKey($definition.IdentityKey)) {
                        $singletonState = $LifecycleState.SingletonByIdentity[$definition.IdentityKey]
                    }
                    else {
                        $singletonState = @{
                            Definition     = $definition
                            Consumers      = @{}
                            OwnedProcesses = @{}
                        }
                        $LifecycleState.SingletonByIdentity[$definition.IdentityKey] = $singletonState
                    }

                    if ($singletonState.Consumers.ContainsKey([string]$ParentProcessId)) {
                        Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Duplicate start event ignored for '$($definition.IdentityKey)' and game PID $ParentProcessId."
                        continue
                    }
                }

                if ($definition.StopMode -eq 'Never' -and
                    $LifecycleState.PendingIfNotRunning.ContainsKey($definition.IdentityKey)) {
                    $pendingUntil = [datetime]$LifecycleState.PendingIfNotRunning[$definition.IdentityKey]
                    if ([datetime]::UtcNow -lt $pendingUntil) {
                        Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Auxiliary '$($definition.IdentityKey)' launch is still pending; duplicate launch skipped."
                        continue
                    }

                    $LifecycleState.PendingIfNotRunning.Remove($definition.IdentityKey)
                }

                $beforeQuery = Get-MatchingAuxProcesses -Definition $definition
                if (-not $beforeQuery.Succeeded) {
                    Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Process query failed for '$($definition.IdentityKey)'; launch was skipped to avoid a duplicate. $($beforeQuery.Error)"
                    if ($singletonState -and $singletonState.Consumers.Count -eq 0 -and $singletonState.OwnedProcesses.Count -eq 0) {
                        $LifecycleState.SingletonByIdentity.Remove($definition.IdentityKey)
                    }
                    continue
                }

                if ($singletonState) {
                    if (Add-AuxProgramConsumer -SingletonState $singletonState -ParentProcessId $ParentProcessId -ParentProgramName $ParentProgramName) {
                        Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Consumer added for '$($definition.IdentityKey)': $ParentProgramName [PID:$ParentProcessId]."
                    }
                }

                if ($beforeQuery.Records.Count -gt 0) {
                    Write-AuxProgramLog -ForegroundColor 'Cyan' -Message "Auxiliary '$($definition.IdentityKey)' is already running; launch skipped. Existing PID(s): $((@($beforeQuery.Records.ProcessId)) -join ', ')."
                    Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Existing process for '$($definition.IdentityKey)' was deliberately not claimed."
                    if ($definition.MatchType -eq 'PowerShellScript') {
                        Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "PowerShell script match found for '$($definition.IdentityKey)'."
                    }
                    continue
                }

                $launchTimeUtc = [datetime]::UtcNow
                if (-not (Start-AuxProgramPath -Definition $definition -Reason 'No matching process existed.')) {
                    if ($singletonState) {
                        $singletonState.Consumers.Remove([string]$ParentProcessId)
                        if ($singletonState.Consumers.Count -eq 0 -and $singletonState.OwnedProcesses.Count -eq 0) {
                            $LifecycleState.SingletonByIdentity.Remove($definition.IdentityKey)
                        }
                    }
                    continue
                }

                if ($definition.StopMode -eq 'OwnedOnly') {
                    $newRecords = @(Wait-ForNewAuxProcesses `
                        -Definition $definition `
                        -BeforeRecords $beforeQuery.Records `
                        -LaunchTimeUtc $launchTimeUtc `
                        -ScheduledActionState $ScheduledActionState)

                    if ($newRecords.Count -gt 0) {
                        Add-OwnedAuxProcessRecords -OwnedProcesses $singletonState.OwnedProcesses -Records $newRecords -IdentityKey $definition.IdentityKey
                    }
                    elseif (-not ($ScheduledActionState -and $ScheduledActionState.Error)) {
                        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Ownership PID discovery timed out for '$($definition.IdentityKey)'. No process will be stopped later."
                    }
                }
                else {
                    # StopMode=Never or ForceAll does not need ownership discovery. A bounded pending
                    # marker closes the indirect-launch race without delaying boost actions.
                    $LifecycleState.PendingIfNotRunning[$definition.IdentityKey] = [datetime]::UtcNow.AddSeconds([int]$definition.StartupTimeoutSeconds)
                }
                continue
            }

            # Remaining valid combinations: Always + OwnedOnly / Always + ForceAll.
            $parentKey = [string]$ParentProcessId
            if ($LifecycleState.AlwaysByParentPid.ContainsKey($parentKey)) {
                $parentState = $LifecycleState.AlwaysByParentPid[$parentKey]
            }
            else {
                $parentState = @{
                    ProcessId   = $ParentProcessId
                    ProgramName = $ParentProgramName
                    Launches    = @{}
                }
                $LifecycleState.AlwaysByParentPid[$parentKey] = $parentState
            }

            if ($parentState.Launches.ContainsKey($definition.IdentityKey)) {
                Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Duplicate start event ignored for managed Always auxiliary '$($definition.IdentityKey)' and game PID $ParentProcessId."
                continue
            }

            $launchRecord = @{
                LaunchId        = [guid]::NewGuid().ToString('N')
                Definition      = $definition
                OwnedProcesses  = @{}
            }
            $parentState.Launches[$definition.IdentityKey] = $launchRecord

            $beforeQuery = Get-MatchingAuxProcesses -Definition $definition
            $canDiscoverOwnership = $beforeQuery.Succeeded
            if (-not $canDiscoverOwnership) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Before-launch process query failed for '$($definition.IdentityKey)'. LaunchMode=Always will still launch, but no process will be claimed. $($beforeQuery.Error)"
            }

            $launchTimeUtc = [datetime]::UtcNow
            if (-not (Start-AuxProgramPath -Definition $definition -Reason 'LaunchMode=Always.')) {
                $parentState.Launches.Remove($definition.IdentityKey)
                if ($parentState.Launches.Count -eq 0) {
                    $LifecycleState.AlwaysByParentPid.Remove($parentKey)
                }
                continue
            }

            if ($canDiscoverOwnership -and $definition.StopMode -eq 'OwnedOnly') {
                $newRecords = @(Wait-ForNewAuxProcesses `
                    -Definition $definition `
                    -BeforeRecords $beforeQuery.Records `
                    -LaunchTimeUtc $launchTimeUtc `
                    -ScheduledActionState $ScheduledActionState)
                if ($newRecords.Count -gt 0) {
                    Add-OwnedAuxProcessRecords -OwnedProcesses $launchRecord.OwnedProcesses -Records $newRecords -IdentityKey $definition.IdentityKey
                }
                elseif (-not ($ScheduledActionState -and $ScheduledActionState.Error)) {
                    Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Ownership PID discovery timed out for '$($definition.IdentityKey)'. No process will be stopped later."
                }
            }
        }
        catch {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Auxiliary start handling failed for '$($definition.IdentityKey)': $($_.Exception.Message)"
        }
        finally {
            [System.Threading.Monitor]::Exit($LifecycleState.SyncRoot)
        }
    }
}

function Stop-OwnedAuxProcesses {
    param(
        [Parameter(Mandatory)][hashtable]$Definition,
        [Parameter(Mandatory)][hashtable]$OwnedProcesses
    )

    if ($OwnedProcesses.Count -eq 0) {
        return
    }

    $matchingQuery = Get-MatchingAuxProcesses -Definition $Definition
    if (-not $matchingQuery.Succeeded) {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Process query failed while cleaning '$($Definition.IdentityKey)'. Owned processes were retained for safety. $($matchingQuery.Error)"
        return
    }

    $matchingByPid = @{}
    foreach ($record in @($matchingQuery.Records)) {
        $matchingByPid[[string]$record.ProcessId] = $record
    }

    foreach ($ownedRecord in @($OwnedProcesses.Values)) {
        $pidKey = [string]$ownedRecord.ProcessId
        if (-not $matchingByPid.ContainsKey($pidKey)) {
            $stillExists = Get-Process -Id ([int]$ownedRecord.ProcessId) -ErrorAction SilentlyContinue
            if ($stillExists) {
                Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "PID $($ownedRecord.ProcessId) was not stopped because it no longer matches '$($Definition.IdentityKey)'."
            }
            else {
                Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Owned process for '$($Definition.IdentityKey)' already exited: PID $($ownedRecord.ProcessId)."
            }
            continue
        }

        $currentRecord = $matchingByPid[$pidKey]
        if (-not $ownedRecord.SafeCreationProof -or
            $null -eq $ownedRecord.CreationTimeUtc -or
            $null -eq $currentRecord.CreationTimeUtc) {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "PID $($ownedRecord.ProcessId) was not stopped because creation-time identity could not be verified."
            continue
        }

        $currentCreationTicks = ([datetime]$currentRecord.CreationTimeUtc).ToUniversalTime().Ticks
        $ownedCreationTicks = ([datetime]$ownedRecord.CreationTimeUtc).ToUniversalTime().Ticks
        if ($currentCreationTicks -ne $ownedCreationTicks) {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "PID $($ownedRecord.ProcessId) was not stopped because its creation time changed, indicating possible PID reuse."
            continue
        }

        try {
            Stop-Process -Id ([int]$ownedRecord.ProcessId) -ErrorAction Stop
            Write-AuxProgramLog -ForegroundColor 'Green' -Message "Owned auxiliary '$($Definition.IdentityKey)' stopped after its final consumer exited: PID $($ownedRecord.ProcessId)."
        }
        catch {
            Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Failed to stop owned PID $($ownedRecord.ProcessId) for '$($Definition.IdentityKey)': $($_.Exception.Message)"
        }
    }
}

function Stop-ConfiguredAuxPrograms {
    param(
        [Parameter(Mandatory)][hashtable]$LifecycleState,
        [Parameter(Mandatory)][int]$ParentProcessId
    )

    [System.Threading.Monitor]::Enter($LifecycleState.SyncRoot)
    try {
        $consumerKey = [string]$ParentProcessId

        foreach ($identity in @($LifecycleState.SingletonByIdentity.Keys)) {
            $singletonState = $LifecycleState.SingletonByIdentity[$identity]
            if (-not $singletonState.Consumers.ContainsKey($consumerKey)) {
                continue
            }

            $singletonState.Consumers.Remove($consumerKey)
            Write-AuxProgramLog -ForegroundColor 'DarkGray' -Message "Consumer removed for '$identity': game PID $ParentProcessId."

            if ($singletonState.Consumers.Count -gt 0) {
                Write-AuxProgramLog -ForegroundColor 'Cyan' -Message "Auxiliary '$identity' retained because $($singletonState.Consumers.Count) consumer(s) remain."
                continue
            }

            # Final consumer exited!
            if ($singletonState.Definition.StopMode -in @('ForceAll', 'KillAll')) {
                # StopMode=ForceAll unconditionally kills all matching processes on game exit
                Stop-AllMatchingAuxProcesses -Definition $singletonState.Definition -Reason "StopMode=ForceAll requested unconditional cleanup after final consumer (game PID $ParentProcessId) exited."
            }
            else {
                # StopMode=OwnedOnly stops only verified owned PIDs
                Stop-OwnedAuxProcesses -Definition $singletonState.Definition -OwnedProcesses $singletonState.OwnedProcesses
            }

            $LifecycleState.SingletonByIdentity.Remove($identity)
        }

        if ($LifecycleState.AlwaysByParentPid.ContainsKey($consumerKey)) {
            $parentState = $LifecycleState.AlwaysByParentPid[$consumerKey]
            foreach ($launchRecord in @($parentState.Launches.Values)) {
                if ($launchRecord.Definition.StopMode -in @('ForceAll', 'KillAll')) {
                    Stop-AllMatchingAuxProcesses -Definition $launchRecord.Definition -Reason "StopMode=ForceAll requested unconditional cleanup after game PID $ParentProcessId exited."
                }
                else {
                    Stop-OwnedAuxProcesses -Definition $launchRecord.Definition -OwnedProcesses $launchRecord.OwnedProcesses
                }
            }
            $LifecycleState.AlwaysByParentPid.Remove($consumerKey)
        }
    }
    catch {
        Write-AuxProgramLog -ForegroundColor 'DarkYellow' -Message "Auxiliary cleanup failed for game PID ${ParentProcessId}: $($_.Exception.Message)"
    }
    finally {
        [System.Threading.Monitor]::Exit($LifecycleState.SyncRoot)
    }
}
