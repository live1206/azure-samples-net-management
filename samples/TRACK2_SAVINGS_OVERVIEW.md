# Track 2 savings versus generated Track 1 overview

This document aggregates the final comparison between Track 2 and the published generated Track 1 management clients commonly used by service teams. Generated Track 1 is the baseline in every row.

## Track 1 package versions

The standard sample scenarios use the final published generated Track 1 clients:

- `Microsoft.Azure.Management.Compute` 61.0.0
- `Microsoft.Azure.Management.Network` 26.0.0
- `Microsoft.Azure.Management.ResourceManager` 3.17.4-preview

The production SSIS scenario uses its original service package versions and is presented separately below.

Savings are calculated as:

```text
(Generated Track 1 - Track 2) / Generated Track 1 * 100
```

A positive value means Track 2 consumed less CPU core-time or allocated less managed memory.

## How the comparison is prepared

1. Generated Track 1 source and packages are recovered from the final published Track 1 SDK generation.
2. Generated Track 1 and Track 2 use separate projects and benchmark processes so their package graphs cannot be unified by the .NET loader.
3. The implementations are normalized to create equivalent resources and perform equivalent reads, updates, and cleanup.
4. Both clients call the same scenario-specific loopback ARM server with deterministic terminal responses, no-op credentials, and retries disabled. The server runs in a separate process.
5. Sample and benchmark assemblies use matching `netcoreapp3.1`, `net8.0`, or `net10.0` targets.
6. Client CPU core-time is sampled around every scenario invocation using `Process.TotalProcessorTime`. It includes user and kernel CPU consumed by all client-process threads and excludes the mock-server process.
7. Managed allocations are reported by BenchmarkDotNet 0.14.0. The job uses one launch, three warmups, ten measured iterations, and ten invocations per iteration.

See the [full benchmark process](network/manage-ip-address/BENCHMARK_PROCESS.md) and linked scenario results for raw measurements.

## Completed generated Track 1 comparisons

| Scenario | Runtime | CPU core-time saved by Track 2 | Allocation saved by Track 2 | Details |
| --- | --- | ---: | ---: | --- |
| Manage IP address | .NET Core 3.1.32 | 20.17% | 54.75% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage IP address | .NET 8.0.30 | 32.75% | 58.82% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage IP address | .NET 10.0.11 | 37.00% | 59.05% | [Results](network/manage-ip-address/BENCHMARK_RESULTS.md) |
| Manage virtual network | .NET Core 3.1.32 | 18.70% | 59.59% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Manage virtual network | .NET 8.0.30 | 41.87% | 62.97% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Manage virtual network | .NET 10.0.11 | 39.32% | 63.17% | [Results](network/manage-virtual-network/BENCHMARK_RESULTS.md) |
| Create virtual machine | .NET Core 3.1.32 | 20.00% | 39.30% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machine | .NET 8.0.30 | 29.47% | 44.00% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machine | .NET 10.0.11 | 28.18% | 44.72% | [Results](compute/create-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | .NET Core 3.1.32 | 31.40% | 54.52% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | .NET 8.0.30 | 24.62% | 60.46% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Manage virtual machine | .NET 10.0.11 | 51.01% | 60.66% | [Results](compute/manage-virtual-machine/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | .NET Core 3.1.32 | 32.24% | 64.56% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | .NET 8.0.30 | 50.39% | 69.85% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Create virtual machines in parallel | .NET 10.0.11 | 59.73% | 69.49% | [Results](compute/create-virtual-machines-in-parallel/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | .NET Core 3.1.32 | 25.00% | 50.06% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | .NET 8.0.30 | 32.29% | 57.64% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |
| Manage virtual-machine extensions | .NET 10.0.11 | 38.30% | 57.90% | [Results](compute/manage-virtual-machine-extension/BENCHMARK_RESULTS.md) |

Historical Fluent results are intentionally excluded from this final overview.

## Production service scenario: Manage SSIS integration runtime

This scenario comes from a real service implementation rather than an Azure SDK sample. Its Track 1 code uses the service's original packages:

- `Microsoft.Azure.Management.Compute` 24.1.0
- `Microsoft.Azure.Management.DataFactory` 2.3.1

The supplied service also references ResourceManager and SQL Track 1 packages, but this particular execution path does not call them, so they are excluded from the measured project. The equivalent Track 2 implementation uses `Azure.ResourceManager.Compute` 1.16.0 and `Azure.ResourceManager.DataFactory` 1.11.1.

| Runtime | CPU core-time saved by Track 2 | Allocation saved by Track 2 | Details |
| --- | ---: | ---: | --- |
| .NET 8.0.30 | 6.97% | -15.56% | [Results](datafactory/manage-ssis-integration-runtime/BENCHMARK_RESULTS.md) |
| .NET 10.0.11 | 20.15% | -14.92% | [Results](datafactory/manage-ssis-integration-runtime/BENCHMARK_RESULTS.md) |

The improvement is smaller than in the broader sample scenarios for several reasons:

- The old Track 1 Data Factory and Compute clients are lightweight AutoRest operation clients that call service methods directly, without an ARM resource hierarchy.
- The scenario is small: one integration-runtime create/update, one status request, one start request, and one VM-size list request. Fixed Track 2 pipeline and model overhead is therefore a larger fraction of the total work.
- Track 2 constructs resource identifiers, resource and collection wrappers, `ArmOperation` state, pageable state, and newer polymorphic Data Factory models.
- The scenario uses cached clients, so Track 2 does not benefit from improvements in client construction during the measured operation.
- The newer serialization and resource abstractions reduce CPU core-time, especially on .NET 10, but allocate about 15% more memory for this narrow workflow.

The mock server verified exact request parity: both implementations send the same four methods and paths. Because the initial workload was too short for stable process CPU sampling, the final result uses batches of 100 scenarios, three independent process launches, and 1,500 sampled scenarios per SDK per launch.

## Scope and interpretation

CPU core-time is the primary cost metric. It can exceed elapsed wall time when GC, thread-pool work, or explicit parallel operations use multiple cores. Allocation values cover managed allocations in the client benchmark process. Neither metric includes mock-server CPU or Azure service behavior.

Scenario complexity and request sequences differ, so compare percentages within a scenario rather than averaging them across scenarios.
