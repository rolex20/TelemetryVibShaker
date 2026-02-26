function New-IpcServerJobAction {
    param(
        [Parameter(Mandatory)][string]$ModulePath,
        [Parameter(Mandatory)][string]$PipeName
    )

    return {
        param($ModPath, $Pn)
        Import-Module $ModPath -Force
        Start-IpcPipeServer -PipeName $Pn
    }.GetNewClosure()
}
