// Monitor Gpu Utilization, Temperature, Fan Speed
// Based on GpuPerfCounters (GitHub)
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq.Expressions;

public class NvidiaGpu
{
    private readonly IntPtr _device;
    public readonly int DeviceId;

    private Nvml.NvmlUtilization _utilization;
    private uint _fanSpeed;
    private uint _power;
    private uint _smClock;
    private uint _temperature;

    public readonly string Name;
    public readonly string Uuid;

    private int lastExceptionsCounter;
    public int ReadResetExceptionsCounter { get
        {
            int result = lastExceptionsCounter;
            lastExceptionsCounter = 0;
            return result;
        } }


    public NvidiaGpu(int deviceNumber = 0)
	{
        try
        {
            var err = Nvml.nvmlInit_v2();
            if (err == Nvml.nvmlReturn.NVML_SUCCESS)
            {
                DeviceId = deviceNumber;

                err = Nvml.nvmlDeviceGetHandleByIndex((uint)deviceNumber, out _device);
                if (err == Nvml.nvmlReturn.NVML_SUCCESS)
                {
                    var name = new StringBuilder(250);
                    var uuid = new StringBuilder(250);
                    if (Nvml.nvmlDeviceGetName(_device, name, (uint)name.Capacity) == Nvml.nvmlReturn.NVML_SUCCESS &&
                        Nvml.nvmlDeviceGetUUID(_device, uuid, (uint)uuid.Capacity) == Nvml.nvmlReturn.NVML_SUCCESS)
                    {
                        Name = name.ToString();
                        Uuid = uuid.ToString();
                        return;
                    }
                }
            }
        } catch
        {
            lastExceptionsCounter++;
        }

        Name = "<FailedToInitialize>";
        Uuid = Name;
        DeviceId = -1;
    }

    //Percent of time over the past sample period during which one or more kernels was executing on the GPU
    public int Utilization { get {
            try {

                var err = Nvml.nvmlDeviceGetUtilizationRates(_device, out _utilization);
                _utilization = (err == Nvml.nvmlReturn.NVML_SUCCESS) ? _utilization : default(Nvml.NvmlUtilization);

                return (int)_utilization.gpu;
            } catch
            {
                lastExceptionsCounter++;
                return 0;
            }
            }

    }

    public int FanSpeed { 
        get {
            try
            {
                var err = Nvml.nvmlDeviceGetTemperature(_device, Nvml.nvmlTemperatureSensors.NVML_TEMPERATURE_GPU, out _temperature);
                _temperature = (err == Nvml.nvmlReturn.NVML_SUCCESS) ? _temperature : 0;

                return (int)_fanSpeed;
            } catch
            {
                lastExceptionsCounter++;
                return 0;
            }
        } 
    }

    //Power usage for this GPU in Watts and its associated circuitry
    public int PowerW { 
        get {
            try
            {
                var err = Nvml.nvmlDeviceGetPowerUsage(_device, out _power);
                _power = (err == Nvml.nvmlReturn.NVML_SUCCESS) ? _power : 0;

                return (int)(_power / 1000);
            }
            catch { 
                lastExceptionsCounter++;
                return 0;
            }
        } 
    }

    //The current SM clock speed for the device, in MHz"
    public int SMClockMhz { get {
            try
            {
                var err = Nvml.nvmlDeviceGetClockInfo(_device, Nvml.nvmlClockType.NVML_CLOCK_SM, out _smClock);
                _smClock = (err == Nvml.nvmlReturn.NVML_SUCCESS) ? _smClock : 0;

                return (int)_smClock;
            }
            catch
            {
                lastExceptionsCounter++;
                return 0;
            }
        } 
    }

    //The current temperature readings for the device, in degrees C
    public int TemperatureC { get {
            try
            {
                var err = Nvml.nvmlDeviceGetFanSpeed(_device, out _fanSpeed);
                _fanSpeed = (err == Nvml.nvmlReturn.NVML_SUCCESS) ? _fanSpeed : 0;

                return (int)_temperature;
            } catch
            {
                lastExceptionsCounter++;
                return 0;
            }
    } }

}

internal static class Nvml
{
    public enum nvmlReturn
    {
        NVML_SUCCESS = 0,                   //!< The operation was successful
        NVML_ERROR_UNINITIALIZED = 1,       //!< NVML was not first initialized with nvmlInit()
        NVML_ERROR_INVALID_ARGUMENT = 2,    //!< A supplied argument is invalid
        NVML_ERROR_NOT_SUPPORTED = 3,       //!< The requested operation is not available on target device
        NVML_ERROR_NO_PERMISSION = 4,       //!< The current user does not have permission for operation
        NVML_ERROR_ALREADY_INITIALIZED = 5, //!< Deprecated: Multiple initializations are now allowed through ref counting
        NVML_ERROR_NOT_FOUND = 6,           //!< A query to find an object was unsuccessful
        NVML_ERROR_INSUFFICIENT_SIZE = 7,   //!< An input argument is not large enough
        NVML_ERROR_INSUFFICIENT_POWER = 8,  //!< A device's external power cables are not properly attached
        NVML_ERROR_DRIVER_NOT_LOADED = 9,   //!< NVIDIA driver is not loaded
        NVML_ERROR_TIMEOUT = 10,            //!< User provided timeout passed
        NVML_ERROR_IRQ_ISSUE = 11,          //!< NVIDIA Kernel detected an interrupt issue with a GPU
        NVML_ERROR_LIBRARY_NOT_FOUND = 12,  //!< NVML Shared Library couldn't be found or loaded
        NVML_ERROR_FUNCTION_NOT_FOUND = 13, //!< Local version of NVML doesn't implement this function
        NVML_ERROR_CORRUPTED_INFOROM = 14,  //!< infoROM is corrupted
        NVML_ERROR_GPU_IS_LOST = 15,        //!< The GPU has fallen off the bus or has otherwise become inaccessible
        NVML_ERROR_UNKNOWN = 999            //!< An internal driver error occurred
    };

    public enum nvmlTemperatureSensors
    {
        NVML_TEMPERATURE_GPU = 0
    }

    internal enum nvmlClockType
    {
        NVML_CLOCK_GRAPHICS = 0,
        NVML_CLOCK_SM = 1,
        NVML_CLOCK_MEM = 2,
    }

    //private const string NvmlDll = @"C:\Program Files\NVIDIA Corporation\NVSMI\nvml.dll";
    private const string NvmlDll = @"C:\Windows\System32\nvml.dll";

    [DllImport(NvmlDll, EntryPoint = "nvmlInit_v2", ExactSpelling = true)]
    public static extern nvmlReturn nvmlInit_v2();

    [DllImport(NvmlDll, EntryPoint = "nvmlShutdown", ExactSpelling = true)]
    public static extern nvmlReturn nvmlShutdown();

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetCount", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetCount([Out] out int deviceCount);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetHandleByIndex", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetHandleByIndex([In] uint index,
        [Out] out IntPtr device);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetName", ExactSpelling = true, CharSet = CharSet.Ansi)]
    public static extern nvmlReturn nvmlDeviceGetName([In] IntPtr device, StringBuilder name, [In] uint length);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetUUID", ExactSpelling = true, CharSet = CharSet.Ansi)]
    public static extern nvmlReturn nvmlDeviceGetUUID([In] IntPtr device, StringBuilder name, [In] uint length);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetPciInfo_v2", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetPciInfo([In] IntPtr device, [Out] out nvmlPciInfo pci);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetUtilizationRates", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetUtilizationRates([In] IntPtr device, [Out] out NvmlUtilization utilization);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetMemoryInfo", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetMemoryInfo([In] IntPtr device, [Out] out NvmlMemory memory);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetTemperature", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetTemperature([In] IntPtr device, [In] nvmlTemperatureSensors sensorType, [Out] out uint temp);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetClockInfo", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetClockInfo([In] IntPtr device, [In] nvmlClockType type, [Out] out uint clock);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetPowerUsage", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetPowerUsage([In] IntPtr device, [Out] out uint power);

    [DllImport(NvmlDll, EntryPoint = "nvmlDeviceGetFanSpeed", ExactSpelling = true)]
    public static extern nvmlReturn nvmlDeviceGetFanSpeed([In] IntPtr device, [Out] out uint speed);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct nvmlPciInfo
    {
        private const int NVML_DEVICE_PCI_BUS_ID_BUFFER_SIZE = 16;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NVML_DEVICE_PCI_BUS_ID_BUFFER_SIZE)]
        public string busId;
        public uint domain;
        public uint bus;
        public uint device;
        public uint pciDeviceId;

        public uint pciSubSystemId;

        private uint reserved0;
        private uint reserved1;
        private uint reserved2;
        private uint reserved3;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct NvmlUtilization
    {
        public uint gpu;
        public uint memory;
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct NvmlMemory
    {
        public ulong total;
        public ulong free;
        public ulong used;
    };
}

