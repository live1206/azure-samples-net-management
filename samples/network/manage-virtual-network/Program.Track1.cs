// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ManageVirtualNetwork
{
    public class Program
    {
        private const string UserName = "tirekicker";
        private const string SshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC+wWK73dCr+jgQOAxNsHAnNNNMEMWOHYEccp6wJm2gotpr9katuF/ZAdou5AaW1C61slRkHRkpRRX9FA9CYBiitZgvCCz+3nWNN7l/Up54Zps/pHWGZLHNJZRYyAB6j5yVLMVHIHriY49d/GZTZVNB8GoJv9Gakwc/fuEZYYl4YDFiGMBP///TzlI4jhiJzjKnEvqPFki5p2ZRJqcbCiF4pJrxUQR/RXqVFQdbRLZgYfJ8xGB878RENq3yQ39d8dVOkq4edbkzwcUmwwwkYVPIoDGsYLaRHnG+To7FvMeyO7xDVQkMKzopTQV8AuKpyvpqu0a9pWOMaiCyDytO7GGN you@me.com";

        public static void RunSample(IAzure azure)
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            string resourceGroupName = $"rgNEMV-{suffix}";
            string vnetName1 = $"vnet1-{suffix}";
            string vnetName2 = $"vnet2-{suffix}";
            string frontEndVmName = $"fevm{suffix}";
            string backEndVmName = $"bevm{suffix}";
            string frontEndSubnetName = "frontend";
            string backEndSubnetName = "backend";
            string frontEndNsgName = $"frontendnsg-{suffix}";
            string backEndNsgName = $"backendnsg-{suffix}";
            Region region = Region.USEast;

            try
            {
                azure.ResourceGroups.Define(resourceGroupName).WithRegion(region).Create();

                var backEndNsg = azure.NetworkSecurityGroups.Define(backEndNsgName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .DefineRule("DenyInternetInComing")
                        .DenyInbound().FromAddress("Internet").FromAnyPort()
                        .ToAnyAddress().ToAnyPort().WithAnyProtocol().Attach()
                    .DefineRule("DenyInternetOutGoing")
                        .DenyOutbound().FromAnyAddress().FromAnyPort()
                        .ToAddress("Internet").ToAnyPort().WithAnyProtocol().Attach()
                    .Create();

                var frontEndNsg = azure.NetworkSecurityGroups.Define(frontEndNsgName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .DefineRule("AllowHttpInComing")
                        .AllowInbound().FromAddress("Internet").FromAnyPort()
                        .ToAnyAddress().ToPort(80).WithProtocol(SecurityRuleProtocol.Tcp).Attach()
                    .DefineRule("DenyInternetOutGoing")
                        .DenyOutbound().FromAnyAddress().FromAnyPort()
                        .ToAddress("Internet").ToAnyPort().WithAnyProtocol().Attach()
                    .Create();

                var virtualNetwork1 = azure.Networks.Define(vnetName1)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithAddressSpace("192.168.0.0/16")
                    .WithSubnet(frontEndSubnetName, "192.168.1.0/24")
                    .DefineSubnet(backEndSubnetName)
                        .WithAddressPrefix("192.168.2.0/24")
                        .WithExistingNetworkSecurityGroup(backEndNsg)
                        .Attach()
                    .Create();

                virtualNetwork1 = virtualNetwork1.Update()
                    .UpdateSubnet(frontEndSubnetName)
                        .WithExistingNetworkSecurityGroup(frontEndNsg)
                        .Parent()
                    .Apply();

                var publicIPAddress = azure.PublicIPAddresses.Define($"{frontEndVmName}-ip")
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithDynamicIP()
                    .Create();

                var frontEndNic = azure.NetworkInterfaces.Define($"{frontEndVmName}-nic")
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetwork(virtualNetwork1)
                    .WithSubnet(frontEndSubnetName)
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                    .Create();

                var backEndNic = azure.NetworkInterfaces.Define($"{backEndVmName}-nic")
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetwork(virtualNetwork1)
                    .WithSubnet(backEndSubnetName)
                    .WithPrimaryPrivateIPAddressDynamic()
                    .Create();

                var image = new ImageReference
                {
                    Publisher = "Canonical",
                    Offer = "UbuntuServer",
                    Sku = "16.04-LTS",
                    Version = "latest"
                };

                azure.VirtualMachines.Define(frontEndVmName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetworkInterface(frontEndNic)
                    .WithSpecificLinuxImageVersion(image)
                    .WithRootUsername(UserName)
                    .WithSsh(SshKey)
                    .WithSize(VirtualMachineSizeTypes.Parse("Standard_D3_v2"))
                    .Create();

                azure.VirtualMachines.Define(backEndVmName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetworkInterface(backEndNic)
                    .WithSpecificLinuxImageVersion(image)
                    .WithRootUsername(UserName)
                    .WithSsh(SshKey)
                    .WithSize(VirtualMachineSizeTypes.Parse("Standard_D3_v2"))
                    .Create();

                var virtualNetwork2 = azure.Networks.Define(vnetName2)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .Create();

                foreach (var virtualNetwork in azure.Networks.ListByResourceGroup(resourceGroupName))
                {
                    _ = virtualNetwork.Id;
                }

                azure.Networks.DeleteById(virtualNetwork2.Id);
            }
            finally
            {
                try
                {
                    azure.ResourceGroups.DeleteByName(resourceGroupName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public static IAzure CreateMockClient(string mockEndpoint, string subscriptionId)
        {
            var mockUri = new Uri(EnsureTrailingSlash(mockEndpoint));
            string tlsEndpoint = new UriBuilder(mockUri) { Scheme = Uri.UriSchemeHttps, Port = mockUri.Port }.Uri.AbsoluteUri;
            var environment = new AzureEnvironment
            {
                Name = "Mock",
                AuthenticationEndpoint = tlsEndpoint,
                ResourceManagerEndpoint = tlsEndpoint,
                GraphEndpoint = tlsEndpoint,
                ManagementEndpoint = tlsEndpoint,
                StorageEndpointSuffix = "mock.local",
                KeyVaultSuffix = "mock.local"
            };
            var credentials = new AzureCredentials(
                new MockServiceClientCredentials(), new MockServiceClientCredentials(),
                "mock-tenant", environment).WithDefaultSubscription(subscriptionId);
            var restClient = RestClient.Configure()
                .WithEnvironment(environment)
                .WithCredentials(credentials)
                .WithDelegatingHandler(new MockEndpointHandler())
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.None)
                .Build();
            return Azure.Authenticate(restClient, "mock-tenant").WithSubscription(subscriptionId);
        }

        public static void Main(string[] args)
        {
            try
            {
                string mockEndpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT");
                IAzure azure;
                if (string.IsNullOrEmpty(mockEndpoint))
                {
                    var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                        Environment.GetEnvironmentVariable("CLIENT_ID"),
                        Environment.GetEnvironmentVariable("CLIENT_SECRET"),
                        Environment.GetEnvironmentVariable("TENANT_ID"),
                        AzureEnvironment.AzureGlobalCloud);
                    azure = Azure.Configure().WithLogLevel(HttpLoggingDelegatingHandler.Level.None)
                        .Authenticate(credentials)
                        .WithSubscription(Environment.GetEnvironmentVariable("SUBSCRIPTION_ID"));
                }
                else
                {
                    azure = CreateMockClient(mockEndpoint,
                        Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000");
                }
                RunSample(azure);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string EnsureTrailingSlash(string endpoint) => endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : endpoint + "/";

        private sealed class MockEndpointHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = request.RequestUri.Port }.Uri;
                return base.SendAsync(request, cancellationToken);
            }
        }

        private sealed class MockServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
