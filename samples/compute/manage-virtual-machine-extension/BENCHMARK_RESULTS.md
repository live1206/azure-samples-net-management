# Track 1 and Track 2 VM-extension benchmark results

## Configuration

- Date: 2026-08-25 UTC
- BenchmarkDotNet 0.14.0: 1 launch, 3 warmups, 10 iterations, 10 invocations
- Matching `netcoreapp3.1`, `net8.0`, and `net10.0` targets
- Separate .NET 8 mock ARM server
- .NET SDK 10.0.400 on Ubuntu 26.04 WSL

Both implementations create a resource group, VNet, Linux and Windows VMs, create and replace Linux VMAccess/custom-script extensions, create and replace a Windows VMAccess extension, and clean up the resource group.

## Results

| Runtime | Track 1 mean | Track 2 mean | Track 1 allocated | Track 2 allocated |
| --- | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 32.24 ms | 11.25 ms | 2570.71 KB | 548.50 KB |
| .NET 8.0.30 | 39.02 ms | 12.13 ms | 2265.40 KB | 395.05 KB |
| .NET 10.0.11 | 39.17 ms | 12.93 ms | 2268.14 KB | 395.28 KB |

## Within-runtime SDK comparison

Track 1 is the baseline.

| Runtime | Time saved by Track 2 | Allocation saved by Track 2 |
| --- | ---: | ---: |
| .NET Core 3.1.32 | 65.11% | 78.66% |
| .NET 8.0.30 | 68.91% | 82.56% |
| .NET 10.0.11 | 66.99% | 82.57% |

## Within-SDK runtime comparison

.NET Core 3.1 is the runtime baseline.

| Implementation | Runtime | Time saved | Allocation saved |
| --- | --- | ---: | ---: |
| Track 1 Fluent | .NET 8.0.30 | -21.03% | 11.88% |
| Track 1 Fluent | .NET 10.0.11 | -21.50% | 11.77% |
| Track 2 ARM | .NET 8.0.30 | -7.82% | 27.98% |
| Track 2 ARM | .NET 10.0.11 | -14.93% | 27.93% |

These are wall-clock measurements against a loopback server, not direct CPU-utilization or Azure service-latency measurements. Track 1 extension replacement is expressed through Fluent VM update operations, while Track 2 addresses extension resources directly; that abstraction difference is part of the measured SDK behavior.

## Latest Track 2 package rerun

The original results above are retained. A second benchmark project compares Track 1 with the latest stable Track 2 packages available on 2026-08-25:

- `Azure.Identity` 1.21.0
- `Azure.ResourceManager.Compute` 1.16.0
- `Azure.ResourceManager.Network` 1.17.0
- `Azure.ResourceManager.Resources` 1.11.2 where directly referenced

The latest-package benchmark runs in a separate process from the legacy Track 2 benchmark to prevent .NET assembly-version unification from mixing the two package graphs. Current Track 2 dependencies do not support .NET Core 3.1, so latest-package results are limited to .NET 8 and .NET 10.

| Runtime | Track 1 mean | Track 2 latest mean | Track 1 allocated | Track 2 latest allocated | Time saved | Allocation saved |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| .NET 8.0.30 | 38.23 ms | 13.10 ms | 2265.39 KB | 545.45 KB | 65.73% | 75.92% |
| .NET 10.0.11 | 39.63 ms | 11.80 ms | 2269.29 KB | 545.57 KB | 70.22% | 75.96% |
