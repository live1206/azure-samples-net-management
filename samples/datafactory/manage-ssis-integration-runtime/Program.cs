// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Core.Expressions.DataFactory;
using Azure.Core.Pipeline;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.DataFactory;
using Azure.ResourceManager.DataFactory.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ManageSsisIntegrationRuntime
{
    public static class Track2Scenario
    {
        private const string ResourceGroupName = "ssis-benchmark-rg";
        private const string FactoryName = "ssis-benchmark-df";
        private const string IntegrationRuntimeName = "ssis-ir";

        public static async Task RunAsync(ArmClient client, string subscriptionId)
        {
            var computeProperties = new IntegrationRuntimeComputeProperties
            {
                NodeSize = "Standard_D2_v3",
                NumberOfNodes = 2,
                Location = AzureLocation.EastUS,
                MaxParallelExecutionsPerNode = 4
            };
            var catalogInfo = new IntegrationRuntimeSsisCatalogInfo
            {
                CatalogServerEndpoint = "server.database.windows.net",
                CatalogAdminUserName = "admin",
                CatalogAdminPassword = new DataFactorySecretString("Benchmark!Passw0rd123"),
                CatalogPricingTier = IntegrationRuntimeSsisCatalogPricingTier.Standard
            };
            var ssisProperties = new IntegrationRuntimeSsisProperties
            {
                CatalogInfo = catalogInfo,
                LicenseType = IntegrationRuntimeLicenseType.LicenseIncluded,
                Edition = IntegrationRuntimeEdition.Standard
            };
            var managedIntegrationRuntime = new ManagedIntegrationRuntime
            {
                ComputeProperties = computeProperties,
                SsisProperties = ssisProperties
            };

            var factoryId = DataFactoryResource.CreateResourceIdentifier(
                subscriptionId,
                ResourceGroupName,
                FactoryName);
            var factory = client.GetDataFactoryResource(factoryId);
            var integrationRuntime = (await factory
                .GetDataFactoryIntegrationRuntimes()
                .CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    IntegrationRuntimeName,
                    new DataFactoryIntegrationRuntimeData(managedIntegrationRuntime)))
                .Value;

            await integrationRuntime.GetStatusAsync();
            await integrationRuntime.StartAsync(Azure.WaitUntil.Started);

            var subscription = client.GetSubscriptionResource(
                new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
            var sizes = new List<Azure.ResourceManager.Compute.Models.VirtualMachineSize>();
            await foreach (var size in subscription.GetVirtualMachineSizesAsync(AzureLocation.EastUS))
            {
                sizes.Add(size);
            }

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

        public static ArmClient CreateMockClient(string endpoint, string subscriptionId)
        {
            var mockUri = new Uri(EnsureTrailingSlash(endpoint));
            var tlsEndpoint = new UriBuilder(mockUri)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = mockUri.Port
            }.Uri;
            var options = new ArmClientOptions
            {
                Environment = new ArmEnvironment(tlsEndpoint, "https://management.azure.com/"),
                Transport = new HttpClientTransport(
                    new HttpClient(new MockEndpointHandler()))
            };
            options.Retry.MaxRetries = 0;

            return new ArmClient(new MockCredential(), subscriptionId, options);
        }

        private static string EnsureTrailingSlash(string endpoint) =>
            endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : endpoint + "/";

        private sealed class MockEndpointHandler : DelegatingHandler
        {
            public MockEndpointHandler()
                : base(new HttpClientHandler())
            {
            }

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

        private sealed class MockCredential : TokenCredential
        {
            private static readonly AccessToken Token = new AccessToken(
                "mock",
                DateTimeOffset.MaxValue);

            public override AccessToken GetToken(
                TokenRequestContext requestContext,
                CancellationToken cancellationToken) => Token;

            public override ValueTask<AccessToken> GetTokenAsync(
                TokenRequestContext requestContext,
                CancellationToken cancellationToken) => new ValueTask<AccessToken>(Token);
        }
    }
}
