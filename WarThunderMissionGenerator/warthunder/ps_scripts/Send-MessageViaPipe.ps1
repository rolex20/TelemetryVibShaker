function Send-MessageViaPipe {
    param (
        [string]$pipeName,
        [string]$message
    )

    $pipePath = "\\.\pipe\$pipeName"

    try {
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::Out)
        $pipe.Connect(1000)

        $writer = New-Object System.IO.StreamWriter($pipe)
        $writer.AutoFlush = $true
        $writer.WriteLine($message)
        $writer.Close()
        $pipe.Close()

        Write-Output "Message sent successfully."
    } catch {
        Write-Error "Failed to send message: $_"
    }
}

#You can use this function like this:

#Send-MessageViaPipe -pipeName "MyPipe" -message "Hello, World!"

Send-MessageViaPipe -pipeName "WarThunderExporterPipeCommands" -message "MONITOR"