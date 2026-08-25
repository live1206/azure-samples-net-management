# Process for benchmarking Track 1 and Track 2 management SDKs

This document describes the process used to compare the Track 1 Fluent management SDK with the Track 2 `Azure.ResourceManager` SDK for the IP-address management scenario. Use it as a template when adding more scenarios.

The goal is to measure client-side SDK execution against deterministic local ARM responses. It is not intended to measure Azure service latency.

## 1. Find the corresponding Track 1 sample

Some Track 1 samples were maintained in separate repositories before they were consolidated into this repository. A folder's local Git history may therefore begin with a Track 2 implementation.

Search for distinctive names, comments, and resource-group prefixes from the Track 2 sample. For this scenario, the previous implementation was found in:

- Repository: `Azure-Samples/network-dotnet-manage-ip-address`
- Last Track 1 tree: commit `b4e769be7c13d87e833126f0acc48db6ff0da075`
- Track 1 package: `Microsoft.Azure.Management.Fluent` 1.36.1

Record the source repository and commit so that later changes can be traced back to the released sample.

## 2. Keep Track 1 and Track 2 side by side

Use separate source files and projects so package dependencies do not interfere with each other:

```text
Program.Track1.cs
ManageIPAddress.Track1.csproj
Program.cs
ManageIPAddress.csproj
```

The default project excludes the other entry points:

```xml
<ItemGroup>
  <Compile Remove="Program.Benchmarks.cs" />
  <Compile Remove="Program.MockServer.cs" />
  <Compile Remove="Program.Track1.cs" />
</ItemGroup>
```

The Track 1 project explicitly includes only its source:

```xml
<PropertyGroup>
  <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
</PropertyGroup>

<ItemGroup>
  <Compile Include="Program.Track1.cs" />
</ItemGroup>
```

When multiple projects occupy one directory, give them separate intermediate paths in `Directory.Build.props`. Otherwise parallel restore and build operations can overwrite the shared `obj/project.assets.json` file.

## 3. Multi-target every measured project

The sample and benchmark assemblies should match the runtime being tested. Multi-target the Track 1 sample, Track 2 sample, and benchmark project:

```xml
<TargetFrameworks>netcoreapp3.1;net8.0;net10.0</TargetFrameworks>
```

This produces the following comparisons:

| Benchmark target | Track 1 target | Track 2 target |
| --- | --- | --- |
| `netcoreapp3.1` | `netcoreapp3.1` | `netcoreapp3.1` |
| `net8.0` | `net8.0` | `net8.0` |
| `net10.0` | `net10.0` | `net10.0` |

Do not reference a `netcoreapp3.1` sample assembly from every benchmark target if the objective is to compare code built for each corresponding framework.

## 4. Normalize the scenarios

The original samples may describe the same high-level task while issuing substantially different operations. Normalize them before benchmarking.

For the IP-address scenario, both implementations now perform this sequence:

1. Create a resource group.
2. Create dynamic IPv4 public IP 1.
3. Create a `/16` virtual network containing a `/28` subnet.
4. Create a NIC associated with public IP 1.
5. Create a `Standard_D3_v2` Windows Server 2016 Datacenter VM using the existing NIC.
6. Refresh the NIC and retrieve public IP 1.
7. Create public IP 2.
8. Update the NIC to use public IP 2.
9. Refresh the NIC and retrieve public IP 2.
10. Remove the public IP association from the NIC.
11. Delete both public IPs.
12. Delete the resource group.

Normalize all inputs that can affect behavior:

- Region
- Resource names and suffix generation
- Address spaces and subnet prefixes
- Public-IP allocation method and version
- VM image
- VM size
- Admin credentials
- Verification reads
- Deletion and cleanup behavior

Avoid convenience methods that perform hidden setup only on one side. For example, the original Fluent VM builder created a VNet and NIC implicitly, while Track 2 created them explicitly. The normalized Track 1 scenario creates those resources explicitly before creating the VM.

### Normalization checklist

- [ ] Both scenarios create equivalent resource types.
- [ ] Both scenarios use equivalent resource payloads.
- [ ] Both scenarios perform equivalent verification reads.
- [ ] Both scenarios remove and delete the same resources.
- [ ] Both scenarios use the same names, region, image, and size patterns.
- [ ] The measured method boundaries are equivalent.
- [ ] HTTP logging is disabled for both clients.
- [ ] Retries are disabled or configured equivalently in mock mode.

Track 1 abstractions may still issue hidden requests. That behavior is part of the Track 1 client cost, but the mock server should be able to identify unexpected or unsupported requests.

## 5. Implement one shared mock ARM server

Use a loopback HTTP server rather than separate in-memory transports. Track 1 uses the `Microsoft.Rest` stack while Track 2 uses `Azure.Core.Pipeline`; separate transport implementations would add different mock-transport overhead to each measurement.

The shared server for this scenario is implemented by:

```text
MockArmServer.csproj
Program.MockServer.cs
```

It:

- Listens on `MOCK_ARM_ENDPOINT`.
- Stores resource representations in memory.
- Routes by HTTP method and ARM resource path.
- Ignores SDK-specific `api-version` query values.
- Completes resource IDs and child IDs.
- Adds `provisioningState: Succeeded`.
- Maintains NIC state across public-IP updates.
- Returns terminal responses so long-running operations do not poll.
- Supports resource deletion and resource-group cleanup.

Useful control endpoints are:

```text
GET  /__mock/health
POST /__mock/reset
```

### Response requirements

At minimum, prepare responses for:

- Resource-group PUT and DELETE
- Public-IP PUT, GET, and DELETE
- Virtual-network PUT
- Network-interface PUT and GET
- Virtual-machine PUT

A successful create or update should return a final resource body with HTTP 200 and a succeeded provisioning state. A response that looks asynchronous, such as HTTP 202 with `Azure-AsyncOperation`, changes the benchmark into a polling benchmark.

The response body should be a superset accepted by both generations. Echoing the request and filling server-generated fields is a convenient way to preserve SDK-specific payload details.

## 6. Configure both clients for mock mode

Both sample programs detect `MOCK_ARM_ENDPOINT`. Live authentication remains unchanged when it is absent.

### Track 1

Track 1 uses:

- A custom `AzureEnvironment`
- No-op `ServiceClientCredentials`
- `RestClient.Configure()`
- A delegating handler that routes the mock HTTPS endpoint to the loopback HTTP server

### Track 2

Track 2 uses:

- A custom `ArmEnvironment`
- A no-op `TokenCredential`
- `ArmClientOptions`
- `MaxRetries = 0`
- A matching delegating handler that routes the mock HTTPS endpoint to HTTP

Azure.Core rejects bearer-token authentication over a non-TLS request URI. Both clients therefore see an HTTPS ARM endpoint in their pipelines, while equivalent final handlers rewrite only the loopback transport request to HTTP. Applying the same adaptation to both clients avoids giving only one implementation the additional handler cost.

Client factory methods such as `CreateMockClient` make it possible for the benchmark project to create clients during global setup rather than during the measured operation.

## 7. Create the BenchmarkDotNet project

The benchmark project is:

```text
ManageIPAddress.Benchmarks.csproj
Program.Benchmarks.cs
```

It references both projects using aliases because both sample assemblies contain `ManageIPAddress.Program`:

```xml
<ProjectReference Include="ManageIPAddress.Track1.csproj" Aliases="Track1" />
<ProjectReference Include="ManageIPAddress.csproj" Aliases="Track2" />
```

Use the aliases in source:

```csharp
extern alias Track1;
extern alias Track2;

using Track1Program = Track1::ManageIPAddress.Program;
using Track2Program = Track2::ManageIPAddress.Program;
```

The benchmark setup should:

1. Verify `GET /__mock/health`.
2. Create one Track 1 client.
3. Create one Track 2 client.
4. Keep client creation outside the measured methods.

The measured methods should invoke the complete normalized scenario. Use Track 1 as the BenchmarkDotNet baseline when calculating Track 2 ratios.

The old Track 1 Fluent package triggers BenchmarkDotNet's optimization validator. The benchmark uses `ConfigOptions.DisableOptimizationsValidator` because it intentionally measures the shipped package rather than a locally rebuilt replacement.

## 8. Run the mock server out of process

Do not host the mock server inside the benchmark process. Its allocations and CPU work could otherwise be attributed to the client benchmark.

`run-benchmarks.sh`:

1. Starts `MockArmServer.csproj` in a child process.
2. Waits for the health endpoint.
3. Runs BenchmarkDotNet for the selected target framework.
4. Terminates the server on completion or failure.

Run each framework separately:

```bash
# .NET 8, the default
./run-benchmarks.sh

# .NET Core 3.1
BENCHMARK_FRAMEWORK=netcoreapp3.1 ./run-benchmarks.sh

# .NET 10
BENCHMARK_FRAMEWORK=net10.0 ./run-benchmarks.sh
```

The script uses `DOTNET_ROLL_FORWARD=LatestPatch` by default. This ensures that a missing target runtime fails instead of silently running on a different major runtime.

## 9. Install and verify runtimes

List installed runtimes and SDKs before testing:

```bash
dotnet --list-runtimes
dotnet --list-sdks
```

.NET Core 3.1 is unsupported. On current Linux distributions it may require legacy OpenSSL and ICU compatibility libraries that are no longer provided by the OS. Prefer an isolated container or VM with a supported historical distribution for repeatable 3.1 measurements. If local compatibility libraries are used, record their versions and how they were supplied.

Verify the runtime printed by BenchmarkDotNet for every run. The host and job should both report the requested version.

Also verify the target metadata of each output assembly when changing project references or target frameworks. A benchmark running on .NET 10 does not prove that its referenced sample assembly was built for `net10.0`.

## 10. Record results and percentages

Store results in a scenario-local Markdown file such as `BENCHMARK_RESULTS.md`. Record:

- Date
- Command
- Target framework and exact runtime
- .NET SDK
- BenchmarkDotNet version and job configuration
- OS and CPU
- JIT and available instruction sets
- GC mode
- Package versions
- Mean, error, and standard deviation
- Managed allocation
- Baselines and percentage formulas
- Compatibility libraries or special environment variables

Use this formula for percentage saved:

```text
(baseline - comparison) / baseline * 100
```

A positive value means the comparison used less time or memory. A negative value means it used more.

Keep the comparison dimensions separate:

### SDK comparison

Hold the runtime constant and compare Track 2 with Track 1:

```text
Track 2 saving = (Track 1 - Track 2) / Track 1 * 100
```

### Runtime comparison

Hold the SDK constant and compare a newer runtime with .NET Core 3.1:

```text
Runtime saving = (.NET Core 3.1 - newer runtime) / .NET Core 3.1 * 100
```

Do not describe a Track 2 versus Track 1 percentage as a .NET 10 versus .NET Core 3.1 percentage. Include the underlying mean values in percentage tables to make the baseline unambiguous.

BenchmarkDotNet's `Mean` is wall-clock elapsed time. It is not direct CPU consumption. The benchmark projects therefore also wrap every invocation with a process CPU recorder based on `Process.TotalProcessorTime`. The recorder reports CPU milliseconds per operation across all client-process threads. Because the mock server runs out of process, its CPU is excluded. These process-level samples are suitable for estimating vCore cost; hardware counters can be added when cycle-level precision and platform permissions are available.

## 11. Validate before recording results

For every scenario and target framework:

```bash
dotnet build ManageIPAddress.Benchmarks.csproj --configuration Release
BENCHMARK_FRAMEWORK=<target> ./run-benchmarks.sh
```

Confirm that:

- [ ] Both benchmark methods complete successfully.
- [ ] Both methods use the requested runtime.
- [ ] Both referenced sample assemblies use the matching target framework.
- [ ] The mock server remains healthy throughout the run.
- [ ] Every invocation cleans up its in-memory resources.
- [ ] No live Azure credentials or endpoints are used.
- [ ] There are no unexpected retries or LRO polls.
- [ ] The result document contains the final rerun, not an earlier validation run.
- [ ] Generated `BenchmarkDotNet.Artifacts` are not committed unless intentionally required.

## 12. Suggested layout for another scenario

Use the same structure under the scenario folder:

```text
<Scenario>.Track1.csproj
<Scenario>.csproj
<Scenario>.Benchmarks.csproj
MockArmServer.csproj
Program.Track1.cs
Program.cs
Program.Benchmarks.cs
Program.MockServer.cs
Directory.Build.props
run-benchmarks.sh
BENCHMARK_RESULTS.md
```

The mock-server implementation can eventually be extracted into shared test infrastructure. Until then, keep scenario-specific response completion and state transitions close to the scenario so reviewers can verify that the responses match the operations being benchmarked.

## 13. Review principles

Before treating a result as an SDK performance comparison, answer these questions:

1. Are the resulting ARM resources equivalent?
2. Are the high-level operation sequences equivalent?
3. Are both clients using the same loopback server and terminal response behavior?
4. Are client construction and server startup outside the measurement?
5. Are both samples compiled for the runtime being measured?
6. Are logging, retries, and cleanup configured equivalently?
7. Is the percentage baseline explicitly stated?
8. Can another developer rerun the test from the documented command?

If any answer is no, document the difference before publishing the comparison.
