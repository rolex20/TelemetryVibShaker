# Run in a fresh Windows PowerShell 5.1 process. Tests use isolated pipe/mutex
# names and a two-second idle period; no production server/window is touched.
$ErrorActionPreference = 'Stop'
$path = Join-Path (Split-Path -Parent $PSScriptRoot) 'Stutter-Hunter-IPC.ps1'
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
$source = Get-Content -Raw -Encoding UTF8 $path
$code = [regex]::Match($source, '(?s)\$code = @''\r?\n(.*?)\r?\n''@').Groups[1].Value
Add-Type -TypeDefinition $code
$serverFunction = $ast.FindAll({
    param($node)
    $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Start-StutterHunterServer'
}, $true)[0].Extent.Text

function Assert-True([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "FAIL: $Name" }
    Write-Host "PASS: $Name" -ForegroundColor Green
}

function New-TestServer([string]$Name = ('StutterHunterTest.' + [guid]::NewGuid().ToString('N'))) {
    $shell = [powershell]::Create()
    [void]$shell.AddScript($serverFunction + @'
function Restore-PowerShellWindow {}
$mutexName = $args[0]
$pipeName = $args[0]
$IdleExitSeconds = 2
$SampleIntervalMs = 100
$CpuSpikeThreshold = 100000
$HardFaultThreshold = 10
Start-StutterHunterServer
'@).AddArgument($Name)
    $running = $shell.BeginInvoke()
    return @{ Shell = $shell; Running = $running; Name = $Name }
}

function Send-TestCommand($Server, [string]$Command) {
    $client = [System.IO.Pipes.NamedPipeClientStream]::new('.', $Server.Name, [System.IO.Pipes.PipeDirection]::InOut)
    try {
        $client.Connect(3000)
        $writer = [System.IO.StreamWriter]::new($client)
        $reader = [System.IO.StreamReader]::new($client)
        $writer.WriteLine($Command)
        $writer.Flush()
        $reply = $reader.ReadLineAsync()
        if (-not $reply.Wait(3000)) { throw "No reply to $Command" }
        return $reply.Result
    }
    finally { $client.Dispose() }
}

function Complete-TestServer($Server) {
    Assert-True ($Server.Running.AsyncWaitHandle.WaitOne(5000)) 'Server exits without a client waking its pipe'
    $Server.Shell.EndInvoke($Server.Running) | Out-Null
    if ($Server.Shell.Streams.Error.Count) { throw ($Server.Shell.Streams.Error | Out-String) }
    $Server.Shell.Dispose()
}

$server = $null
$child = $null
$duplicate = $null
try {
    $defaultTimeout = $ast.ParamBlock.Parameters | Where-Object { $_.Name.VariablePath.UserPath -eq 'IdleExitSeconds' }
    Assert-True ($defaultTimeout.DefaultValue.Value -eq 300) 'Production idle default is five minutes'

    $timer = [System.Diagnostics.Stopwatch]::StartNew()
    $server = New-TestServer
    Complete-TestServer $server
    Assert-True ($timer.Elapsed.TotalSeconds -ge 1.8) 'Initially empty server waits for the idle deadline'
    $server = $null

    $server = New-TestServer
    Assert-True ((Send-TestCommand $server "ADD $PID test") -eq "OK ADDED $PID") 'ADD accepts a live target'
    Assert-True ((Send-TestCommand $server "ADD $PID renamed") -eq "OK ADDED $PID") 'Repeated ADD updates the existing target'
    Assert-True ((Send-TestCommand $server 'LIST') -eq "OK ${PID}:renamed") 'LIST reports the updated target'
    Assert-True (-not $server.Running.AsyncWaitHandle.WaitOne(2300)) 'A live target cancels idle shutdown'

    $duplicate = New-TestServer $server.Name
    Complete-TestServer $duplicate
    $duplicate = $null
    Assert-True ((Send-TestCommand $server 'PING') -eq 'OK PONG') 'Duplicate server returns without disturbing the active server'

    Assert-True ((Send-TestCommand $server "REMOVE $PID") -eq "OK REMOVED $PID") 'REMOVE accepts a tracked target'
    Assert-True (-not $server.Running.AsyncWaitHandle.WaitOne(1100)) 'Idle deadline is measured from last target removal'
    Assert-True ((Send-TestCommand $server "ADD $PID test") -eq "OK ADDED $PID") 'Adding during idle cancels the pending timer'
    Assert-True (-not $server.Running.AsyncWaitHandle.WaitOne(1200)) 'Canceled idle deadline does not shut down the server'
    [void](Send-TestCommand $server "REMOVE $PID")
    $timer.Restart()
    Complete-TestServer $server
    Assert-True ($timer.Elapsed.TotalSeconds -ge 1.8) 'Last removal arms a fresh full idle period'
    $server = $null

    # Disable CPU sampling during this case so removal can only come from the
    # process-exit event, rather than the monitor noticing an absent PID.
    [StutterHunterIpcRunner]::Start(100, 100000, 10, 2)
    Assert-True ([StutterHunterIpcRunner]::AddTarget($PID, 'test')) 'Event test registers its initial target'
    $monitorField = [StutterHunterIpcRunner].GetField('_thread', [System.Reflection.BindingFlags]'NonPublic,Static')
    $stopField = [StutterHunterIpcRunner].GetField('_stopRequested', [System.Reflection.BindingFlags]'NonPublic,Static')
    $stopField.SetValue($null, $true)
    Assert-True ($monitorField.GetValue($null).Join(2000)) 'Sampling thread stopped for isolated process-exit test'
    $child = Start-Process powershell.exe -WindowStyle Hidden -ArgumentList '-NoProfile', '-Command', 'Start-Sleep -Seconds 60' -PassThru
    Assert-True ([StutterHunterIpcRunner]::AddTarget($child.Id, 'child')) 'Child process registers an exit handler'
    [void][StutterHunterIpcRunner]::RemoveTarget($PID)
    Assert-True (-not [StutterHunterIpcRunner]::IdleExpired.WaitOne(2200)) 'Another target prevents idle shutdown'
    $child.Kill()
    $child.WaitForExit()
    Assert-True ([StutterHunterIpcRunner]::IdleExpired.WaitOne(5000)) 'Process-exit event removes the last target and arms idle shutdown'
    Assert-True ([StutterHunterIpcRunner]::TargetCount() -eq 0) 'Exited process is removed without sampling'
    Assert-True (-not [StutterHunterIpcRunner]::AddTarget($PID, 'late')) 'Targets are rejected once idle shutdown wins'
    [StutterHunterIpcRunner]::Stop()

    $server = New-TestServer
    Assert-True ((Send-TestCommand $server 'ADD -1 invalid') -like 'ERR*') 'Invalid targets do not cancel idle shutdown'
    Complete-TestServer $server
    $server = $null

    $server = New-TestServer
    [void](Send-TestCommand $server "ADD $PID test")
    Assert-True ((Send-TestCommand $server 'SHUTDOWN') -eq 'OK SHUTDOWN') 'Explicit shutdown still replies'
    Complete-TestServer $server
    $server = $null
    Assert-True ([StutterHunterIpcRunner]::TargetCount() -eq 0) 'Shutdown cleans up target registrations'
}
finally {
    foreach ($instance in @($duplicate, $server)) {
        if ($instance) { $instance.Shell.Stop(); $instance.Shell.Dispose() }
    }
    [StutterHunterIpcRunner]::Stop()
    if ($child) {
        if (-not $child.HasExited) { $child.Kill(); $child.WaitForExit() }
        $child.Dispose()
    }
}
