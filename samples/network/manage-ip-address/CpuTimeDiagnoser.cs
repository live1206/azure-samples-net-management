using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ManagementBenchmarks
{
    public static class CpuTimeRecorder
    {
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();
        private static long _cpuTicks;
        private static long _operations;

        public static void Reset()
        {
            _cpuTicks = 0;
            _operations = 0;
        }

        public static void Measure(Action action)
        {
            TimeSpan cpuStart = CurrentProcess.TotalProcessorTime;
            action();
            Record(cpuStart);
        }

        public static async Task MeasureAsync(Func<Task> action)
        {
            TimeSpan cpuStart = CurrentProcess.TotalProcessorTime;
            await action().ConfigureAwait(false);
            Record(cpuStart);
        }

        private static void Record(TimeSpan cpuStart)
        {
            CurrentProcess.Refresh();
            _cpuTicks += (CurrentProcess.TotalProcessorTime - cpuStart).Ticks;
            _operations++;
        }

        public static void Report()
        {
            double cpuMilliseconds = TimeSpan.FromTicks(_cpuTicks).TotalMilliseconds / _operations;
            Console.WriteLine($"CPU-CORE-METRIC: cpu-ms/op={cpuMilliseconds:F4}; operations={_operations}");
        }
    }
}
