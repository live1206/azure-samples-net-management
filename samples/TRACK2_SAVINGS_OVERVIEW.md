# Track 2 savings versus Track 1 overview

This document aggregates client-side management benchmarks for three SDK configurations:

1. **Track 1:** `Microsoft.Azure.Management.Fluent` 1.36.1 — the baseline.
2. **Track 2 legacy:** the package versions originally pinned by these samples, primarily Compute 1.2.1 and Network 1.6.0.
3. **Track 2 latest:** the latest stable versions available on 2026-08-25: Identity 1.21.0, Compute 1.16.0, Network 1.17.0, and Resources 1.11.2 where directly referenced.

Savings are calculated as `(Track 1 - Track 2) / Track 1 * 100`. Positive values mean Track 2 used less wall-clock time or allocated less managed memory. Runtime-versus-runtime comparisons are intentionally excluded.

## How the comparison was prepared

1. Historical Track 1 Fluent implementations were recovered from archived standalone Azure-Samples repositories.
2. Track 1 and Track 2 were placed in separate projects and normalized to create equivalent resources and perform equivalent reads, updates, and cleanup.
3. Sample and benchmark projects use matching target frameworks for each run.
4. Both SDK generations call the same scenario-specific loopback ARM server with deterministic terminal responses, no-op credentials, and retries disabled. Client creation and server startup are outside the measured operation.
5. BenchmarkDotNet 0.14.0 runs one launch, three warmups, ten measured iterations, and ten invocations per iteration.
6. Legacy and latest Track 2 packages are benchmarked in separate projects and processes. This prevents .NET assembly-version unification from loading one package graph for both tests.

Current Track 2 dependencies do not support .NET Core 3.1, so the latest-package comparison is available only for .NET 8 and .NET 10. See the [full process](network/manage-ip-address/BENCHMARK_PROCESS.md) and linked scenario results for raw measurements.

## .NET Core 3.1.32

| Scenario | Legacy Track 2 time saved | Legacy allocation saved | Latest Track 2 time saved | Latest allocation saved | Details |
| --- | ---: | ---: | ---: | ---: | --- |
| Manage IP address | 32.59% | 48.83% | N/A | N/A | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 34.85% | 61.06% | N/A | N/A | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 33.16% | 43.23% | N/A | N/A | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 40.60% | 59.91% | N/A | N/A | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 17.61% | 61.06% | N/A | N/A | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 65.11% | 78.66% | N/A | N/A | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## .NET 8.0.30

| Scenario | Legacy Track 2 time saved | Legacy allocation saved | Latest Track 2 time saved | Latest allocation saved | Details |
| --- | ---: | ---: | ---: | ---: | --- |
| Manage IP address | 37.59% | 56.22% | 37.35% | 45.42% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 41.44% | 66.15% | 44.16% | 58.20% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 34.29% | 49.88% | 30.58% | 35.86% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 44.64% | 66.60% | 46.78% | 60.78% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 28.79% | 68.04% | 25.36% | 57.64% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 68.91% | 82.56% | 65.73% | 75.92% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## .NET 10.0.11

| Scenario | Legacy Track 2 time saved | Legacy allocation saved | Latest Track 2 time saved | Latest allocation saved | Details |
| --- | ---: | ---: | ---: | ---: | --- |
| Manage IP address | 38.80% | 56.37% | 38.87% | 45.55% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 45.13% | 66.22% | 40.08% | 58.26% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 41.12% | 50.27% | 43.83% | 36.26% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 48.59% | 66.63% | 52.67% | 60.85% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 35.01% | 67.73% | 32.63% | 57.64% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 66.99% | 82.57% | 70.22% | 75.96% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## Scope and interpretation

Time values are wall-clock client-scenario measurements and include loopback response latency. They are not direct CPU-utilization measurements or Azure service latency. Allocation values are managed allocations reported for the benchmark process.

The legacy and latest percentages come from separate benchmark runs, each with its own Track 1 baseline run. Small differences between the two Track 1 measurements are expected benchmark variation; compare package variants primarily by their savings percentages and detailed confidence intervals. Scenario complexity and request sequences differ, so percentages should be compared primarily within each scenario.
