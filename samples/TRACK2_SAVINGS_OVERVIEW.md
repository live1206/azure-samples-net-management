# Track 2 savings versus Track 1 overview

This document aggregates Track 2 versus Track 1 results for six normalized management scenarios. Track 1 is the baseline in every row. Runtime-versus-runtime comparisons and the latest Track 2 package experiment are intentionally excluded.

## Track 1 package versions

The original tables use the historical Fluent sample baseline:

- `Microsoft.Azure.Management.Fluent` 1.36.1

Because service teams more commonly used the generated Track 1 clients, the primary baseline is being replaced scenario by scenario with the final published generated packages:

- `Microsoft.Azure.Management.Compute` 61.0.0
- `Microsoft.Azure.Management.Network` 26.0.0
- `Microsoft.Azure.Management.ResourceManager` 3.17.4-preview

The generated Track 1 IP-address result is already available in the [detailed IP-address results](network/manage-ip-address/BENCHMARK_RESULTS.md). Until the remaining generated-client reruns are complete, the aggregate tables below continue to show the Fluent 1.36.1 baseline.

Savings are calculated as `(Track 1 - Track 2) / Track 1 * 100`. Positive values mean Track 2 consumed less time, CPU core-time, or managed memory.

## How the comparison was prepared

1. Historical Fluent Track 1 implementations were recovered from archived Azure-Samples repositories.
2. Track 1 and Track 2 were placed in separate projects and normalized to create equivalent resources and perform equivalent operations and cleanup.
3. Both clients call the same scenario-specific loopback ARM server with deterministic terminal responses, no-op credentials, and retries disabled. The server runs in a separate process.
4. Sample and benchmark projects use matching `netcoreapp3.1`, `net8.0`, or `net10.0` targets.
5. BenchmarkDotNet performs one launch, three warmups, ten measured iterations, and ten invocations per iteration.
6. Client process CPU time is sampled around every scenario invocation using `Process.TotalProcessorTime`. CPU milliseconds per operation measure core-time across all client-process threads.

See the [full benchmark process](network/manage-ip-address/BENCHMARK_PROCESS.md) and linked scenario results for details.

## .NET Core 3.1.32

| Scenario | Wall time saved | CPU core-time saved | Allocation saved | Details |
| --- | ---: | ---: | ---: | --- |
| Manage IP address | 32.59% | 30.66% | 48.83% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 34.85% | 38.98% | 61.06% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 33.16% | 36.23% | 43.23% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 40.60% | 43.67% | 59.91% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 17.61% | 42.95% | 61.06% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 65.11% | 63.31% | 78.66% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## .NET 8.0.30

| Scenario | Wall time saved | CPU core-time saved | Allocation saved | Details |
| --- | ---: | ---: | ---: | --- |
| Manage IP address | 37.59% | 43.20% | 56.22% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 41.44% | 51.91% | 66.15% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 34.29% | 40.00% | 49.88% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 44.64% | 55.15% | 66.60% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 28.79% | 50.60% | 68.04% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 68.91% | 74.76% | 82.56% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## .NET 10.0.11

| Scenario | Wall time saved | CPU core-time saved | Allocation saved | Details |
| --- | ---: | ---: | ---: | --- |
| Manage IP address | 38.80% | 43.82% | 56.37% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | 45.13% | 53.75% | 66.22% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | 41.12% | 45.08% | 50.27% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | 48.59% | 55.79% | 66.63% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | 35.01% | 56.13% | 67.73% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | 66.99% | 75.45% | 82.57% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

## Scope and interpretation

Wall-clock time includes loopback waits and scheduler delay. CPU core-time measures total user and kernel CPU consumed by all threads in the client benchmark process; the mock server's CPU is excluded. It can exceed wall time when GC, thread-pool work, or explicit parallel operations use multiple cores. The parallel-VM scenario demonstrates why wall time alone is not a CPU-cost metric.

CPU samples include warmup and measured invocations after global setup, while BenchmarkDotNet's reported wall-time mean is based on its measured iterations. CPU results should therefore be treated as process-level operational estimates rather than cycle-accurate hardware-counter measurements. Scenario complexity differs, so compare percentages primarily within each scenario.
