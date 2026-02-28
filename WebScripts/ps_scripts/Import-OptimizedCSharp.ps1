function Import-OptimizedCSharp {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Source,
        [Parameter(Mandatory)] [string] $ExpectedTypeName,

        [Parameter(Mandatory)] [string] $CallerScriptPath,

        [string] $CacheRoot,

        [ValidateSet("AnyCPU", "x64")]
        [string] $Platform = "AnyCPU",

        [string[]] $ReferencedAssemblies = @(
            "System.dll",
            "System.Core.dll",
            "Microsoft.CSharp.dll"
        ),

        [switch] $Force
    )

    # Already loaded?
    $already = [AppDomain]::CurrentDomain.GetAssemblies() |
        ForEach-Object { $_.GetType($ExpectedTypeName, $false, $false) } |
        Where-Object { $_ } |
        Select-Object -First 1
    if ($already) { return }

    $callerItem = Get-Item -LiteralPath $CallerScriptPath -ErrorAction Stop

    if (-not $CacheRoot) {
        $CacheRoot = Join-Path (Split-Path -Parent $CallerScriptPath) ".cache"
    }
    if (-not (Test-Path -LiteralPath $CacheRoot)) {
        New-Item -ItemType Directory -Path $CacheRoot -Force | Out-Null
    }

    # Cache key = per-script + per-source + platform (+ refs)
    $scriptId = [IO.Path]::GetFileNameWithoutExtension($CallerScriptPath)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes(
            ($callerItem.FullName + "`n" + $callerItem.LastWriteTimeUtc.ToString("o") + "`n") +
            ($Platform + "`n") +
            (($ReferencedAssemblies -join ";") + "`n") +
            $Source
        )
        $hash = ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") }) -join ""
    }
    finally { $sha.Dispose() }

    $dllName = "{0}.{1}.{2}.dll" -f $scriptId, $Platform.ToLowerInvariant(), $hash.Substring(0,16)
    $dllPath = Join-Path $CacheRoot $dllName

    $dllItem = Get-Item -LiteralPath $dllPath -ErrorAction SilentlyContinue
    $needsCompile = $Force.IsPresent -or (-not $dllItem) -or ($dllItem.LastWriteTimeUtc -lt $callerItem.LastWriteTimeUtc)

    if ($needsCompile) {
        # Make sure compiler types are available
        Add-Type -AssemblyName "Microsoft.CSharp" -ErrorAction Stop
        Add-Type -AssemblyName "System" -ErrorAction Stop

        # IMPORTANT: use parameterless ctor (PS 5.1-friendly)
        $provider = New-Object Microsoft.CSharp.CSharpCodeProvider

        $cp = New-Object System.CodeDom.Compiler.CompilerParameters
        $cp.GenerateExecutable = $false
        $cp.GenerateInMemory   = $false
        $cp.OutputAssembly     = $dllPath
        $cp.IncludeDebugInformation = $false
        $cp.WarningLevel = 4
        $cp.TreatWarningsAsErrors = $false

        foreach ($ref in $ReferencedAssemblies) { [void]$cp.ReferencedAssemblies.Add($ref) }

        $platOpt = if ($Platform -eq "x64") { "/platform:x64" } else { "/platform:anycpu" }
        $cp.CompilerOptions = "/target:library /optimize+ /debug- $platOpt"

        $results = $provider.CompileAssemblyFromSource($cp, $Source)

        if ($results.Errors -and $results.Errors.Count -gt 0) {
            $errs = $results.Errors | ForEach-Object { $_.ToString() }
            throw ("C# compile failed:`n" + ($errs -join "`n"))
        }
    }

    if (-not (Test-Path -LiteralPath $dllPath)) {
        throw "Compile step did not produce DLL: $dllPath"
    }

    # Load without locking the DLL
    $raw = [IO.File]::ReadAllBytes($dllPath)
    [void][System.Reflection.Assembly]::Load([byte[]]$raw)

    # Verify expected type exists now
    $t = [AppDomain]::CurrentDomain.GetAssemblies() |
        ForEach-Object { $_.GetType($ExpectedTypeName, $false, $false) } |
        Where-Object { $_ } |
        Select-Object -First 1
    if (-not $t) { throw "Loaded assembly, but type '$ExpectedTypeName' was not found." }
}