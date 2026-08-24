# Track 1 and Track 2 benchmark results

## Test runs

- Date: 2026-08-24 (UTC)
- Command: `BENCHMARK_FRAMEWORK=<target> ./run-benchmarks.sh`
- Targets: `netcoreapp3.1`, `net8.0`, and `net10.0`
- Scenario: Complete normalized IP-address management scenario against the shared local mock ARM server
- Benchmark framework: BenchmarkDotNet 0.14.0
- Launches: 1
- Warmup iterations: 3
- Measurement iterations: 10
- Invocations per iteration: 10
- Unroll factor: 1

Client construction and mock-server startup were performed outside the measured operation. The .NET 8 mock server ran in a separate process, so its managed allocations were not attributed to either client benchmark. The same server runtime and benchmark configuration were used for all three client-runtime comparisons.

## .NET versions

| Benchmark target | Runtime used | JIT and instruction set |
| --- | --- | --- |
| `netcoreapp3.1` | .NET Core 3.1.32 (CoreCLR 4.700.22.55902, CoreFX 4.700.22.56512) | x64 RyuJIT, AVX2 |
| `net8.0` | .NET 8.0.30 (8.0.3026.36720) | x64 RyuJIT, AVX-512 |
| `net10.0` | .NET 10.0.11 (10.0.1126.37416) | x64 RyuJIT, AVX-512 |
| Mock ARM server | .NET 8.0.30 | Separate process |

Both the Track 1 and Track 2 sample assemblies target `netcoreapp3.1`. The multi-targeted benchmark executable loads those same assemblies into each selected runtime, ensuring that both SDK implementations use the same runtime during any individual comparison.

The projects were built with .NET SDK 10.0.400.

## Environment

- OS: Ubuntu 26.04 LTS under WSL
- CPU: Intel Xeon Platinum 8370C @ 2.80 GHz
- CPU topology reported by BenchmarkDotNet: 1 CPU, 8 physical cores, 16 logical cores
- Architecture: x64
- GC: Concurrent Workstation

.NET Core 3.1.32 was installed under `~/.dotnet` with `dotnet-install.sh`. Because .NET Core 3.1 is out of support and is incompatible with the OpenSSL and ICU versions shipped by Ubuntu 26.04, the 3.1 run used locally extracted OpenSSL 1.1 and ICU 66 compatibility libraries through `LD_LIBRARY_PATH`. Globalization invariant mode was not used for the recorded result.

## Package versions

| Implementation | Packages |
| --- | --- |
| Track 1 | `Microsoft.Azure.Management.Fluent` 1.36.1 |
| Track 2 | `Azure.Identity` 1.11.4, `Azure.ResourceManager.Compute` 1.2.1, `Azure.ResourceManager.Network` 1.6.0 |

## Cross-runtime summary

Track 1 is the baseline within each runtime.

| Runtime | Track 1 mean | Track 2 mean | Track 2 time ratio | Track 1 allocated | Track 2 allocated | Track 2 allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 16.80 ms | 10.69 ms | 0.64 | 1047.08 KB | 549.21 KB | 0.52 |
| .NET 8.0.30 | 18.63 ms | 11.94 ms | 0.64 | 896.47 KB | 392.44 KB | 0.44 |
| .NET 10.0.11 | 19.85 ms | 11.59 ms | 0.59 | 899.83 KB | 392.65 KB | 0.44 |

## Percentage comparison

The percentage saved is calculated as:

```text
(baseline - comparison) / baseline * 100
```

A positive value means the comparison used less time or memory. A negative value means it used more.

### Track 2 savings compared with Track 1

Track 1 is the baseline in this table.

| Runtime | Elapsed time saved by Track 2 | Allocation saved by Track 2 |
| --- | ---: | ---: |
| .NET Core 3.1.32 | 36.37% | 47.55% |
| .NET 8.0.30 | 35.91% | 56.22% |
| .NET 10.0.11 | 41.61% | 56.36% |

### Runtime savings compared with .NET Core 3.1

.NET Core 3.1 is the only runtime baseline in this table. A negative saving means that the newer runtime took more elapsed time than the .NET Core 3.1 baseline.

| Implementation | Runtime | Elapsed time saved | Allocation saved |
| --- | --- | ---: | ---: |
| Track 1 Fluent | .NET 8.0.30 | -10.89% | 14.38% |
| Track 1 Fluent | .NET 10.0.11 | -18.15% | 14.06% |
| Track 2 ARM | .NET 8.0.30 | -11.69% | 28.54% |
| Track 2 ARM | .NET 10.0.11 | -8.42% | 28.51% |

For example, Track 1 on .NET 10 took 19.85 ms compared with 16.80 ms on .NET Core 3.1. The `-18.15%` saving therefore means it was 18.15% slower in this local run, while its positive `14.06%` allocation saving means it allocated less memory.

The BenchmarkDotNet `Mean` values are wall-clock elapsed time for the client operation, not direct process CPU-utilization measurements. They are useful for comparing client-side completion time, but should not be labeled as measured CPU consumption.

## Detailed results

### .NET Core 3.1.32

| Method | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Track 2 ARM | 10.69 ms | 0.384 ms | 0.201 ms | 0.64 | 0.07 | 549.21 KB | 0.52 |
| Track 1 Fluent | 16.80 ms | 2.917 ms | 1.929 ms | 1.01 | 0.15 | 1047.08 KB | 1.00 |

### .NET 8.0.30

| Method | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Track 2 ARM | 11.94 ms | 0.360 ms | 0.238 ms | 0.64 | 0.03 | 392.44 KB | 0.44 |
| Track 1 Fluent | 18.63 ms | 1.322 ms | 0.875 ms | 1.00 | 0.06 | 896.47 KB | 1.00 |

### .NET 10.0.11

| Method | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Track 2 ARM | 11.59 ms | 0.237 ms | 0.157 ms | 0.59 | 0.04 | 392.65 KB | 0.44 |
| Track 1 Fluent | 19.85 ms | 1.961 ms | 1.297 ms | 1.00 | 0.09 | 899.83 KB | 1.00 |

## Interpretation

Across all three runtimes, Track 2 completed the scenario faster and allocated less managed memory than Track 1. Allocation was substantially higher for both implementations on .NET Core 3.1 than on .NET 8 or .NET 10.

Elapsed-time differences between runtime rows should be treated cautiously because this benchmark includes loopback HTTP scheduling and was run once per runtime. The within-runtime Track 1 versus Track 2 comparison is the primary result.

These results measure SDK and client-side scenario execution against the mock server. They do not measure Azure service latency.
