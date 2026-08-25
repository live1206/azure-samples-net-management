# Track 2 savings versus Track 1 overview

This document aggregates the within-runtime Track 2 versus Track 1 benchmark results for the network and compute management scenarios. Track 1 is the baseline in every row.

The percentage saved is calculated as:

```text
(Track 1 - Track 2) / Track 1 * 100
```

A positive value means Track 2 used less wall-clock time or allocated less managed memory than Track 1. Runtime-versus-runtime comparisons are intentionally excluded from this overview.

## .NET Core 3.1.32

| Scenario | Track 2 time saved | Track 2 allocation saved | Detailed results |
| --- | ---: | ---: | --- |
| Manage IP address | 32.59% | 48.83% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 34.85% | 61.06% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 33.16% | 43.23% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 40.60% | 59.91% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 17.61% | 61.06% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 65.11% | 78.66% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## .NET 8.0.30

| Scenario | Track 2 time saved | Track 2 allocation saved | Detailed results |
| --- | ---: | ---: | --- |
| Manage IP address | 37.59% | 56.22% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 41.44% | 66.15% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 34.29% | 49.88% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 44.64% | 66.60% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 28.79% | 68.04% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 68.91% | 82.56% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## .NET 10.0.11

| Scenario | Track 2 time saved | Track 2 allocation saved | Detailed results |
| --- | ---: | ---: | --- |
| Manage IP address | 38.80% | 56.37% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 45.13% | 66.22% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 41.12% | 50.27% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 48.59% | 66.63% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 35.01% | 67.73% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 66.99% | 82.57% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## Scope and interpretation

All scenarios use matching Track 1, Track 2, and benchmark target frameworks for the selected runtime. They run against scenario-specific local mock ARM servers in separate processes. Client creation and mock-server startup are outside the measured operations.

The time values are wall-clock client-scenario measurements and include loopback response latency. They are not direct CPU-utilization measurements and do not represent Azure service latency. Allocation values are managed allocations reported by BenchmarkDotNet for the client benchmark process.

Scenario complexity and request sequences differ, so percentages should be compared primarily within each scenario. See each linked result document for normalized operations, raw means, allocation values, and runtime details.
