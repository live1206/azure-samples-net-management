# Track 1 and Track 2 manage-VM benchmark results

## Configuration

- Date: 2026-08-24 UTC
- BenchmarkDotNet 0.14.0: 1 launch, 3 warmups, 10 iterations, 10 invocations
- Matching `netcoreapp3.1`, `net8.0`, and `net10.0` sample and benchmark targets
- Separate .NET 8 mock ARM server process
- .NET SDK 10.0.400, Ubuntu 26.04 WSL, Intel Xeon Platinum 8370C

Both implementations create a resource group, VNet, two NICs, a Windows VM with data disks, apply tags, attach and detach a disk, restart and power off the VM, create a Linux VM, list VMs, delete the Windows VM, and delete the resource group.

## Results

| Runtime | Track 1 mean | Track 2 mean | Track 1 allocated | Track 2 allocated |
| --- | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 21.65 ms | 12.86 ms | 1345.25 KB | 539.37 KB |
| .NET 8.0.30 | 25.45 ms | 14.09 ms | 1200.34 KB | 400.88 KB |
| .NET 10.0.11 | 26.61 ms | 13.68 ms | 1201.97 KB | 401.06 KB |

## Within-runtime SDK comparison

Track 1 is the baseline.

| Runtime | Time saved by Track 2 | Allocation saved by Track 2 |
| --- | ---: | ---: |
| .NET Core 3.1.32 | 40.60% | 59.91% |
| .NET 8.0.30 | 44.64% | 66.60% |
| .NET 10.0.11 | 48.59% | 66.63% |

## Within-SDK runtime comparison

.NET Core 3.1 is the runtime baseline. Negative time savings mean the newer runtime took longer.

| Implementation | Runtime | Time saved | Allocation saved |
| --- | --- | ---: | ---: |
| Track 1 Fluent | .NET 8.0.30 | -17.55% | 10.77% |
| Track 1 Fluent | .NET 10.0.11 | -22.91% | 10.65% |
| Track 2 ARM | .NET 8.0.30 | -9.56% | 25.68% |
| Track 2 ARM | .NET 10.0.11 | -6.38% | 25.64% |

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
| .NET 8.0.30 | 23.79 ms | 12.66 ms | 1199.95 KB | 470.66 KB | 46.78% | 60.78% |
| .NET 10.0.11 | 24.11 ms | 11.41 ms | 1202.71 KB | 470.93 KB | 52.67% | 60.85% |
