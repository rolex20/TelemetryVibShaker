#Requires -Version 5.1 # Or lower that supports Add-Type

# --- Load Required .NET Assembly ---
# Ensures the MessageBox class is available
try {
    Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop
} catch {
    Write-Error "Failed to load System.Windows.Forms assembly. GUI prompts unavailable."
    # You might want to default to 'No' or exit here if the GUI can't be shown
    Exit 1 # Exit PowerShell script with an error
}

# --- Configuration ---
$question = "Do you want to proceed with the important operation?"
$title = "Confirmation Required"
# -------------------


# --- Display the Confirmation Dialog and Get User Choice ---
Write-Host "Asking user for confirmation via PowerShell GUI..."

# Show the MessageBox:
# Arguments: Message Text, Window Title, Buttons Type, Icon Type
$result = [System.Windows.Forms.MessageBox]::Show(
    $question,
    $title,
    [System.Windows.Forms.MessageBoxButtons]::YesNo,  # Use YesNo buttons
    [System.Windows.Forms.MessageBoxIcon]::Question # Show a question mark icon
    # You can add more options like DefaultButton if needed
    # [System.Windows.Forms.MessageBoxDefaultButton]::Button2 # Default to No
)

Write-Host "User selected: $result" # Output will be 'Yes' or 'No'

# --- Act Based on User Choice ---
# Compare the result against the DialogResult enumeration values
if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
    # User clicked "Yes"
    Write-Host "User clicked YES. Performing YES action..."
    # Add your PowerShell actions for YES here
    # Example:
    Start-Sleep -Seconds 10
    Write-Host "Proceeding..."

}
else {
    # User clicked "No" (or potentially closed the dialog via Alt+F4, which usually defaults to the 'Cancel' or second button action for Yes/No)
    Write-Host "User clicked NO or closed the window. Performing NO action..."
    # Add your PowerShell actions for NO here
    # Example:
    Write-Warning "Operation cancelled by user."
	Start-Sleep -Seconds 10
}
# -------------------------------


Write-Host "PowerShell script finished."

# Optional: Exit with a specific code if needed by a calling process
# Exit 0 # Success