# Track 1 and Track 2 parallel-VM benchmark results

## Configuration

- Date: 2026-08-24 UTC
- BenchmarkDotNet 0.14.0: 1 launch, 3 warmups, 10 iterations, 10 invocations
- Matching `netcoreapp3.1`, `net8.0`, and `net10.0` targets
- Separate .NET 8 mock ARM server
- .NET SDK 10.0.400 on Ubuntu 26.04 WSL

Both implementations create a resource group, one VNet in each of two regions, and ten Linux VMs in parallel. Each VM has its own public IP and NIC. The resource group is deleted after every invocation.

## Results

| Runtime | Track 1 mean | Track 2 mean | Track 1 allocated | Track 2 allocated |
| --- | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 9.470 ms | 7.802 ms | 3.57 MB | 1.39 MB |
| .NET 8.0.30 | 11.425 ms | 8.136 ms | 3.16 MB | 1.01 MB |
| .NET 10.0.11 | 12.083 ms | 7.853 ms | 3.13 MB | 1.01 MB |

## Within-runtime SDK comparison

Track 1 is the baseline. Allocation percentages are based on BenchmarkDotNet's reported values rounded to two decimal places.

| Runtime | Time saved by Track 2 | Allocation saved by Track 2 |
| --- | ---: | ---: |
| .NET Core 3.1.32 | 17.61% | 61.06% |
| .NET 8.0.30 | 28.79% | 68.04% |
| .NET 10.0.11 | 35.01% | 67.73% |

## Within-SDK runtime comparison

.NET Core 3.1 is the runtime baseline.

| Implementation | Runtime | Time saved | Allocation saved |
| --- | --- | ---: | ---: |
| Track 1 Fluent | .NET 8.0.30 | -20.64% | 11.48% |
| Track 1 Fluent | .NET 10.0.11 | -27.59% | 12.32% |
| Track 2 ARM | .NET 8.0.30 | -4.28% | 27.34% |
| Track 2 ARM | .NET 10.0.11 | -0.65% | 27.34% |

These are wall-clock measurements against a loopback server, not direct CPU-utilization or Azure service-latency measurements.

## CPU core-time results

CPU core-time is sampled with `Process.TotalProcessorTime` around each invocation. Average vCores are CPU core-time divided by measured wall time. The mock-server process is excluded.

| Runtime | Track 1 CPU ms/op | Track 2 CPU ms/op | CPU saved by Track 2 | Track 1 avg vCores | Track 2 avg vCores |
| --- | ---: | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 44.2553 | 25.2482 | 42.95% | 3.4543 | 2.4266 |
| .NET 8.0.30 | 52.8369 | 26.0993 | 50.60% | 3.8364 | 2.7366 |
| .NET 10.0.11 | 54.4238 | 23.8742 | 56.13% | 3.7368 | 2.6321 |
