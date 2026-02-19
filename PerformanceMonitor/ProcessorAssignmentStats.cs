using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PerformanceMonitor
{
    internal sealed class ProcessorAssignmentStats : IDisposable
    {
        //[DllImport("kernel32.dll")]
        //private static extern uint GetCurrentProcessorNumber();

        private readonly object _lock = new object();
        private readonly long[] _counts;
        private long _totalTicks;
        private readonly string _filePath;

        // Optional: treat CPUs 0-15 as P, 16-27 as E (your 14700K assumption)
        private readonly int _pCoreMaxLogical;

        public ProcessorAssignmentStats(int logicalProcessorCount, int pCoreMaxLogicalInclusive = 15, string filePrefix = "PerformanceMonitor_CPU_Assignments")
        {
            if (logicalProcessorCount <= 0) throw new ArgumentOutOfRangeException(nameof(logicalProcessorCount));

            _counts = new long[logicalProcessorCount];
            _pCoreMaxLogical = pCoreMaxLogicalInclusive;

            string tempDir = Path.GetTempPath(); // respects TEMP/TMP
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _filePath = Path.Combine(tempDir, $"{filePrefix}_{stamp}_pid{Process.GetCurrentProcess().Id}.txt");

            // Create header immediately
            File.WriteAllText(_filePath,
                $"CPU Assignment Stats\nStarted: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                $"Logical CPUs: {logicalProcessorCount}\n" +
                $"Output: {_filePath}\n\n");
        }

        /// <summary>Call once per "tick" (e.g., timer1_Tick) from the thread you want to measure.</summary>
        public uint Tick(uint cpu)
        {
            //uint cpu = GetCurrentProcessorNumber();

            lock (_lock)
            {
                _totalTicks++;
                if (cpu < _counts.Length)
                    _counts[cpu]++;
                // else: ignore (shouldn't happen unless processor count changed dynamically)
            }

            return cpu;
        }

        /// <summary>
        /// Starts a new measurement window by clearing the accumulated tick total and per-CPU counts.
        /// Use this when changing scheduling settings (affinity, CPU sets, EcoQoS) so statistics reflect
        /// only the new configuration. Optionally records snapshots around the reset for traceability.
        /// </summary>
        /// <param name="title">Optional label to annotate the reset/snapshot entries.</param>
        /// <param name="writeSnapshotBeforeReset">When true, writes a "pre-reset" snapshot before counters are cleared.</param>
        public void Reset(string title = null, bool writeSnapshotBeforeReset = true)
        {
            if (writeSnapshotBeforeReset)
            {
                // Capture what happened up to this point (helps when switching modes)
                WriteReportSnapshot(title: string.IsNullOrWhiteSpace(title) ? "Reset (pre)" : $"Reset (pre): {title}");
            }

            lock (_lock)
            {
                _totalTicks = 0;
                Array.Clear(_counts, 0, _counts.Length);
            }

            // Optional: mark the reset point in the file
            WriteReportSnapshot(title: string.IsNullOrWhiteSpace(title) ? "Reset (post)" : $"Reset (post): {title}");
        }


        /// <summary>Writes a fresh report snapshot (append by default).</summary>
        public void WriteReportSnapshot(bool append = true, string title = null)
        {
            string report = BuildReport(title);

            lock (_lock)
            {
                if (append) File.AppendAllText(_filePath, report);
                else File.WriteAllText(_filePath, report);
            }
        }

        private string BuildReport(string title)
        {
            long total;
            long[] copy;

            lock (_lock)
            {
                total = _totalTicks;
                copy = (long[])_counts.Clone();
            }

            var sb = new StringBuilder();
            sb.AppendLine("============================================================");
            sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {(string.IsNullOrWhiteSpace(title) ? "Snapshot" : title)}");
            sb.AppendLine($"Total ticks: {total:N0}");

            if (total <= 0)
            {
                sb.AppendLine("No ticks recorded yet.");
                return sb.ToString();
            }

            // Group summary (optional)
            long pTicks = 0, eTicks = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                if (i <= _pCoreMaxLogical) pTicks += copy[i];
                else eTicks += copy[i];
            }
            sb.AppendLine($"P-core logical range: 0..{_pCoreMaxLogical}  | ticks: {pTicks:N0} ({(100.0 * pTicks / total):F2}%)");
            sb.AppendLine($"E-core logical range: {_pCoreMaxLogical + 1}..{copy.Length - 1}  | ticks: {eTicks:N0} ({(100.0 * eTicks / total):F2}%)");
            sb.AppendLine();

            // Per-CPU table
            sb.AppendLine("CPU\tTicks\t\tPct\t\tBar");
            sb.AppendLine("---\t-----\t\t---\t\t---");

            // Scale a simple bar to ~40 chars
            long max = copy.Max();
            const int barWidth = 40;

            for (int cpu = 0; cpu < copy.Length; cpu++)
            {
                double pct = 100.0 * copy[cpu] / total;
                int filled = max > 0 ? (int)Math.Round((double)copy[cpu] / max * barWidth) : 0;
                string bar = new string('#', filled).PadRight(barWidth, '.');

                sb.AppendLine($"{cpu:00}\t{copy[cpu],10:N0}\t{pct,7:F2}%\t{bar}");
            }

            sb.AppendLine();
            return sb.ToString();
        }

        public string OutputPath => _filePath;

        public void Dispose()
        {
            // Final snapshot on dispose
            try { WriteReportSnapshot(append: true, title: "Final"); }
            catch { /* ignore */ }
        }
    }
}
