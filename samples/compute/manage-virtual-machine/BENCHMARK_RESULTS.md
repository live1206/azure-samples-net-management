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

## CPU core-time results

CPU core-time is sampled with `Process.TotalProcessorTime` around each invocation. Average vCores are CPU core-time divided by measured wall time. The mock-server process is excluded.

| Runtime | Track 1 CPU ms/op | Track 2 CPU ms/op | CPU saved by Track 2 | Track 1 avg vCores | Track 2 avg vCores |
| --- | ---: | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 22.4113 | 12.6241 | 43.67% | 0.9694 | 0.8957 |
| .NET 8.0.30 | 30.9929 | 13.9007 | 55.15% | 1.1060 | 0.9975 |
| .NET 10.0.11 | 29.2794 | 12.9444 | 55.79% | 1.0720 | 0.9394 |
