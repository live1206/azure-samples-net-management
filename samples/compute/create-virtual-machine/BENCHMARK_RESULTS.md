# Track 1 and Track 2 create-VM benchmark results

## Configuration

- Date: 2026-08-24 UTC
- BenchmarkDotNet 0.14.0: 1 launch, 3 warmups, 10 iterations, 10 invocations
- Matching sample and benchmark targets: `netcoreapp3.1`, `net8.0`, `net10.0`
- Separate .NET 8 mock ARM server process
- .NET SDK 10.0.400 on Ubuntu 26.04 WSL
- Intel Xeon Platinum 8370C, 8 physical/16 logical cores

Both implementations create a resource group, VNet and subnet, NIC, and equivalent `Standard_F2` Windows Server 2016 VM, then delete the resource group.

## Results

| Runtime | Track 1 mean | Track 2 mean | Track 1 allocated | Track 2 allocated |
| --- | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 7.121 ms | 4.760 ms | 399.65 KB | 226.89 KB |
| .NET 8.0.30 | 7.483 ms | 4.917 ms | 341.51 KB | 171.18 KB |
| .NET 10.0.11 | 8.398 ms | 4.945 ms | 342.91 KB | 170.52 KB |

## Within-runtime SDK comparison

Track 1 is the baseline.

| Runtime | Time saved by Track 2 | Allocation saved by Track 2 |
| --- | ---: | ---: |
| .NET Core 3.1.32 | 33.16% | 43.23% |
| .NET 8.0.30 | 34.29% | 49.88% |
| .NET 10.0.11 | 41.12% | 50.27% |

## Within-SDK runtime comparison

.NET Core 3.1 is the runtime baseline. Negative time savings mean the newer runtime took longer in this run.

| Implementation | Runtime | Time saved | Allocation saved |
| --- | --- | ---: | ---: |
| Track 1 Fluent | .NET 8.0.30 | -5.08% | 14.55% |
| Track 1 Fluent | .NET 10.0.11 | -17.93% | 14.20% |
| Track 2 ARM | .NET 8.0.30 | -3.30% | 24.55% |
| Track 2 ARM | .NET 10.0.11 | -3.89% | 24.84% |

These are wall-clock client-scenario measurements against a loopback server, not direct CPU-utilization or Azure service-latency measurements.

## Latest Track 2 package rerun

The original results above are retained. A second benchmark project compares Track 1 with the latest stable Track 2 packages available on 2026-08-25:

- `Azure.Identity` 1.21.0
- `Azure.ResourceManager.Compute` 1.16.0
- `Azure.ResourceManager.Network` 1.17.0
- `Azure.ResourceManager.Resources` 1.11.2 where directly referenced

The latest-package benchmark runs in a separate process from the legacy Track 2 benchmark to prevent .NET assembly-version unification from mixing the two package graphs. Current Track 2 dependencies do not support .NET Core 3.1, so latest-package results are limited to .NET 8 and .NET 10.

| Runtime | Track 1 mean | Track 2 latest mean | Track 1 allocated | Track 2 latest allocated | Time saved | Allocation saved |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| .NET 8.0.30 | 7.649 ms | 5.310 ms | 341.51 KB | 219.06 KB | 30.58% | 35.86% |
| .NET 10.0.11 | 8.227 ms | 4.621 ms | 342.91 KB | 218.58 KB | 43.83% | 36.26% |
