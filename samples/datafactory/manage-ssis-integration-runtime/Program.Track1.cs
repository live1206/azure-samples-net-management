// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Rest;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ManageSsisIntegrationRuntime
{
    public static class Track1Scenario
    {
        private const string ResourceGroupName = "ssis-benchmark-rg";
        private const string FactoryName = "ssis-benchmark-df";
        private const string IntegrationRuntimeName = "ssis-ir";
        private const string Location = "eastus";

        public sealed class Clients
        {
            public Clients(
                DataFactoryManagementClient dataFactory,
                ComputeManagementClient compute)
            {
                DataFactory = dataFactory;
                Compute = compute;
            }

            public DataFactoryManagementClient DataFactory { get; }

            public ComputeManagementClient Compute { get; }
        }

        public static async Task RunAsync(Clients clients, string subscriptionId)
        {
            var integrationRuntime = new ManagedIntegrationRuntime
            {
                SsisProperties = new IntegrationRuntimeSsisProperties
                {
                    CatalogInfo = new IntegrationRuntimeSsisCatalogInfo
                    {
                        CatalogServerEndpoint = "server.database.windows.net",
                        CatalogAdminUserName = "admin",
                        CatalogAdminPassword = new SecureString("Benchmark!Passw0rd123"),
                        CatalogPricingTier = "Standard"
                    },
                    LicenseType = "LicenseIncluded",
                    Edition = "Standard"
                },
                ComputeProperties = new IntegrationRuntimeComputeProperties
                {
                    NodeSize = "Standard_D2_v3",
                    NumberOfNodes = 2,
                    Location = Location,
                    MaxParallelExecutionsPerNode = 4
                }
            };

            var resource = new IntegrationRuntimeResource(
                integrationRuntime,
                name: IntegrationRuntimeName,
                type: IntegrationRuntimeType.Managed);

            await clients.DataFactory.IntegrationRuntimes.CreateOrUpdateAsync(
                ResourceGroupName,
                FactoryName,
                IntegrationRuntimeName,
                resource);

            await clients.DataFactory.IntegrationRuntimes.GetStatusAsync(
                ResourceGroupName,
                FactoryName,
                IntegrationRuntimeName);

            await clients.DataFactory.IntegrationRuntimes.BeginStartAsync(
                ResourceGroupName,
                FactoryName,
                IntegrationRuntimeName);

            var sizes = await clients.Compute.VirtualMachineSizes.ListAsync(Location);
            _ = sizes
                .Where(size => size.Name == "Standard_D2_v3" || size.Name == "Standard_D4_v3")
                .Select(size => Tuple.Create(size.Name, size.NumberOfCores, size.MemoryInMB))
                .ToList();
        }

        public static async Task Main(string[] args)
        {
            string endpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT")
                ?? "http://127.0.0.1:5050";
            string subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")
                ?? "00000000-0000-0000-0000-000000000000";

            await RunAsync(CreateMockClient(endpoint, subscriptionId), subscriptionId);
        }

        public static Clients CreateMockClient(string endpoint, string subscriptionId)
        {
            var mockUri = new Uri(EnsureTrailingSlash(endpoint));
            var tlsEndpoint = new UriBuilder(mockUri)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = mockUri.Port
            }.Uri;
            var credentials = new MockCredentials();

            var dataFactory = new DataFactoryManagementClient(credentials, new MockEndpointHandler())
            {
                SubscriptionId = subscriptionId,
                BaseUri = tlsEndpoint
            };
            var compute = new ComputeManagementClient(credentials, new MockEndpointHandler())
            {
                SubscriptionId = subscriptionId,
                BaseUri = tlsEndpoint
            };

            return new Clients(dataFactory, compute);
        }

        private static string EnsureTrailingSlash(string endpoint) =>
            endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : endpoint + "/";

        private sealed class MockEndpointHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                request.RequestUri = new UriBuilder(request.RequestUri)
                {
                    Scheme = Uri.UriSchemeHttp,
                    Port = request.RequestUri.Port
                }.Uri;
                return base.SendAsync(request, cancellationToken);
            }
        }

        private sealed class MockCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
