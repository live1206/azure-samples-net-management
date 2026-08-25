---
page_type: sample
languages:
- csharp
products:
- azure
description: "This code samples will show you how to manage virtual network using Azure SDK for .NET."
urlFragment: network-manage-virtual-network
---
# Getting started - Managing Virtual Network using Azure .NET SDK

This code sample will show you how to manage Virtual Network using Azure SDK for .NET.

## Features

This project framework provides examples for the following services:

### Network

| SDK | Source | Project |
| --- | --- | --- |
| Track 1 (`Microsoft.Azure.Management.Fluent`) | `Program.Track1.cs` | `ManageVirtualNetwork.Track1.csproj` |
| Track 2 legacy (`Azure.ResourceManager.*`) | `Program.cs` | `ManageVirtualNetwork.csproj` |
| Track 2 latest (`Azure.ResourceManager.*`) | `Program.cs` | `ManageVirtualNetwork.Track2Latest.csproj` |

## Getting Started

### Prerequisites

You will need the following values to authenticate to Azure

- **Subscription ID**
- **Client ID**
- **Client Secret**
- **Tenant ID**

These values can be obtained from the portal, here's the instructions:

### Get Subscription ID

1. Login into your Azure account
2. Select `Subscriptions` under `Navigation` section in the portal
3. Select whichever subscription is needed
4. Click on `Overview`
5. Copy the `Subscription ID`

### Get Client ID / Client Secret / Tenant ID

For information on how to get Client ID, Client Secret, and Tenant ID,
please refer to [this
document](https://docs.microsoft.com/azure/active-directory/develop/howto-create-service-principal-portal)

### Setting Environment Variables

After you obtained the values, you need to set the following values as
your environment variables

- `AZURE_CLIENT_ID`
- `AZURE_CLIENT_SECRET`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

To set the following environment variables on your development system:

Windows: (Note: Administrator access is required)

1. Open the System Control Panel
2. Select `Advanced system settings`
3. Open the `Advanced` tab, then click `Environment Variables...`
   button.
4. Click on the property you would like to change, then click the `Edit…`
   button. If the property name is not listed, then click the `New…`
   button.

Linux-based OS :

```bash
export AZURE_CLIENT_ID="__CLIENT_ID__"
export AZURE_CLIENT_SECRET="__CLIENT_SECRET__"
export AZURE_TENANT_ID="__TENANT_ID__"
export AZURE_SUBSCRIPTION_ID="__SUBSCRIPTION_ID__"
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
cd samples/network/manage-virtual-network
```

3. Run either implementation with `dotnet run --project <project>`.

### Running the benchmarks

The samples and benchmark target .NET Core 3.1, .NET 8, and .NET 10. Run the
mock server and benchmark together with:

```bash
# .NET 8 (default)
./run-benchmarks.sh

BENCHMARK_FRAMEWORK=netcoreapp3.1 ./run-benchmarks.sh
BENCHMARK_FRAMEWORK=net10.0 ./run-benchmarks.sh

# Latest Track 2 packages (supports .NET 8 and .NET 10)
./run-latest-benchmarks.sh
```

See [`BENCHMARK_RESULTS.md`](BENCHMARK_RESULTS.md) for recorded results and
[`../manage-ip-address/BENCHMARK_PROCESS.md`](../manage-ip-address/BENCHMARK_PROCESS.md)
for the reusable process.

## This sample shows how to do following operations to manage Virtual Network

- Create a virtual network with Subnets.
- Update a virtual network.
- Create virtual machines in the virtual network subnets.
- Create another virtual network.
- List virtual networks.

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
