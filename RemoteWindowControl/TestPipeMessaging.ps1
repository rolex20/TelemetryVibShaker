$pipeName = "PerformanceMonitorCommandsPipe"
$pipeClient = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::Out)
$pipeClient.Connect()

$streamWriter = New-Object System.IO.StreamWriter($pipeClient)
$streamWriter.AutoFlush = $true

$message = "ERRORS"
$streamWriter.WriteLine($message)
