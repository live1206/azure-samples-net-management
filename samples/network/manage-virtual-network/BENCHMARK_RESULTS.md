# Track 1 and Track 2 virtual-network benchmark results

## Test configuration

- Date: 2026-08-24 (UTC)
- Scenario: Complete normalized virtual-network management scenario against the local mock ARM server
- BenchmarkDotNet: 0.14.0
- Job: 1 launch, 3 warmups, 10 iterations, 10 invocations per iteration
- Build SDK: .NET SDK 10.0.400
- OS: Ubuntu 26.04 LTS under WSL
- CPU: Intel Xeon Platinum 8370C, 8 physical and 16 logical cores
- Mock server: .NET 8.0.30, separate process

The Track 1 sample, Track 2 sample, and benchmark use matching `netcoreapp3.1`, `net8.0`, or `net10.0` targets for each run. Client construction and mock-server startup are outside the measured operation.

## Normalized scenario

Both implementations:

1. Create a resource group.
2. Create backend and frontend network security groups with equivalent rules.
3. Create a VNet with frontend and backend subnets.
4. Update the frontend subnet to associate its NSG.
5. Create a public IP and two NICs.
6. Create equivalent frontend and backend Linux VMs using the existing NICs.
7. Create a second VNet.
8. List the resource group's VNets.
9. Delete the second VNet.
10. Delete the resource group.

## Results

| Runtime | Track 1 mean | Track 2 mean | Track 2 time ratio | Track 1 allocated | Track 2 allocated | Track 2 allocation ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 19.17 ms | 12.49 ms | 0.65 | 1538.82 KB | 599.24 KB | 0.39 |
| .NET 8.0.30 | 23.43 ms | 13.72 ms | 0.59 | 1338.10 KB | 452.91 KB | 0.34 |
| .NET 10.0.11 | 23.91 ms | 13.12 ms | 0.55 | 1341.83 KB | 453.21 KB | 0.34 |

## Percentage comparisons

The percentage saved is `(baseline - comparison) / baseline * 100`. Positive values mean less time or allocation; negative values mean more.

### Within-runtime SDK comparison

Track 1 is the baseline for each row.

| Runtime | Track 1 mean | Track 2 mean | Time saved by Track 2 | Allocation saved by Track 2 |
| --- | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 19.17 ms | 12.49 ms | 34.85% | 61.06% |
| .NET 8.0.30 | 23.43 ms | 13.72 ms | 41.44% | 66.15% |
| .NET 10.0.11 | 23.91 ms | 13.12 ms | 45.13% | 66.22% |

### Within-SDK runtime comparison

.NET Core 3.1 is the runtime baseline for each row.

| Implementation | Runtime | .NET Core 3.1 mean | Comparison mean | Time saved | Allocation saved |
| --- | --- | ---: | ---: | ---: | ---: |
| Track 1 Fluent | .NET 8.0.30 | 19.17 ms | 23.43 ms | -22.22% | 13.04% |
| Track 1 Fluent | .NET 10.0.11 | 19.17 ms | 23.91 ms | -24.73% | 12.80% |
| Track 2 ARM | .NET 8.0.30 | 12.49 ms | 13.72 ms | -9.85% | 24.42% |
| Track 2 ARM | .NET 10.0.11 | 12.49 ms | 13.12 ms | -5.04% | 24.37% |

These values are wall-clock elapsed time, not direct CPU-utilization measurements. They measure SDK-side execution plus loopback response latency, not Azure service latency.

## CPU core-time results

CPU core-time is sampled with `Process.TotalProcessorTime` around each invocation. Average vCores are CPU core-time divided by measured wall time. The mock-server process is excluded.

| Runtime | Track 1 CPU ms/op | Track 2 CPU ms/op | CPU saved by Track 2 | Track 1 avg vCores | Track 2 avg vCores |
| --- | ---: | ---: | ---: | ---: | ---: |
| .NET Core 3.1.32 | 22.1986 | 13.5461 | 38.98% | 0.9490 | 0.8932 |
| .NET 8.0.30 | 27.8723 | 13.4043 | 51.91% | 1.0685 | 0.9494 |
| .NET 10.0.11 | 29.4816 | 13.6362 | 53.75% | 1.1139 | 0.9533 |
