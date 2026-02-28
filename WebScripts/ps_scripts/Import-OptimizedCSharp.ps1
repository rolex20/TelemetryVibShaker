<#
.SYNOPSIS
Compiles embedded C# source into an optimized, cached assembly and loads it into the current PowerShell session without locking the file.

.DESCRIPTION
Import-OptimizedCSharp is a PowerShell 5.1–compatible helper for scripts that embed substantial C# source code.
It compiles the provided C# source into a DLL using the .NET CodeDOM compiler with /optimize+ enabled, caches the DLL
on disk, and then loads it into the current PowerShell process from a byte array to avoid file locking.

Caching is performed per calling script and per C# source content. The cache key is derived from:
- The calling script’s full path and LastWriteTimeUtc (per-script invalidation)
- The C# source text (per-source invalidation)
- The chosen compilation platform (AnyCPU or x64)
- The set of referenced assemblies

If a matching cached DLL exists and is not older than the calling script file, compilation is skipped and the cached DLL is loaded.
If the cached DLL is missing or older than the calling script, the DLL is recompiled and the cache is refreshed.

This function is intended to replace Add-Type -TypeDefinition for scenarios where you want:
- Optimized C# compilation
- Disk-backed compilation cache
- Reliable reload/overwrite behavior (no assembly file locks)
- Deterministic per-script/per-source cache separation

.PARAMETER Source
The C# source code to compile. Typically provided as a here-string (@' ... '@).

.PARAMETER ExpectedTypeName
The fully qualified type name expected to exist after compilation/load (for example: "MyNamespace.MyType").
This is used to short-circuit if the type is already loaded and to validate that compilation/load succeeded.

.PARAMETER CallerScriptPath
The full path to the PowerShell script that is requesting compilation (typically pass $PSCommandPath).
This is used for cache scoping and to determine whether the cached DLL is older than the calling script.

.PARAMETER CacheRoot
Optional root directory for cached assemblies. If not provided, defaults to a ".cache" directory alongside the calling script.

.PARAMETER Platform
Target platform for compilation: "AnyCPU" (default) or "x64".
Use AnyCPU for maximum compatibility (the assembly will run as 64-bit when loaded by 64-bit PowerShell).
Use x64 only if you must ensure the assembly cannot load in 32-bit PowerShell.

.PARAMETER ReferencedAssemblies
Additional assembly references required by the C# source. Accepts simple assembly file names (e.g. "System.Core.dll")
or full paths. Defaults include System.dll, System.Core.dll, and Microsoft.CSharp.dll.

.PARAMETER Force
Forces recompilation even if a valid cached DLL exists.  (Default is $False)

.OUTPUTS
None. The compiled assembly is loaded into the current process. Types become available for use via:
  [MyNamespace.MyType]::SomeMethod()

.EXAMPLE
# Dot-source the helper, then compile/load a C# class with caching and optimizations
. "$PSScriptRoot\Import-OptimizedCSharp.ps1"

$code = @'
namespace Demo {
  public static class MathUtil {
    public static int Add(int a, int b) { return a + b; }
  }
}
'@

Import-OptimizedCSharp -Source $code -ExpectedTypeName "Demo.MathUtil" -CallerScriptPath $PSCommandPath -Platform AnyCPU
[Demo.MathUtil]::Add(1,2)

.EXAMPLE
# Force rebuild of the cached assembly (e.g. after changing compiler options or troubleshooting)
Import-OptimizedCSharp -Source $code -ExpectedTypeName "Demo.MathUtil" -CallerScriptPath $PSCommandPath -Force

.NOTES
- Designed for Windows PowerShell 5.1 and .NET Framework CodeDOM compilation.
- Loads assemblies from a byte array to avoid locking the cached DLL on disk.
- Cache invalidation is driven by the calling script timestamp and a hash of inputs (script identity, source text, platform, references).
- If compilation fails, the function throws with compiler error details.

#>
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