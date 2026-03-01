// This class will calculate an Ideal Processor in Round Robin Fashion considering only Efficiency Processors
// Helpful in my set of TelemetryVibShaker programs which runs only in Efficiency Processors
// Tested in my Intel 12700K and 14700K

// My Intel 14700K has 8 performance cores and 12 efficiency cores.
// CPU numbers 0-15 are performance
// CPU numbers 16-27 are efficiency


using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System;



namespace IdealProcessorEnhanced
{
    internal class ProcessorAssigner
    {
        private const string ProcessorAssignerMutex = @"Global\ProcessorAssignerMutex";
        private const string ProcessorAssignerMMF = @"Global\ProcessorAssignerMMF";

        // Static variables to hold the starting processor number, mutex, and memory-mapped file
        private uint startProcessor;
        private Mutex mutex;
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;
        public string IpcInitMessage { get; private set; }

        // Constructor to initialize the starting processor number
        public ProcessorAssigner(uint maxProcessorIndexInclusive)
        {
            InitializeIpcObjects(ProcessorAssignerMutex, ProcessorAssignerMMF);

            accessor = mmf.CreateViewAccessor();

            startProcessor = maxProcessorIndexInclusive;
            InitializeProcessor();
        }

        private void InitializeIpcObjects(string mutexName, string mmfName, Exception globalOpenFailure = null)
        {
            bool isGlobalName = mutexName.StartsWith(@"Global\", StringComparison.OrdinalIgnoreCase)
                                && mmfName.StartsWith(@"Global\", StringComparison.OrdinalIgnoreCase);

            if (isGlobalName)
            {
                try
                {
                    mutex = Mutex.OpenExisting(mutexName);
                    try
                    {
                        mmf = MemoryMappedFile.OpenExisting(mmfName, MemoryMappedFileRights.ReadWrite);
                    }
                    catch
                    {
                        mutex.Dispose();
                        mutex = null;
                        throw;
                    }

                    IpcInitMessage = $"Using Global IPC objects ({mutexName}, {mmfName}) by opening existing shared objects.";
                    return;
                }
                catch (Exception ex) when (ex is WaitHandleCannotBeOpenedException ||
                                           ex is UnauthorizedAccessException ||
                                           ex is FileNotFoundException ||
                                           ex is IOException)
                {
                    string localMutexName = RemoveGlobalPrefix(mutexName);
                    string localMmfName = RemoveGlobalPrefix(mmfName);
                    InitializeIpcObjects(localMutexName, localMmfName, ex);
                    return;
                }
            }

            // Define the security settings for the mutex
            var mutexSecurity = new MutexSecurity();

            // Allow full control to the "Everyone" group
            var mu_rule = new MutexAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MutexRights.FullControl,
                AccessControlType.Allow);

            mutexSecurity.AddAccessRule(mu_rule);

            bool createdNew;
            mutex = new Mutex(false, mutexName, out createdNew, mutexSecurity);

            // Define the security settings for the memory-mapped file
            var security = new MemoryMappedFileSecurity();

            // Allow full control to the "Everyone" group
            var mm_rule = new AccessRule<MemoryMappedFileRights>(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MemoryMappedFileRights.FullControl,
                AccessControlType.Allow);

            security.AddAccessRule(mm_rule);

            // Create/open the local memory-mapped file with the specified security settings
            mmf = MemoryMappedFile.CreateOrOpen(mmfName, 4, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, security, HandleInheritability.None);

            if (globalOpenFailure == null)
            {
                IpcInitMessage = $"Using local IPC objects ({mutexName}, {mmfName}) with create/open mode.";
            }
            else
            {
                IpcInitMessage = $"Global IPC open failed ({globalOpenFailure.GetType().Name}: {globalOpenFailure.Message}). Falling back to local IPC objects ({mutexName}, {mmfName}).";
            }
        }

        private static string RemoveGlobalPrefix(string name)
        {
            const string globalPrefix = @"Global\";
            return name.StartsWith(globalPrefix, StringComparison.OrdinalIgnoreCase)
                ? name.Substring(globalPrefix.Length)
                : name;
        }

        // Method to initialize the processor number in the memory-mapped file
        private void InitializeProcessor()
        {
            mutex.WaitOne();
            try
            {
                uint currentProcessor;
                accessor.Read(0, out currentProcessor);

                // Clamp/repair invalid values
                if (currentProcessor == 0 || currentProcessor < 16 || currentProcessor > startProcessor)
                {
                    accessor.Write(0, startProcessor);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        // Method to get the next processor number in a round-robin fashion
        public uint GetNextProcessor()
        {
            // Wait for the mutex to ensure exclusive access
            mutex.WaitOne();
            try
            {
                uint currentProcessor;
                // Read the current processor number from the memory-mapped file
                accessor.Read(0, out currentProcessor);

                // Clamp/repair invalid values *before* decrementing/returning
                if (currentProcessor == 0 || currentProcessor < 16 || currentProcessor > startProcessor)
                {
                    currentProcessor = startProcessor;
                }
                // Decrement the processor number
                uint nextProcessor = currentProcessor - 1;
                // If the processor number goes below 16, reset it to the starting processor number
                if (nextProcessor < 16)
                {
                    nextProcessor = startProcessor;
                }
                // Write the updated processor number back to the memory-mapped file
                accessor.Write(0, nextProcessor);
                return currentProcessor;
            }
            finally
            {
                // Release the mutex to allow other processes to access the critical section
                mutex.ReleaseMutex();
            }
        }
    }
}
