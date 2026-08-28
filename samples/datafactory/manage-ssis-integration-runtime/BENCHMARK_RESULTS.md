# SSIS integration runtime management benchmark results

## Scenario

This benchmark is derived from a production Track 1 service pattern. Both implementations:

1. Construct an Azure-SSIS managed integration runtime definition.
2. Create or update the integration runtime.
3. Retrieve its status.
4. Start it.
5. List VM sizes for the location.
6. Filter supported node sizes and project name, core count, and memory.

Account-token acquisition and the service application's model mapping are outside the measured operation. Both SDKs use cached clients created during benchmark setup.

## Packages

| Implementation | Packages |
| --- | --- |
| Generated Track 1 | `Microsoft.Azure.Management.Compute` 24.1.0; `Microsoft.Azure.Management.DataFactory` 2.3.1 |
| Track 2 | `Azure.ResourceManager.Compute` 1.16.0; `Azure.ResourceManager.DataFactory` 1.11.1 |

The supplied service also references `Microsoft.Azure.Management.ResourceManager.SignedWithSha256` 2.0.0-preview and `Microsoft.Azure.Management.Sql` 1.24.0-preview, but this code path does not call either package, so they are not included in the benchmark project.

Current Track 2 dependencies do not support .NET Core 3.1, so this comparison uses .NET 8 and .NET 10.

## Results

| Runtime | Generated Track 1 mean | Track 2 mean | Generated Track 1 CPU ms/op | Track 2 CPU ms/op | CPU core-time saved | Generated Track 1 allocated | Track 2 allocated | Allocation saved |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| .NET 8.0.30 | 5.804 ms | 4.934 ms | 5.8865 | 7.0213 | -19.28% | 133.74 KB | 148.47 KB | -11.01% |
| .NET 10.0.11 | 4.266 ms | 3.818 ms | 4.6854 | 4.4186 | 5.69% | 132.40 KB | 145.80 KB | -10.12% |

Track 1 is the baseline. A negative saving means Track 2 consumed more CPU core-time or allocated more memory. CPU core-time is sampled from `Process.TotalProcessorTime` across all client-process threads; mock-server CPU is excluded.
