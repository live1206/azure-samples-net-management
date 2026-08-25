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

## CPU core-time results

CPU core-time is sampled with `Process.TotalProcessorTime` around each invocation and includes all client-process threads. The mock-server process is excluded.

| Runtime | Track 1 CPU ms/op | Track 2 CPU ms/op | CPU saved by Track 2 |
| --- | ---: | ---: | ---: |
| .NET Core 3.1.32 | 9.7872 | 6.2411 | 36.23% |
| .NET 8.0.30 | 9.5745 | 5.7447 | 40.00% |
| .NET 10.0.11 | 10.0807 | 5.5360 | 45.08% |
