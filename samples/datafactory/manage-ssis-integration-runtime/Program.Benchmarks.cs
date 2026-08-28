extern alias Track1;
extern alias Track2;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using ManagementBenchmarks;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using T1 = Track1::ManageSsisIntegrationRuntime.Track1Scenario;
using T2 = Track2::ManageSsisIntegrationRuntime.Track2Scenario;

namespace ManageSsisIntegrationRuntime.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(launchCount: 3, warmupCount: 3, iterationCount: 10, invocationCount: 1)]
    public class IntegrationRuntimeBenchmarks
    {
        private const int BatchSize = 100;
        private const string SubscriptionId = "00000000-0000-0000-0000-000000000000";
        private T1.Clients _track1;
        private Azure.ResourceManager.ArmClient _track2;

        [GlobalSetup]
        public void Setup()
        {
            CpuTimeRecorder.Reset();
            string endpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT") ?? "http://127.0.0.1:5050";
            using (var http = new HttpClient())
            {
                http.GetAsync(endpoint + "/__mock/health").GetAwaiter().GetResult().EnsureSuccessStatusCode();
            }
            _track1 = T1.CreateMockClient(endpoint, SubscriptionId);
            _track2 = T2.CreateMockClient(endpoint, SubscriptionId);

            // Force client and serializer initialization outside measured batches.
            T1.RunAsync(_track1, SubscriptionId).GetAwaiter().GetResult();
            T2.RunAsync(_track2, SubscriptionId).GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true, Description = "Track 1 generated", OperationsPerInvoke = BatchSize)]
        public Task Track1() => CpuTimeRecorder.MeasureAsync(async () =>
        {
            for (int i = 0; i < BatchSize; i++)
                await T1.RunAsync(_track1, SubscriptionId).ConfigureAwait(false);
        }, BatchSize);

        [Benchmark(Description = "Track 2 ARM", OperationsPerInvoke = BatchSize)]
        public Task Track2() => CpuTimeRecorder.MeasureAsync(async () =>
        {
            for (int i = 0; i < BatchSize; i++)
                await T2.RunAsync(_track2, SubscriptionId).ConfigureAwait(false);
        }, BatchSize);

        [GlobalCleanup]
        public void Report() => CpuTimeRecorder.Report();
    }

    public static class Program
    {
        public static void Main(string[] args) =>
            BenchmarkRunner.Run<IntegrationRuntimeBenchmarks>(
                DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator));
    }
}
