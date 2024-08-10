# Create a global list
$global:timestampList = [System.Collections.Generic.List[datetime]]::new()

# Function to add a timestamp to the global list
function Add-Timestamp {
    $timestamp = Get-Date
    $global:timestampList.Add($timestamp)
}

# Function to check if the current timestamp is 100 milliseconds older than each item in the list
function Is-OlderThan100ms {
    $currentTimestamp = Get-Date
    foreach ($timestamp in $global:timestampList) {
        if (($currentTimestamp - $timestamp).TotalMilliseconds -lt 100) {
            return $false
        }
    }
    return $true
}

# Add several timestamps
for ($i = 0; $i -lt 5; $i++) {
    Start-Sleep -Seconds 1  # Adding a delay to get different timestamps
    Add-Timestamp
    
}

# Display the timestamps
$global:timestampList

# Check if the current timestamp is 100 milliseconds older than each item in the list
Is-OlderThan100ms
