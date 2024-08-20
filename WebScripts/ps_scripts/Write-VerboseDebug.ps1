function Write-VerboseDebug {
    param (
        [datetime]$Timestamp,
        [string]$Title,
        [string]$Message,
        [string]$ForegroundColor = "White",  # Default color is White
        [bool]$Speak = $false
    )

    $formattedTimestamp = $Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff") #maximum is fffffff
    $output = "[{0}] [{1,-10}] [{2}]" -f $formattedTimestamp, $Title, $Message #another option is [{0,-16}]

    if ($ForegroundColor -eq "Red") {
        [console]::beep(1000, 500)  # Play system error sound
    }

    Write-Host $output -ForegroundColor $ForegroundColor

    if ($ForegroundColor -eq "Red" -OR $Speak) {
        Add-Type -AssemblyName System.Speech
        $synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer
        $synthesizer.Speak("PS-Watcher WARNING: $Title  $Message")
    }
}

# Example usage:
#Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "This is a critical error message." -ForegroundColor "Red"
#Write-VerboseDebug -Timestamp (Get-Date) -Title "NORMAL" -Message "This is a debug message." -ForegroundColor "White"
#Write-VerboseDebug -Timestamp (Get-Date) -Title "NORMAL" -Message "This is a debug message."
