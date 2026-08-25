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

## CPU core-time results

CPU core-time is sampled with `Process.TotalProcessorTime` around each invocation and includes all client-process threads. The mock-server process is excluded.

| Runtime | Track 1 CPU ms/op | Track 2 CPU ms/op | CPU saved by Track 2 |
| --- | ---: | ---: | ---: |
| .NET Core 3.1.32 | 35.1773 | 12.9078 | 63.31% |
| .NET 8.0.30 | 51.4184 | 12.9787 | 74.76% |
| .NET 10.0.11 | 52.9583 | 13.0006 | 75.45% |
