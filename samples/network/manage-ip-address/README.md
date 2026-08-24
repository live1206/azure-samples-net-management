---
page_type: sample
languages:
- csharp
products:
- azure
description: "This code samples will show you how to manage ip address using Azure SDK for .NET."
urlFragment: network-manage-ip-address
---
# Getting started - Managing IP Address using Azure .NET SDK

This code sample will show you how to manage IP Address using Azure SDK for .NET.

## Features

This project framework provides examples for the following services:

### Network

The two implementations are kept side by side so the same scenario can be compared across SDK generations:

| SDK | Source | Project |
| --- | --- | --- |
| Track 1 (`Microsoft.Azure.Management.Fluent`) | `Program.Track1.cs` | `ManageIPAddress.Track1.csproj` |
| Track 2 (`Azure.ResourceManager.*`) | `Program.cs` | `ManageIPAddress.csproj` |

## Getting Started

### Prerequisites

You will need the following values to authenticate to Azure

- **Subscription ID**
- **Client ID**
- **Client Secret**
- **Tenant ID**

These values can be obtained from the portal, here's the instructions:

#### Get Subscription ID

1. Login into your Azure account
2. Select `Subscriptions` under `Navigation` section in the portal
3. Select whichever subscription is needed
4. Click on `Overview`
5. Copy the `Subscription ID`

#### Get Client ID / Client Secret / Tenant ID

For information on how to get Client ID, Client Secret, and Tenant ID,
please refer to [this
document](https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal)

### Authentication

Both samples read the same service-principal environment variables:

```bash
export CLIENT_ID="__CLIENT_ID__"
export CLIENT_SECRET="__CLIENT_SECRET__"
export TENANT_ID="__TENANT_ID__"
export SUBSCRIPTION_ID="__SUBSCRIPTION_ID__"
```

### Installation

To complete this tutorial:

- Install .NET Core latest version for [Linux] or [Windows]

If you don't have an Azure subscription, create a [free account] before you begin.

### Quickstart

1. Clone the repository on your machine:

```bash
git clone https://github.com/Azure-Samples/azure-samples-net-management.git
```

2. Switch to the project folder:

```bash
cd samples/network/manage-ip-address
```

3. Replace the `<password>` placeholder in both source files with a valid password.
4. Run either implementation:

```bash
# Track 1
DOTNET_ROLL_FORWARD=Major dotnet run --project ManageIPAddress.Track1.csproj

# Track 2
dotnet run --project ManageIPAddress.csproj
```

`DOTNET_ROLL_FORWARD=Major` is only needed when the .NET Core 3.1 runtime is not installed.

### Running against the mock ARM server

The shared mock server returns completed ARM resources from local memory, so both
implementations can run without Azure credentials or creating Azure resources.
Start the server in one terminal:

```bash
export MOCK_ARM_ENDPOINT="http://127.0.0.1:5050"
dotnet run --project MockArmServer.csproj
```

Then use the same endpoint and subscription ID for either client:

```bash
export MOCK_ARM_ENDPOINT="http://127.0.0.1:5050"
export SUBSCRIPTION_ID="00000000-0000-0000-0000-000000000000"

# Track 1
DOTNET_ROLL_FORWARD=Major dotnet run --project ManageIPAddress.Track1.csproj

# Track 2
DOTNET_ROLL_FORWARD=Major dotnet run --project ManageIPAddress.csproj
```

The server exposes `GET /__mock/health` for readiness checks and
`POST /__mock/reset` to clear its in-memory resources between runs.

### Running the benchmarks

The BenchmarkDotNet project measures the complete normalized management scenario
for each SDK. Client creation and mock-server startup occur outside the measured
benchmark operation.

Run the benchmark and its mock server together with:

```bash
./run-benchmarks.sh
```

BenchmarkDotNet arguments can be passed through the script, for example:

```bash
./run-benchmarks.sh --filter '*Track1*'
```

The mock server runs in a separate process so its allocations and processing time
are not attributed to either client process. See [`BENCHMARK_RESULTS.md`](BENCHMARK_RESULTS.md)
for a recorded local test run and its runtime details.

## This sample shows how to do following operations to manage IP Address

- Assign a public IP address for a virtual machine during its creation.
- Assign a public IP address for a virtual machine through an virtual machine update action.
- Get the associated public IP address for a virtual machine.
- Get the assigned public IP address for a virtual machine.
- Remove a public IP address from a virtual machine.

## More information

The [Azure Compute documentation] includes a rich set of tutorials and conceptual articles, which serve as a good complement to the samples.

This project has adopted the [Microsoft Open Source Code of Conduct].
For more information see the [Code of Conduct FAQ] or contact [opencode@microsoft.com] with any additional questions or comments.

<!-- LINKS -->
[Linux]: https://dotnet.microsoft.com/download
[Windows]: https://dotnet.microsoft.com/download
[free account]: https://azure.microsoft.com/free/?WT.mc_id=A261C142F
[Azure Portal]: https://portal.azure.com
[Azure Compute documentation]: https://docs.microsoft.com/azure/?product=compute
[Microsoft Open Source Code of Conduct]: https://opensource.microsoft.com/codeofconduct/
[Code of Conduct FAQ]: https://opensource.microsoft.com/codeofconduct/faq/
[opencode@microsoft.com]: mailto:opencode@microsoft.com
