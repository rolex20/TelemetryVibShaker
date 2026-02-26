function Start-WarThunderDistanceWatcher {
    param([string]$WatchFile)

    $action = {
        $path = $Event.SourceEventArgs.FullPath
        $configContent = Get-Content $path -ErrorAction SilentlyContinue
        $distMulLine = $configContent | Where-Object { $_ -match 'rendinstDistMul:r=' }
        if ($distMulLine -match 'rendinstDistMul:r=([0-9\.]+)') {
            $distMulValue = $matches[1]
            Add-Type -AssemblyName System.Speech
            $synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer
            $synthesizer.Speak("New distance multiplier: $distMulValue")
        }
    }

    return Get-ModificationWatcher -WatchFile $WatchFile -Action $action
}
