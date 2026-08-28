# Manage an Azure-SSIS integration runtime

This scenario compares a production-style generated Track 1 implementation with an equivalent Track 2 implementation.

| SDK | Source | Project |
| --- | --- | --- |
| Generated Track 1 | `Program.Track1.cs` | `IntegrationRuntime.Track1.csproj` |
| Track 2 | `Program.cs` | `IntegrationRuntime.Track2.csproj` |

## Run the benchmarks

The runner starts the scenario-specific mock ARM server in a separate process:

```bash
# .NET 8 (default)
./run-benchmarks.sh

# .NET 10
BENCHMARK_FRAMEWORK=net10.0 ./run-benchmarks.sh
```

See [`BENCHMARK_RESULTS.md`](BENCHMARK_RESULTS.md) for package versions, CPU core-time, managed allocations, and interpretation.
