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

## Latest Track 2 package rerun

The original results above are retained. A second benchmark project compares Track 1 with the latest stable Track 2 packages available on 2026-08-25:

- `Azure.Identity` 1.21.0
- `Azure.ResourceManager.Compute` 1.16.0
- `Azure.ResourceManager.Network` 1.17.0
- `Azure.ResourceManager.Resources` 1.11.2 where directly referenced

The latest-package benchmark runs in a separate process from the legacy Track 2 benchmark to prevent .NET assembly-version unification from mixing the two package graphs. Current Track 2 dependencies do not support .NET Core 3.1, so latest-package results are limited to .NET 8 and .NET 10.

| Runtime | Track 1 mean | Track 2 latest mean | Track 1 allocated | Track 2 latest allocated | Time saved | Allocation saved |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| .NET 8.0.30 | 23.96 ms | 13.38 ms | 1338.10 KB | 559.29 KB | 44.16% | 58.20% |
| .NET 10.0.11 | 22.58 ms | 13.53 ms | 1341.84 KB | 560.06 KB | 40.08% | 58.26% |
