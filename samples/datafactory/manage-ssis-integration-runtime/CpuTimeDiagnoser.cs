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

        public static Task MeasureAsync(Func<Task> action) => MeasureAsync(action, 1);

        public static async Task MeasureAsync(Func<Task> action, int operations)
        {
            TimeSpan cpuStart = CurrentProcess.TotalProcessorTime;
            await action().ConfigureAwait(false);
            Record(cpuStart, operations);
        }

        private static void Record(TimeSpan cpuStart, int operations = 1)
        {
            CurrentProcess.Refresh();
            _cpuTicks += (CurrentProcess.TotalProcessorTime - cpuStart).Ticks;
            _operations += operations;
        }

        public static void Report()
        {
            double cpuMilliseconds = TimeSpan.FromTicks(_cpuTicks).TotalMilliseconds / _operations;
            Console.WriteLine($"CPU-CORE-METRIC: cpu-ms/op={cpuMilliseconds:F4}; operations={_operations}");
        }
    }
}
