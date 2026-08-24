# Track 1 and Track 2 benchmark results

## Test run

- Date: 2026-08-24 (UTC)
- Command: `./run-benchmarks.sh`
- Scenario: Complete normalized IP-address management scenario against the shared local mock ARM server
- Benchmark framework: BenchmarkDotNet 0.14.0
- Launches: 1
- Warmup iterations: 3
- Measurement iterations: 10
- Invocations per iteration: 10
- Unroll factor: 1

Client construction and mock-server startup were performed outside the measured operation. The mock server ran in a separate process, so its managed allocations were not attributed to either client benchmark.

## .NET versions

| Component | Target framework | Runtime used during the benchmark |
| --- | --- | --- |
| Benchmark process | `net8.0` | .NET 8.0.30 |
| Track 1 sample assembly | `netcoreapp3.1` | Loaded and executed in the .NET 8.0.30 benchmark process |
| Track 2 sample assembly | `netcoreapp3.1` | Loaded and executed in the .NET 8.0.30 benchmark process |
| Mock ARM server | `net8.0` | .NET 8 with major-version roll-forward enabled on this machine |

The projects were built with .NET SDK 10.0.400. Both client implementations ran under the same .NET 8 runtime, making the runtime and JIT consistent across the comparison.

## Environment

- OS: Ubuntu 26.04 LTS under WSL
- CPU: Intel Xeon Platinum 8370C @ 2.80 GHz
- CPU topology reported by BenchmarkDotNet: 1 CPU, 8 physical cores, 16 logical cores
- Architecture: x64
- JIT: RyuJIT
- GC: Concurrent Workstation

## Package versions

| Implementation | Packages |
| --- | --- |
| Track 1 | `Microsoft.Azure.Management.Fluent` 1.36.1 |
| Track 2 | `Azure.Identity` 1.11.4, `Azure.ResourceManager.Compute` 1.2.1, `Azure.ResourceManager.Network` 1.6.0 |

## Results

| Method | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Track 2 ARM | 11.52 ms | 0.590 ms | 0.390 ms | 0.63 | 0.03 | 405.87 KB | 0.45 |
| Track 1 Fluent | 18.41 ms | 0.699 ms | 0.462 ms | 1.00 | 0.03 | 896.47 KB | 1.00 |

Track 1 is the BenchmarkDotNet baseline. In this run, Track 2 took approximately 63% of the Track 1 elapsed time and allocated approximately 45% as much managed memory.

These results describe one local run against the mock server and should not be interpreted as Azure service latency measurements.
