/*
  WHY WE DO EXPLICIT SECURITY + PRIVILEGES FOR Global\ProcessorAssigner*

  TelemetryVibShaker and MicroTaskScheduler coordinate CPU “round-robin” selection using two named
  kernel objects in the GLOBAL namespace:

    - Mutex:       Global\ProcessorAssignerMutex
    - FileMapping: Global\ProcessorAssignerMMF   (4 bytes storing the next CPU index)

  If we create these objects WITHOUT specifying security (e.g., new Mutex(..., "Global\\...") or
  MemoryMappedFile.CreateOrOpen("Global\\...", ...)), Windows applies a *default security descriptor*.
  That default DACL is taken from the *creator process token* and is not guaranteed to allow later
  access from other identities (notably the MicroTaskScheduler Windows Service running as LocalSystem).

  In the .NET Framework 4.8 MicroTaskScheduler implementation we used managed ACL types
  (MutexSecurity / MemoryMappedFileSecurity) to grant “Everyone” FullControl at creation time,
  ensuring any process (interactive apps, services, LocalSystem) can open/update the objects.

  In modern .NET (net9.0-windows), Windows-only “object ACL” features are intentionally not part of
  the cross-platform base API surface by default. Some are shipped as optional Windows-only packages
  (e.g., System.Threading.AccessControl for Mutex/Semaphore/Event ACLs), and some .NET Framework-era
  patterns are not available in the same way across all target frameworks. To keep TelemetryVibShaker
  self-contained and deterministic, we set the security descriptor ourselves (DACL = Everyone:Full)
  using minimal Win32 interop.

  Global namespace note:
    - Creating Global\ file-mapping objects from an interactive session requires SeCreateGlobalPrivilege.
      Services (including LocalSystem) typically have it already.
    - The privilege check applies to *creation*, not opening an existing object.
    - If privilege enabling fails, we should fall back to opening existing objects rather than crashing.

  SECURITY NOTE:
    - Granting Everyone full control is intentional here because the shared state is just a small counter.
      Do NOT reuse this pattern for sensitive IPC without tightening the DACL.

  Usage in ProcessorAssigner constructor (future code snippet):
    public ProcessorAssigner(uint maxProcessor)
    {
        mutex = GlobalIpc.CreateOrOpenGlobalMutexEveryoneFull(@"Global\ProcessorAssignerMutex");
        mmf   = GlobalIpc.CreateOrOpenGlobalMmfEveryoneFull(@"Global\ProcessorAssignerMMF", 4);
        accessor = mmf.CreateViewAccessor();

        startProcessor = maxProcessor;
        InitializeProcessor();
    }


*/

using System.ComponentModel;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TelemetryVibShaker
{
    internal static class GlobalIpc
    {
        // DACL:
        //   SY = LocalSystem, BA = Built-in Administrators, WD = Everyone
        //   GA = Generic All (Full Control)
        // If you truly want ONLY Everyone, use: "D:(A;;GA;;;WD)"
        private const string SddlEveryoneFull = "D:(A;;GA;;;SY)(A;;GA;;;BA)(A;;GA;;;WD)";

        private const int ERROR_NOT_ALL_ASSIGNED = 1300;
        private const uint SDDL_REVISION_1 = 1;

        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;

        private const uint MUTEX_ALL_ACCESS = 0x001F0001; // MUTEX_ALL_ACCESS (0x1F0001) :contentReference[oaicite:2]{index=2}
        private const uint PAGE_READWRITE = 0x04;
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        // Custom delegate (fixes your CS1677/CS1073/CS1615 errors)
        private delegate IntPtr KernelCreateDelegate(ref SECURITY_ATTRIBUTES sa);

        public static Mutex CreateOrOpenGlobalMutexEveryoneFull(string name)
        {
            // CreateMutexEx allows us to provide SECURITY_ATTRIBUTES with a DACL. :contentReference[oaicite:3]{index=3}
            IntPtr hMutex = CreateKernelObjectWithSddl(
                sddl: SddlEveryoneFull,
                create: (ref SECURITY_ATTRIBUTES sa) => CreateMutexExW(ref sa, name, 0, MUTEX_ALL_ACCESS),
                objectKind: "mutex");

            // Wrap native handle into managed Mutex for WaitOne/ReleaseMutex usage.
            var m = new Mutex(false);
            var old = m.SafeWaitHandle;
            m.SafeWaitHandle = new SafeWaitHandle(hMutex, ownsHandle: true);
            old?.Dispose();
            return m;
        }

        public static MemoryMappedFile CreateOrOpenGlobalMmfEveryoneFull(string name, int bytes)
        {
            // Creating a file-mapping object in Global\ from a non-session-0 process requires SeCreateGlobalPrivilege. :contentReference[oaicite:4]{index=4}
            EnablePrivilegeOrThrow("SeCreateGlobalPrivilege");

            IntPtr hMap = CreateKernelObjectWithSddl(
                sddl: SddlEveryoneFull,
                create: (ref SECURITY_ATTRIBUTES sa) => CreateFileMappingW(
                    INVALID_HANDLE_VALUE, ref sa, PAGE_READWRITE, 0, (uint)bytes, name),
                objectKind: "file-mapping");

            // Keep our raw handle open until managed open succeeds (avoid "object disappears" races).
            var mmf = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.ReadWrite);

            CloseHandle(hMap);
            return mmf;
        }

        private static IntPtr CreateKernelObjectWithSddl(string sddl, KernelCreateDelegate create, string objectKind)
        {
            // Convert SDDL -> SECURITY_DESCRIPTOR; buffer must be freed with LocalFree. :contentReference[oaicite:5]{index=5}
            if (!ConvertStringSecurityDescriptorToSecurityDescriptorW(sddl, SDDL_REVISION_1, out var pSd, out _))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "ConvertStringSecurityDescriptorToSecurityDescriptorW failed");

            try
            {
                var sa = new SECURITY_ATTRIBUTES
                {
                    nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                    lpSecurityDescriptor = pSd,
                    bInheritHandle = false
                };

                IntPtr h = create(ref sa);
                if (h == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"Create {objectKind} failed");

                return h;
            }
            finally
            {
                LocalFree(pSd);
            }
        }

        private static void EnablePrivilegeOrThrow(string privilegeName)
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var hToken))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcessToken failed");

            try
            {
                if (!LookupPrivilegeValueW(null, privilegeName, out var luid))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"LookupPrivilegeValueW failed for {privilegeName}");

                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = SE_PRIVILEGE_ENABLED }
                };

                if (!AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "AdjustTokenPrivileges failed");

                // AdjustTokenPrivileges can succeed but still not enable (ERROR_NOT_ALL_ASSIGNED). :contentReference[oaicite:6]{index=6}
                int err = Marshal.GetLastWin32Error();
                if (err == ERROR_NOT_ALL_ASSIGNED)
                    throw new InvalidOperationException(
                        $"Privilege '{privilegeName}' is not present/enabled in this process token. " +
                        $"If you are an admin, run TelemetryVibShaker elevated.");
            }
            finally
            {
                CloseHandle(hToken);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)] public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LookupPrivilegeValueW(string? lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(
            IntPtr TokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState,
            int BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(
            string StringSecurityDescriptor,
            uint StringSDRevision,
            out IntPtr SecurityDescriptor,
            out uint SecurityDescriptorSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateMutexExW(
            ref SECURITY_ATTRIBUTES lpMutexAttributes,
            string lpName,
            uint dwFlags,
            uint dwDesiredAccess);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFileMappingW(
            IntPtr hFile,
            ref SECURITY_ATTRIBUTES lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            string lpName);
    }

}
