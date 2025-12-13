#Requires -Version 5.1

<#
.SYNOPSIS
Displays a graphical Yes/No prompt to the user and returns $true for Yes, $false for No.

.DESCRIPTION
This function leverages .NET Windows Forms to show a modal MessageBox
with Yes and No buttons. It waits for user input and returns a boolean
value representing their choice.

.PARAMETER Question
The text of the question to display in the message box body. This is mandatory.

.PARAMETER Title
The text to display in the title bar of the message box window.
Defaults to "Confirmation".

.EXAMPLE
PS C:\> . C:\Scripts\Confirm-UserGui.ps1
PS C:\> if (Confirm-UserGui -Question "Do you want to proceed with deletion?") {
>>         Write-Host "Proceeding with deletion..."
>>         # ... deletion logic here ...
>>      } else {
>>         Write-Host "Deletion cancelled by user."
>>      }
# This example dot-sources the script containing the function, then calls it.
# It performs an action only if the user clicks "Yes".

.EXAMPLE
PS C:\> . C:\Scripts\Confirm-UserGui.ps1
PS C:\> $userConsent = Confirm-UserGui -Question "Install optional component?" -Title "Optional Install"
PS C:\> if ($userConsent) { Write-Host "Installing component..." }
# This example uses a custom title and stores the boolean result in a variable.

.OUTPUTS
[boolean] Returns $true if the user clicks "Yes", $false otherwise (No or closing the dialog).

.NOTES
Requires the .NET System.Windows.Forms assembly to be loadable.
If the assembly fails to load, an error is written and the function returns $false.
#>
function Confirm-UserGui {
    [CmdletBinding()] # Enables common parameters like -Verbose
    param(
        [Parameter(Mandatory=$true, HelpMessage="The question to ask the user.")]
        [string]$Question,

        [Parameter(Mandatory=$false, HelpMessage="The title for the confirmation window.")]
        [string]$Title = "Confirmation"
    )

    # --- Load Required .NET Assembly ---
    try {
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction Stop
        Write-Verbose "System.Windows.Forms assembly loaded or already present."
    } catch {
        Write-Error "Failed to load System.Windows.Forms assembly. Cannot display GUI prompt."
        # Indicate failure or inability to confirm positively.
        return $false
    }

    # --- Display the Confirmation Dialog and Get User Choice ---
    Write-Verbose "Displaying confirmation prompt: Title='$Title', Question='$Question'"

    # Show the MessageBox:
    $result = [System.Windows.Forms.MessageBox]::Show(
        $Question,
        $Title,
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
        # Optional: Add default button if desired
        # [System.Windows.Forms.MessageBoxDefaultButton]::Button2 # Default to No
    )

    # --- Return Boolean Result ---
    # Compare the result against the DialogResult enumeration values
    if ($result -eq [System.Windows.Forms.DialogResult]::Yes) {
        Write-Verbose "User selected: Yes"
        return $true
    }
    else {
        Write-Verbose "User selected: No (or closed dialog)"
        return $false
    }
}

# Optional: Export the function if saving as a module (.psm1)
# Export-ModuleMember -Function Confirm-UserGui
