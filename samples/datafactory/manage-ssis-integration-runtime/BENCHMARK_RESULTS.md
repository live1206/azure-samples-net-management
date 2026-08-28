# SSIS integration runtime management benchmark results

## Scenario

This benchmark is derived from a production Track 1 service pattern. Both implementations:

1. Construct an Azure-SSIS managed integration runtime definition.
2. Create or update the integration runtime.
3. Retrieve its status.
4. Start it.
5. List VM sizes for the location.
6. Filter supported node sizes and project name, core count, and memory.

Account-token acquisition and application model mapping are outside the measured operation. Both SDKs use cached clients created during benchmark setup.

## Packages

| Implementation | Packages |
| --- | --- |
| Generated Track 1 | `Microsoft.Azure.Management.Compute` 24.1.0; `Microsoft.Azure.Management.DataFactory` 2.3.1 |
| Track 2 | `Azure.ResourceManager.Compute` 1.16.0; `Azure.ResourceManager.DataFactory` 1.11.1 |

The supplied service also references `Microsoft.Azure.Management.ResourceManager.SignedWithSha256` 2.0.0-preview and `Microsoft.Azure.Management.Sql` 1.24.0-preview, but this code path does not call either package, so they are not included. Current Track 2 dependencies do not support .NET Core 3.1, so the comparison uses .NET 8 and .NET 10.

## Request parity

A single invocation of each implementation sends the same four requests:

| Method | Operation | Track 1 count | Track 2 count |
| --- | --- | ---: | ---: |
| PUT | Create or update integration runtime | 1 | 1 |
| POST | Get integration runtime status | 1 | 1 |
| POST | Start integration runtime | 1 | 1 |
| GET | List VM sizes by location | 1 | 1 |

The mock server's `/__mock/stats` endpoint was used to verify the paths and counts.

## CPU-focused methodology

The initial four-operation scenario was too short for reliable `Process.TotalProcessorTime` sampling. The refined benchmark therefore:

- Performs 100 scenarios per measured benchmark invocation.
- Reports each scenario as one BenchmarkDotNet operation using `OperationsPerInvoke = 100`.
- Executes three independent process launches.
- Performs three warmup and ten measured iterations per launch.
- Executes one unmeasured Track 1 and Track 2 scenario during global setup to initialize clients and serializers.
- Samples client-process CPU around each 100-scenario batch, producing 1,500 sampled scenarios per SDK per launch.
- Excludes mock-server CPU because the server runs in another process.

## Results

CPU milliseconds are the arithmetic mean of the three process-launch CPU samples.

| Runtime | Track 1 CPU ms/op | Track 2 CPU ms/op | CPU core-time saved | Track 1 allocated | Track 2 allocated | Allocation saved |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| .NET 8.0.30 | 3.8245 | 3.5578 | 6.97% | 122.86 KB | 141.98 KB | -15.56% |
| .NET 10.0.11 | 3.9251 | 3.1342 | 20.15% | 123.04 KB | 141.40 KB | -14.92% |

### CPU samples by process launch

| Runtime | Implementation | Launch 1 | Launch 2 | Launch 3 |
| --- | --- | ---: | ---: | ---: |
| .NET 8.0.30 | Track 1 | 3.7467 | 3.8067 | 3.9200 |
| .NET 8.0.30 | Track 2 | 3.6867 | 3.4200 | 3.5667 |
| .NET 10.0.11 | Track 1 | 3.7410 | 4.1753 | 3.8591 |
| .NET 10.0.11 | Track 2 | 3.1685 | 3.1581 | 3.0761 |

Track 1 is the baseline. A negative allocation saving means Track 2 allocated more managed memory. The longer CPU-focused measurement reverses the original short-run .NET 8 CPU result, confirming that the earlier `TotalProcessorTime` sample was too short to support a conclusion.
