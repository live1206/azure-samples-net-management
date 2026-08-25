using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ManagementBenchmarks
{
    public static class CpuTimeRecorder
    {
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();
        private static long _cpuTicks;
        private static long _wallTicks;
        private static long _operations;

        public static void Reset()
        {
            _cpuTicks = 0;
            _wallTicks = 0;
            _operations = 0;
        }

        public static void Measure(Action action)
        {
            TimeSpan cpuStart = CurrentProcess.TotalProcessorTime;
            long wallStart = Stopwatch.GetTimestamp();
            action();
            Record(cpuStart, wallStart);
        }

        public static async Task MeasureAsync(Func<Task> action)
        {
            TimeSpan cpuStart = CurrentProcess.TotalProcessorTime;
            long wallStart = Stopwatch.GetTimestamp();
            await action().ConfigureAwait(false);
            Record(cpuStart, wallStart);
        }

        private static void Record(TimeSpan cpuStart, long wallStart)
        {
            CurrentProcess.Refresh();
            _cpuTicks += (CurrentProcess.TotalProcessorTime - cpuStart).Ticks;
            _wallTicks += Stopwatch.GetTimestamp() - wallStart;
            _operations++;
        }

        public static void Report()
        {
            double cpuMilliseconds = TimeSpan.FromTicks(_cpuTicks).TotalMilliseconds / _operations;
            double wallMilliseconds = _wallTicks * 1000.0 / Stopwatch.Frequency / _operations;
            double averageVCores = cpuMilliseconds / wallMilliseconds;
            Console.WriteLine($"CPU-CORE-METRIC: cpu-ms/op={cpuMilliseconds:F4}; measured-wall-ms/op={wallMilliseconds:F4}; average-vcores={averageVCores:F4}; operations={_operations}");
        }
    }
}
