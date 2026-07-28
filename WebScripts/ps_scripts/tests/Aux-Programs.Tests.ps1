$ErrorActionPreference = 'Stop'

$testsRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $testsRoot 'Write-VerboseDebug.ps1')
. (Join-Path $testsRoot 'Aux-Programs.ps1')

# Keep test output concise and prevent speech/beep side effects from log formatting.
function Write-AuxProgramLog {
    param(
        [string]$Message,
        [string]$ForegroundColor,
        [string]$Title
    )
}

$script:Passed = 0
$script:Failed = 0

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Name
    )

    if ($Condition) {
        $script:Passed++
        Write-Host "PASS: $Name" -ForegroundColor Green
    }
    else {
        $script:Failed++
        Write-Host "FAIL: $Name" -ForegroundColor Red
    }
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [string]$Name
    )

    Assert-True -Condition ([object]::Equals($Expected, $Actual)) -Name "$Name (expected '$Expected', actual '$Actual')"
}

function New-TestProcessRecord {
    param(
        [int]$ProcessId,
        [datetime]$CreationTimeUtc,
        [string]$Name = 'Helper.exe',
        [string]$CommandLine = ''
    )

    return @{
        Name            = $Name
        ProcessId       = $ProcessId
        CreationTimeUtc = $CreationTimeUtc
        CommandLine     = $CommandLine
    }
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("TelemetryVibShaker-AuxTests-" + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $tempRoot
$launchPath = Join-Path $tempRoot 'Helper.lnk'
$null = New-Item -ItemType File -Path $launchPath

try {
    # Normalization and parsing.
    $legacy = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @($launchPath) -WindowStyle Minimized))[0]
    Assert-Equal 'LegacyString' $legacy.SourceFormat 'Legacy string source format'
    Assert-Equal 'Always' $legacy.LaunchMode 'Legacy LaunchMode'
    Assert-Equal 'Never' $legacy.StopMode 'Legacy StopMode'

    $exeShort = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @("[FanatecMonitor.EXE]$launchPath") -WindowStyle Hidden))[0]
    Assert-Equal 'ProcessShorthand' $exeShort.SourceFormat 'Executable shorthand source format'
    Assert-Equal 'FanatecMonitor' $exeShort.ProcessName 'Executable suffix normalization'
    Assert-Equal 'IfNotRunning' $exeShort.LaunchMode 'Executable shorthand LaunchMode'
    Assert-Equal 'Never' $exeShort.StopMode 'Executable shorthand StopMode'

    $psNameShort = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @("[ps1:MyHelper.ps1]$launchPath") -WindowStyle Minimized))[0]
    Assert-Equal 'PowerShellShorthand' $psNameShort.SourceFormat 'PowerShell filename shorthand source format'
    Assert-Equal 'MyHelper.ps1' $psNameShort.ScriptName 'PowerShell filename shorthand matcher'
    Assert-Equal 'powershell.exe' $psNameShort.PowerShellHostProcessName 'PowerShell shorthand host default'

    $psPathShort = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @("[PS1:C:\Helpers\MyHelper.ps1]$launchPath") -WindowStyle Minimized))[0]
    Assert-Equal 'C:\Helpers\MyHelper.ps1' $psPathShort.ScriptPath 'PowerShell full-path shorthand normalization'

    $structuredExe = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @(@{
        Id            = 'FanatecMonitor'
        Path          = $launchPath
        MatchType     = 'processname'
        ProcessName   = 'FanatecMonitor.exe'
        LaunchMode    = 'ifnotrunning'
        StopMode      = 'ownedonly'
    }) -WindowStyle Normal))[0]
    Assert-Equal 'Structured' $structuredExe.SourceFormat 'Structured executable source format'
    Assert-Equal 'id:fanatecmonitor' $structuredExe.IdentityKey 'Explicit IDs are case-insensitive'
    Assert-Equal 10 $structuredExe.StartupTimeoutSeconds 'Structured startup timeout default'

    $structuredPs = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @(@{
        Path                      = $launchPath
        MatchType                 = 'PowerShellScript'
        ScriptPath                = 'C:/Helpers/MyHelper.ps1'
        ScriptName                = 'Ignored.ps1'
        PowerShellHostProcessName = 'PowerShell.EXE'
    }) -WindowStyle Minimized))[0]
    Assert-Equal 'C:\Helpers\MyHelper.ps1' $structuredPs.ScriptPath 'Structured PowerShell full path'
    Assert-True ([string]::IsNullOrWhiteSpace([string]$structuredPs.ScriptName)) 'ScriptPath takes precedence over ScriptName'
    Assert-Equal 'Always' $structuredPs.LaunchMode 'Structured LaunchMode default'
    Assert-Equal 'Never' $structuredPs.StopMode 'Structured StopMode default'

    $killShort = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @("[kill:FanatecMonitor.EXE]$launchPath") -WindowStyle Hidden))[0]
    Assert-Equal 'KillExistingAndLaunch' $killShort.LaunchMode '[kill:...] shorthand LaunchMode'
    Assert-Equal 'OwnedOnly' $killShort.StopMode '[kill:...] shorthand StopMode'

    $killAllShort = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @("[killall:FanatecMonitor.EXE]$launchPath") -WindowStyle Hidden))[0]
    Assert-Equal 'KillExistingAndLaunch' $killAllShort.LaunchMode '[killall:...] shorthand LaunchMode'
    Assert-Equal 'ForceAll' $killAllShort.StopMode '[killall:...] shorthand StopMode'

    $structuredKillForce = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @(@{
        Id            = 'ForceKillHelper'
        Path          = $launchPath
        MatchType     = 'ProcessName'
        ProcessName   = 'Helper.exe'
        LaunchMode    = 'KillExistingAndLaunch'
        StopMode      = 'ForceAll'
    }) -WindowStyle Normal))[0]
    Assert-Equal 'KillExistingAndLaunch' $structuredKillForce.LaunchMode 'Structured KillExistingAndLaunch LaunchMode'
    Assert-Equal 'ForceAll' $structuredKillForce.StopMode 'Structured ForceAll StopMode'

    $invalidDefinitions = @(ConvertTo-AuxProgramDefinitions -AuxPrograms @(
        '[broken'
        @{ Path = $launchPath; MatchType = 'Unknown'; ProcessName = 'Helper.exe' }
        @{ Path = $launchPath; MatchType = 'ProcessName' }
        @{ Path = $launchPath; MatchType = 'PowerShellScript'; ScriptName = 'NotAScript.exe' }
    ) -WindowStyle Minimized)
    Assert-Equal 0 $invalidDefinitions.Count 'Malformed definitions are skipped'

    $fallbackTimeout = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @(@{
        Path                  = $launchPath
        MatchType             = 'ProcessName'
        ProcessName           = 'Helper.exe'
        StartupTimeoutSeconds = -2
    }) -WindowStyle Minimized))[0]
    Assert-Equal 10 $fallbackTimeout.StartupTimeoutSeconds 'Invalid timeout falls back'

    $duplicateDefinitions = @(ConvertTo-AuxProgramDefinitions -AuxPrograms @(
        @{ Id = 'Duplicate'; Path = $launchPath; MatchType = 'ProcessName'; ProcessName = 'Helper.exe' }
        @{ Id = 'duplicate'; Path = $launchPath; MatchType = 'ProcessName'; ProcessName = 'Helper.exe' }
        @{ Id = 'DUPLICATE'; Path = $launchPath; MatchType = 'ProcessName'; ProcessName = 'Different.exe' }
    ) -WindowStyle Minimized)
    Assert-Equal 1 $duplicateDefinitions.Count 'Duplicate and incompatible same-array identities are skipped'

    # Command-line matching is case-insensitive, quote tolerant, and boundary safe.
    Assert-True (Test-AuxPowerShellCommandLine -CommandLine 'powershell.exe -File "C:\Elsewhere\MYHELPER.PS1" arg' -Definition $psNameShort) 'Filename matching is case-insensitive'
    Assert-True (-not (Test-AuxPowerShellCommandLine -CommandLine 'powershell.exe -File "C:\Elsewhere\NotMyHelper.ps1"' -Definition $psNameShort)) 'Filename matching rejects substring collisions'
    Assert-True (Test-AuxPowerShellCommandLine -CommandLine 'powershell.exe -NoLogo -File "C:/Helpers/MyHelper.ps1" arg' -Definition $structuredPs) 'Full path handles slash and quote differences'
    Assert-True (-not (Test-AuxPowerShellCommandLine -CommandLine 'powershell.exe -File "C:\Helpers\MyHelper.ps1.bak"' -Definition $structuredPs)) 'Full path requires an exact boundary'

    # Pure ownership-difference calculation.
    $launchTime = [datetime]::UtcNow
    $before = @(New-TestProcessRecord -ProcessId 100 -CreationTimeUtc $launchTime.AddMinutes(-1))
    $after = @(
        (New-TestProcessRecord -ProcessId 100 -CreationTimeUtc $launchTime.AddMinutes(-1))
        (New-TestProcessRecord -ProcessId 101 -CreationTimeUtc $launchTime.AddMilliseconds(50))
        (New-TestProcessRecord -ProcessId 102 -CreationTimeUtc $launchTime.AddSeconds(-1))
    )
    $newRecords = @(Select-NewAuxProcessRecords -BeforeRecords $before -AfterRecords $after -LaunchTimeUtc $launchTime)
    Assert-Equal 1 $newRecords.Count 'PID difference selects only new processes'
    Assert-Equal 101 $newRecords[0].ProcessId 'PID difference selects expected PID'
    Assert-True (-not (@($newRecords.ProcessId) -contains 102)) 'Concurrent pre-launch process is not claimed'

    # Consumer set behavior.
    $consumerState = @{ Consumers = @{} }
    Assert-True (Add-AuxProgramConsumer -SingletonState $consumerState -ParentProcessId 501 -ParentProgramName 'GameA.exe') 'First consumer is added'
    Assert-True (-not (Add-AuxProgramConsumer -SingletonState $consumerState -ParentProcessId 501 -ParentProgramName 'GameA.exe')) 'Duplicate consumer is ignored'
    Assert-Equal 1 $consumerState.Consumers.Count 'Consumer collection behaves as a set'

    $script:ScheduledActionCalls = 0
    $scheduledActionState = @{
        Action  = { $script:ScheduledActionCalls++ }
        DueAt   = (Get-Date).AddMilliseconds(-1)
        Invoked = $false
        Error   = $null
    }
    Invoke-AuxScheduledActionIfDue -ScheduledActionState $scheduledActionState
    Invoke-AuxScheduledActionIfDue -ScheduledActionState $scheduledActionState
    Assert-Equal 1 $script:ScheduledActionCalls 'Scheduled boost action runs once when due'

    # Mock process operations for lifecycle tests. No real process is launched or stopped.
    $script:MockStartCalls = @()
    $script:MockStopCalls = @()
    $script:MockQueryResults = @()
    $script:MockSequence = @()
    function Start-Process {
        param(
            [string]$FilePath,
            [string]$WindowStyle,
            $ErrorAction
        )
        $script:MockStartCalls += ,@{ FilePath = $FilePath; WindowStyle = $WindowStyle }
        $script:MockSequence += 'start'
    }
    function Stop-Process {
        param(
            [int]$Id,
            $ErrorAction
        )
        $script:MockStopCalls += $Id
    }
    function Get-MatchingAuxProcesses {
        param(
            [hashtable]$Definition
        )
        if ($script:MockQueryResults.Count -eq 0) {
            return @{ Succeeded = $true; Records = @(); Error = $null }
        }
        $next = $script:MockQueryResults[0]
        if ($script:MockQueryResults.Count -eq 1) {
            $script:MockQueryResults = @()
        }
        else {
            $script:MockQueryResults = @($script:MockQueryResults[1..($script:MockQueryResults.Count - 1)])
        }
        return $next
    }

    # Existing process: skip launch, add consumer, and claim nothing.
    $state = New-AuxProgramLifecycleState
    $existingRecord = New-TestProcessRecord -ProcessId 200 -CreationTimeUtc $launchTime.AddMinutes(-1)
    $script:MockQueryResults = @(@{ Succeeded = $true; Records = @($existingRecord); Error = $null })
    $ownedSingleton = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @(@{
        Id                    = 'SharedHelper'
        Path                  = $launchPath
        MatchType             = 'ProcessName'
        ProcessName           = 'Helper.exe'
        LaunchMode            = 'IfNotRunning'
        StopMode              = 'OwnedOnly'
        StartupTimeoutSeconds = 0
    }) -WindowStyle Minimized))[0]
    Start-ConfiguredAuxPrograms -Definitions @($ownedSingleton) -LifecycleState $state -ParentProgramName 'GameA.exe' -ParentProcessId 501
    Assert-Equal 0 $script:MockStartCalls.Count 'Existing matching process prevents launch'
    Assert-Equal 1 $state.SingletonByIdentity[$ownedSingleton.IdentityKey].Consumers.Count 'Existing process still registers consumer'
    Assert-Equal 0 $state.SingletonByIdentity[$ownedSingleton.IdentityKey].OwnedProcesses.Count 'Existing process is never claimed'

    # A newly appearing PID is claimed only after a successful before/after launch query.
    $newOwnedState = New-AuxProgramLifecycleState
    $newOwnedCreation = [datetime]::UtcNow.AddSeconds(5)
    $script:MockStartCalls = @()
    $script:MockSequence = @()
    $ownedBoostState = @{
        Action  = { $script:MockSequence += 'boost' }
        DueAt   = (Get-Date).AddMilliseconds(-1)
        Invoked = $false
        Error   = $null
    }
    $script:MockQueryResults = @(
        @{ Succeeded = $true; Records = @(); Error = $null }
        @{ Succeeded = $true; Records = @((New-TestProcessRecord -ProcessId 250 -CreationTimeUtc $newOwnedCreation)); Error = $null }
    )
    Start-ConfiguredAuxPrograms -Definitions @($ownedSingleton) -LifecycleState $newOwnedState -ParentProgramName 'GameA.exe' -ParentProcessId 550 -ScheduledActionState $ownedBoostState
    Assert-Equal 1 $script:MockStartCalls.Count 'No existing match launches configured auxiliary'
    Assert-True $newOwnedState.SingletonByIdentity[$ownedSingleton.IdentityKey].OwnedProcesses.ContainsKey('250') 'Newly appearing PID is recorded as owned'
    Assert-Equal 'start,boost' ($script:MockSequence -join ',') 'Due boost runs once after auxiliary launch during discovery'

    # A failed IfNotRunning query fails closed rather than risking a duplicate launch.
    $queryFailureState = New-AuxProgramLifecycleState
    $script:MockStartCalls = @()
    $script:MockQueryResults = @(@{ Succeeded = $false; Records = @(); Error = 'synthetic CIM failure' })
    Start-ConfiguredAuxPrograms -Definitions @($ownedSingleton) -LifecycleState $queryFailureState -ParentProgramName 'GameA.exe' -ParentProcessId 551
    Assert-Equal 0 $script:MockStartCalls.Count 'IfNotRunning query failure skips launch'
    Assert-Equal 0 $queryFailureState.SingletonByIdentity.Count 'Failed query leaves no empty singleton state'

    # Always+OwnedOnly records only new PIDs and associates them with one parent game PID.
    $alwaysOwned = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @(@{
        Id                    = 'AlwaysHelper'
        Path                  = $launchPath
        MatchType             = 'ProcessName'
        ProcessName           = 'Helper.exe'
        LaunchMode            = 'Always'
        StopMode              = 'OwnedOnly'
        StartupTimeoutSeconds = 0
    }) -WindowStyle Minimized))[0]
    $alwaysState = New-AuxProgramLifecycleState
    $preExistingCreation = $newOwnedCreation.AddMinutes(-1)
    $alwaysNewCreation = [datetime]::UtcNow.AddSeconds(5)
    $script:MockStartCalls = @()
    $script:MockQueryResults = @(
        @{ Succeeded = $true; Records = @((New-TestProcessRecord -ProcessId 400 -CreationTimeUtc $preExistingCreation)); Error = $null }
        @{
            Succeeded = $true
            Records   = @(
                (New-TestProcessRecord -ProcessId 400 -CreationTimeUtc $preExistingCreation)
                (New-TestProcessRecord -ProcessId 401 -CreationTimeUtc $alwaysNewCreation)
            )
            Error = $null
        }
    )
    Start-ConfiguredAuxPrograms -Definitions @($alwaysOwned) -LifecycleState $alwaysState -ParentProgramName 'GameAlways.exe' -ParentProcessId 800
    $alwaysLaunch = $alwaysState.AlwaysByParentPid['800'].Launches[$alwaysOwned.IdentityKey]
    Assert-Equal 1 $script:MockStartCalls.Count 'Always+OwnedOnly launches for the parent game'
    Assert-Equal 1 $alwaysLaunch.OwnedProcesses.Count 'Always+OwnedOnly claims only one new PID'
    Assert-True $alwaysLaunch.OwnedProcesses.ContainsKey('401') 'Always+OwnedOnly does not claim pre-existing PID'

    $script:MockStopCalls = @()
    $script:MockQueryResults = @(@{
        Succeeded = $true
        Records   = @(
            (New-TestProcessRecord -ProcessId 400 -CreationTimeUtc $preExistingCreation)
            (New-TestProcessRecord -ProcessId 401 -CreationTimeUtc $alwaysNewCreation)
        )
        Error = $null
    })
    Stop-ConfiguredAuxPrograms -LifecycleState $alwaysState -ParentProcessId 800
    Assert-Equal 1 $script:MockStopCalls.Count 'Always+OwnedOnly stops only its parent launch PID'
    Assert-Equal 401 $script:MockStopCalls[0] 'Always+OwnedOnly selects the newly owned PID'

    # A watcher-owned singleton shared by two games survives the first stop and stops only
    # the exact owned PID after the final consumer exits.
    $ownedCreation = [datetime]::UtcNow
    $ownedRecord = @{
        Name              = 'Helper.exe'
        ProcessId         = 301
        CreationTimeUtc   = $ownedCreation
        CommandLine       = ''
        SafeCreationProof = $true
    }
    $sharedState = New-AuxProgramLifecycleState
    $sharedState.SingletonByIdentity[$ownedSingleton.IdentityKey] = @{
        Definition     = $ownedSingleton
        Consumers      = @{
            '601' = @{ ProcessId = 601; ProgramName = 'GameA.exe' }
            '602' = @{ ProcessId = 602; ProgramName = 'GameB.exe' }
        }
        OwnedProcesses = @{ '301' = $ownedRecord }
    }
    $script:MockStopCalls = @()
    Stop-ConfiguredAuxPrograms -LifecycleState $sharedState -ParentProcessId 601
    Assert-Equal 0 $script:MockStopCalls.Count 'First consumer exit retains shared owned process'
    Assert-Equal 1 $sharedState.SingletonByIdentity[$ownedSingleton.IdentityKey].Consumers.Count 'First consumer removal leaves one consumer'

    $script:MockQueryResults = @(@{
        Succeeded = $true
        Records   = @((New-TestProcessRecord -ProcessId 301 -CreationTimeUtc $ownedCreation))
        Error     = $null
    })
    Stop-ConfiguredAuxPrograms -LifecycleState $sharedState -ParentProcessId 602
    Assert-Equal 1 $script:MockStopCalls.Count 'Final consumer selects one exact owned PID'
    Assert-Equal 301 $script:MockStopCalls[0] 'Final consumer selects expected owned PID'
    Assert-Equal 0 $sharedState.SingletonByIdentity.Count 'Final consumer removes singleton runtime state'

    # Same PID plus a different creation time is treated as PID reuse and left running.
    $reuseState = New-AuxProgramLifecycleState
    $reuseState.SingletonByIdentity[$ownedSingleton.IdentityKey] = @{
        Definition     = $ownedSingleton
        Consumers      = @{ '603' = @{ ProcessId = 603; ProgramName = 'GameC.exe' } }
        OwnedProcesses = @{ '301' = $ownedRecord }
    }
    $script:MockStopCalls = @()
    $script:MockQueryResults = @(@{
        Succeeded = $true
        Records   = @((New-TestProcessRecord -ProcessId 301 -CreationTimeUtc $ownedCreation.AddSeconds(1)))
        Error     = $null
    })
    Stop-ConfiguredAuxPrograms -LifecycleState $reuseState -ParentProcessId 603
    Assert-Equal 0 $script:MockStopCalls.Count 'PID reuse creation-time mismatch prevents termination'

    # StopMode=Never and empty restart state can never request termination.
    $neverState = New-AuxProgramLifecycleState
    $script:MockStopCalls = @()
    Stop-ConfiguredAuxPrograms -LifecycleState $neverState -ParentProcessId 700
    Assert-Equal 0 $script:MockStopCalls.Count 'Missing ownership state after restart requests no termination'

    $script:MockQueryResults = @(@{ Succeeded = $true; Records = @($existingRecord); Error = $null })
    Start-ConfiguredAuxPrograms -Definitions @($exeShort) -LifecycleState $neverState -ParentProgramName 'Game.exe' -ParentProcessId 700
    Stop-ConfiguredAuxPrograms -LifecycleState $neverState -ParentProcessId 700
    Assert-Equal 0 $script:MockStopCalls.Count 'StopMode=Never requests no termination'
    Assert-Equal 0 $neverState.SingletonByIdentity.Count 'StopMode=Never creates no ownership registry'

    $pendingState = New-AuxProgramLifecycleState
    $script:MockStartCalls = @()
    $script:MockQueryResults = @(@{ Succeeded = $true; Records = @(); Error = $null })
    Start-ConfiguredAuxPrograms -Definitions @($exeShort) -LifecycleState $pendingState -ParentProgramName 'GameA.exe' -ParentProcessId 701
    Start-ConfiguredAuxPrograms -Definitions @($exeShort) -LifecycleState $pendingState -ParentProgramName 'GameB.exe' -ParentProcessId 702
    Assert-Equal 1 $script:MockStartCalls.Count 'StopMode=Never pending marker suppresses indirect-launch race'
    Assert-True $pendingState.PendingIfNotRunning.ContainsKey($exeShort.IdentityKey) 'StopMode=Never pending marker is recorded without ownership'

    # Active incompatible IDs are rejected; inactive definitions can refresh safely.
    $conflictState = New-AuxProgramLifecycleState
    $conflictState.DefinitionCatalog[$ownedSingleton.IdentityKey] = $ownedSingleton
    $conflictState.SingletonByIdentity[$ownedSingleton.IdentityKey] = @{
        Definition = $ownedSingleton
        Consumers = @{ '1' = @{ ProcessId = 1 } }
        OwnedProcesses = @{}
    }
    $conflictingDefinition = (@(ConvertTo-AuxProgramDefinitions -AuxPrograms @(@{
        Id          = 'SharedHelper'
        Path        = $launchPath
        MatchType   = 'ProcessName'
        ProcessName = 'DifferentHelper.exe'
        LaunchMode  = 'IfNotRunning'
        StopMode    = 'OwnedOnly'
    }) -WindowStyle Minimized))[0]
    Assert-True (-not (Confirm-AuxDefinitionCompatibility -LifecycleState $conflictState -Definition $conflictingDefinition)) 'Incompatible active identity is rejected'
    $conflictState.SingletonByIdentity.Clear()
    Assert-True (Confirm-AuxDefinitionCompatibility -LifecycleState $conflictState -Definition $conflictingDefinition) 'Inactive identity accepts hot-reloaded definition'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "Aux-Programs tests complete: $script:Passed passed, $script:Failed failed."
if ($script:Failed -gt 0) {
    exit 1
}
