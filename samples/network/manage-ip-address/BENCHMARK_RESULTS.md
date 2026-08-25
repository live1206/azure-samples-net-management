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

The Track 1 sample, Track 2 sample, and benchmark executable all target `netcoreapp3.1`, `net8.0`, and `net10.0`. Each benchmark target references the matching sample target, so a .NET 8 benchmark uses the `net8.0` sample assemblies and a .NET 10 benchmark uses the `net10.0` sample assemblies.

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
| .NET Core 3.1.32 | 16.14 ms | 10.88 ms | 0.68 | 1047.08 KB | 535.75 KB | 0.51 |
| .NET 8.0.30 | 18.65 ms | 11.64 ms | 0.63 | 896.44 KB | 392.42 KB | 0.44 |
| .NET 10.0.11 | 18.48 ms | 11.31 ms | 0.61 | 899.81 KB | 392.58 KB | 0.44 |

## Percentage comparison

The percentage saved is calculated as:

```text
(baseline - comparison) / baseline * 100
```

A positive value means the comparison used less time or memory. A negative value means it used more.

### Within-runtime SDK comparison: Track 2 versus Track 1

This comparison holds the runtime constant and changes the SDK. Track 1 is the baseline for each row.

| Runtime | Track 1 mean | Track 2 mean | Elapsed time saved by Track 2 | Allocation saved by Track 2 |
| --- | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 16.14 ms | 10.88 ms | 32.59% | 48.83% |
| .NET 8.0.30 | 18.65 ms | 11.64 ms | 37.59% | 56.22% |
| .NET 10.0.11 | 18.48 ms | 11.31 ms | 38.80% | 56.37% |

For example, the .NET 10 row compares Track 2 on .NET 10 (11.31 ms) with Track 1 on .NET 10 (18.48 ms). It does not compare .NET 10 with .NET Core 3.1.

### Within-SDK runtime comparison: newer runtimes versus .NET Core 3.1

This comparison holds the SDK constant and changes the runtime. .NET Core 3.1 is the runtime baseline for each row. A negative saving means that the newer runtime took more elapsed time than the .NET Core 3.1 baseline.

| Implementation | Runtime | .NET Core 3.1 mean | Comparison mean | Elapsed time saved | Allocation saved |
| --- | --- | ---: | ---: | ---: | ---: |
| Track 1 Fluent | .NET 8.0.30 | 16.14 ms | 18.65 ms | -15.55% | 14.39% |
| Track 1 Fluent | .NET 10.0.11 | 16.14 ms | 18.48 ms | -14.50% | 14.06% |
| Track 2 ARM | .NET 8.0.30 | 10.88 ms | 11.64 ms | -6.99% | 26.75% |
| Track 2 ARM | .NET 10.0.11 | 10.88 ms | 11.31 ms | -3.95% | 26.72% |

For example, the Track 2/.NET 10 row compares Track 2 on .NET 10 (11.31 ms) with Track 2 on .NET Core 3.1 (10.88 ms). Therefore, Track 2 can be substantially faster than Track 1 when both use .NET 10, while also being slightly slower on .NET 10 than the same Track 2 code on .NET Core 3.1.

The BenchmarkDotNet `Mean` values are wall-clock elapsed time for the client operation, not direct process CPU-utilization measurements. They are useful for comparing client-side completion time, but should not be labeled as measured CPU consumption.

## Detailed results

### .NET Core 3.1.32

| Method | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Track 2 ARM | 10.88 ms | 0.722 ms | 0.430 ms | 0.68 | 0.04 | 535.75 KB | 0.51 |
| Track 1 Fluent | 16.14 ms | 1.029 ms | 0.681 ms | 1.00 | 0.06 | 1047.08 KB | 1.00 |

### .NET 8.0.30

| Method | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Track 2 ARM | 11.64 ms | 0.379 ms | 0.250 ms | 0.63 | 0.03 | 392.42 KB | 0.44 |
| Track 1 Fluent | 18.65 ms | 1.121 ms | 0.741 ms | 1.00 | 0.05 | 896.44 KB | 1.00 |

### .NET 10.0.11

| Method | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Track 2 ARM | 11.31 ms | 0.408 ms | 0.243 ms | 0.61 | 0.04 | 392.58 KB | 0.44 |
| Track 1 Fluent | 18.48 ms | 1.879 ms | 1.243 ms | 1.00 | 0.09 | 899.81 KB | 1.00 |

## Interpretation

Across all three runtimes, Track 2 completed the scenario faster and allocated less managed memory than Track 1. Allocation was substantially higher for both implementations on .NET Core 3.1 than on .NET 8 or .NET 10.

Elapsed-time differences between runtime rows should be treated cautiously because this benchmark includes loopback HTTP scheduling and was run once per runtime. The within-runtime Track 1 versus Track 2 comparison is the primary result.

These results measure SDK and client-side scenario execution against the mock server. They do not measure Azure service latency.
