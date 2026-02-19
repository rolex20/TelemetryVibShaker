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



namespace IdealProcessorEnhanced
{
    internal class ProcessorAssigner
    {
        // Static variables to hold the starting processor number, mutex, and memory-mapped file
        private uint startProcessor;
        private Mutex mutex;
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor accessor;

        // Constructor to initialize the starting processor number
        public ProcessorAssigner(uint maxProcessor)
        {
            // Define the security settings for the mutex
            var mutexSecurity = new MutexSecurity();

            // Allow full control to the "Everyone" group
            var mu_rule = new MutexAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MutexRights.FullControl,
                AccessControlType.Allow);

            mutexSecurity.AddAccessRule(mu_rule);


            bool createdNew;
            mutex = new Mutex(false, @"Global\ProcessorAssignerMutex", out createdNew, mutexSecurity);
            //   mutex = new Mutex(false, "GlobalProcessorAssignerMutex");



            // Define the security settings for the memory-mapped file
            var security = new MemoryMappedFileSecurity();

            // Allow full control to the "Everyone" group
            var mm_rule = new AccessRule<MemoryMappedFileRights>(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MemoryMappedFileRights.FullControl,
                AccessControlType.Allow);

            security.AddAccessRule(mm_rule);

            // Create the global memory-mapped file with the specified security settings
            mmf = MemoryMappedFile.CreateOrOpen(@"Global\ProcessorAssignerMMF", 4, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, security, HandleInheritability.None);
            //mmf = MemoryMappedFile.CreateOrOpen("GlobalProcessorAssignerMMF", 4);            

            accessor = mmf.CreateViewAccessor();

            startProcessor = maxProcessor;
            InitializeProcessor();
        }

        // Method to initialize the processor number in the memory-mapped file
        private void InitializeProcessor()
        {
            // Wait for the mutex to ensure exclusive access
            mutex.WaitOne();
            try
            {
                int currentProcessor;
                // Read the current processor number from the memory-mapped file
                accessor.Read(0, out currentProcessor);
                // If the current processor number is 0 (uninitialized), set it to the starting processor number
                if (currentProcessor == 0)
                {
                    accessor.Write(0, startProcessor);
                }
            }
            finally
            {
                // Release the mutex to allow other processes to access the critical section
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
