function Get-IpcServerJobAction {
    param(
        [Parameter(Mandatory=$true)][string]$ModulePath,
        [Parameter(Mandatory=$true)][string]$PipeName,
        [Parameter(Mandatory=$true)][string]$MutexName
    )

    return {
        param($ModulePathArg, $PipeNameArg, $MutexNameArg)
        Import-Module $ModulePathArg -Force
        Start-IpcPipeServer -PipeName $PipeNameArg -MutexName $MutexNameArg
    }.GetNewClosure()
}
