function Start-WarThunderDistanceMultiplierMonitor {
    param(
        [Parameter(Mandatory)]
        [string]$WatchFile
    )

    $action = {
        function Convert-Seconds($dt) {
            return [int64](Get-Date($dt) -UFormat %s)
        }

        $path = $Event.SourceEventArgs.FullPath
        $changeType = $Event.SourceEventArgs.ChangeType

        $ut = Convert-Seconds $Event.TimeGenerated
        if ($global:ModificationWatcherDebouncer -and ($global:ModificationWatcherDebouncer -eq $ut)) {
            return
        }
        $global:ModificationWatcherDebouncer = $ut

        if (-Not (Test-Path $path)) {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "WAR THUNDER" -Message "Config file not found: $path" -ForegroundColor "Yellow"
            return
        }

        Start-Sleep -Milliseconds 100
        $configContent = Get-Content $path
        $distMulLine = $configContent | Where-Object { $_ -match "rendinstDistMul:r=" }

        if ($distMulLine -match 'rendinstDistMul:r=([0-9\.]+)') {
            $distMulValue = $matches[1]
            Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "WAR THUNDER" -Message "CHANGE_TYPE: $changeType DistanceMultiplier: $distMulValue"

            Add-Type -AssemblyName System.Speech
            $synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer
            $synthesizer.Speak("New distance multiplier: $distMulValue")
            $synthesizer.Dispose()
        }
    }

    return Get-ModificationWatcher -WatchFile $WatchFile -Action $action
}
