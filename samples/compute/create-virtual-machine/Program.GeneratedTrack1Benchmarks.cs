// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

extern alias Track1;
extern alias Track2;

using Azure.ResourceManager;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using ManagementBenchmarks;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Track1Program = Track1::CreateVMSample.GeneratedTrack1Program;
using Track2Program = Track2::CreateVMSample.Program;

namespace CreateVMSample.Benchmarks
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10, invocationCount: 10)]
    public class CreateVirtualMachineBenchmarks
    {
        private const string SubscriptionId = "00000000-0000-0000-0000-000000000000";

        private Track1Program.Clients _track1Client;
        private ArmClient _track2Client;
        private string _mockEndpoint;

        [GlobalSetup]
        public void Setup()
        {
            _mockEndpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT")
                ?? "http://127.0.0.1:5050";

            using (var httpClient = new HttpClient())
            {
                var healthEndpoint = new Uri(new Uri(EnsureTrailingSlash(_mockEndpoint)), "__mock/health");
                using (HttpResponseMessage response = httpClient.GetAsync(healthEndpoint).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"The mock ARM server at '{_mockEndpoint}' is not ready. Status: {response.StatusCode}.");
                    }
                }
            }

            CpuTimeRecorder.Reset();
            _track1Client = Track1Program.CreateMockClient(_mockEndpoint, SubscriptionId);
            _track2Client = Track2Program.CreateMockClient(_mockEndpoint, SubscriptionId);
        }

        [Benchmark(Baseline = true, Description = "Track 1 generated")]
        public void Track1()
        {
            TextWriter originalOutput = Console.Out;
            Console.SetOut(TextWriter.Null);
            try
            {
                CpuTimeRecorder.Measure(() => Track1Program.RunSample(_track1Client));
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        [Benchmark(Description = "Track 2 ARM")]
        public async Task Track2()
        {
            TextWriter originalOutput = Console.Out;
            Console.SetOut(TextWriter.Null);
            try
            {
                await CpuTimeRecorder.MeasureAsync(() => Track2Program.RunSample(_track2Client, SubscriptionId));
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }

        [GlobalCleanup]
        public void ReportCpuUsage()
        {
            CpuTimeRecorder.Report();
        }

        private static string EnsureTrailingSlash(string endpoint)
        {
            return endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : endpoint + "/";
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
            BenchmarkRunner.Run<CreateVirtualMachineBenchmarks>(config);
        }
    }
}
