// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ManageVirtualNetwork
{
    public class Program
    {
        private const string UserName = "tirekicker";
        private const string SshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQC+wWK73dCr+jgQOAxNsHAnNNNMEMWOHYEccp6wJm2gotpr9katuF/ZAdou5AaW1C61slRkHRkpRRX9FA9CYBiitZgvCCz+3nWNN7l/Up54Zps/pHWGZLHNJZRYyAB6j5yVLMVHIHriY49d/GZTZVNB8GoJv9Gakwc/fuEZYYl4YDFiGMBP///TzlI4jhiJzjKnEvqPFki5p2ZRJqcbCiF4pJrxUQR/RXqVFQdbRLZgYfJ8xGB878RENq3yQ39d8dVOkq4edbkzwcUmwwwkYVPIoDGsYLaRHnG+To7FvMeyO7xDVQkMKzopTQV8AuKpyvpqu0a9pWOMaiCyDytO7GGN you@me.com";

        public static async Task RunSample(ArmClient client, string subscriptionId)
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
            AzureLocation location = AzureLocation.EastUS;
            ResourceGroupResource resourceGroup = null;

            try
            {
                var subscription = client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                resourceGroup = (await subscription.GetResourceGroups().CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, resourceGroupName, new ResourceGroupData(location))).Value;

                var nsgs = resourceGroup.GetNetworkSecurityGroups();
                var backEndNsg = (await nsgs.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, backEndNsgName, CreateBackEndNsg(location))).Value;
                var frontEndNsg = (await nsgs.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, frontEndNsgName, CreateFrontEndNsg(location))).Value;

                var networks = resourceGroup.GetVirtualNetworks();
                var virtualNetwork1 = (await networks.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, vnetName1,
                    CreateVirtualNetworkData(location, frontEndSubnetName, backEndSubnetName, null, backEndNsg.Id))).Value;

                virtualNetwork1 = (await networks.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, vnetName1,
                    CreateVirtualNetworkData(location, frontEndSubnetName, backEndSubnetName, frontEndNsg.Id, backEndNsg.Id))).Value;

                var publicIPAddress = (await resourceGroup.GetPublicIPAddresses().CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, $"{frontEndVmName}-ip",
                    new PublicIPAddressData
                    {
                        Location = location,
                        PublicIPAddressVersion = NetworkIPVersion.IPv4,
                        PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic
                    })).Value;

                ResourceIdentifier frontEndSubnetId = virtualNetwork1.Data.Subnets.Single(s => s.Name == frontEndSubnetName).Id;
                ResourceIdentifier backEndSubnetId = virtualNetwork1.Data.Subnets.Single(s => s.Name == backEndSubnetName).Id;
                var networkInterfaces = resourceGroup.GetNetworkInterfaces();
                var frontEndNic = (await networkInterfaces.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, $"{frontEndVmName}-nic",
                    CreateNetworkInterfaceData(location, frontEndSubnetId, publicIPAddress.Id))).Value;
                var backEndNic = (await networkInterfaces.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, $"{backEndVmName}-nic",
                    CreateNetworkInterfaceData(location, backEndSubnetId, null))).Value;

                var virtualMachines = resourceGroup.GetVirtualMachines();
                await virtualMachines.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, frontEndVmName,
                    CreateVirtualMachineData(location, frontEndVmName, frontEndNic.Id));
                await virtualMachines.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, backEndVmName,
                    CreateVirtualMachineData(location, backEndVmName, backEndNic.Id));

                var virtualNetwork2 = (await networks.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed, vnetName2,
                    new VirtualNetworkData { Location = location, AddressPrefixes = { "10.0.0.0/16" } })).Value;

                await foreach (var virtualNetwork in networks.GetAllAsync())
                {
                    _ = virtualNetwork.Id;
                }

                await virtualNetwork2.DeleteAsync(Azure.WaitUntil.Completed);
            }
            finally
            {
                if (resourceGroup != null)
                {
                    try
                    {
                        await resourceGroup.DeleteAsync(Azure.WaitUntil.Completed);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        private static NetworkSecurityGroupData CreateBackEndNsg(AzureLocation location) => new NetworkSecurityGroupData
        {
            Location = location,
            SecurityRules =
            {
                CreateSecurityRule("DenyInternetInComing", 700, SecurityRuleAccess.Deny, SecurityRuleDirection.Inbound, "Internet", "*", "*"),
                CreateSecurityRule("DenyInternetOutGoing", 701, SecurityRuleAccess.Deny, SecurityRuleDirection.Outbound, "*", "Internet", "*")
            }
        };

        private static NetworkSecurityGroupData CreateFrontEndNsg(AzureLocation location) => new NetworkSecurityGroupData
        {
            Location = location,
            SecurityRules =
            {
                CreateSecurityRule("AllowHttpInComing", 700, SecurityRuleAccess.Allow, SecurityRuleDirection.Inbound, "Internet", "*", "80", SecurityRuleProtocol.Tcp),
                CreateSecurityRule("DenyInternetOutGoing", 701, SecurityRuleAccess.Deny, SecurityRuleDirection.Outbound, "*", "Internet", "*")
            }
        };

        private static SecurityRuleData CreateSecurityRule(string name, int priority, SecurityRuleAccess access,
            SecurityRuleDirection direction, string source, string destination, string destinationPort,
            SecurityRuleProtocol? protocol = null) => new SecurityRuleData
        {
            Name = name,
            Priority = priority,
            Access = access,
            Direction = direction,
            SourceAddressPrefix = source,
            SourcePortRange = "*",
            DestinationAddressPrefix = destination,
            DestinationPortRange = destinationPort,
            Protocol = protocol ?? SecurityRuleProtocol.Asterisk
        };

        private static VirtualNetworkData CreateVirtualNetworkData(AzureLocation location, string frontEndSubnetName,
            string backEndSubnetName, ResourceIdentifier frontEndNsgId, ResourceIdentifier backEndNsgId)
        {
            var frontEndSubnet = new SubnetData { Name = frontEndSubnetName, AddressPrefix = "192.168.1.0/24" };
            if (frontEndNsgId != null) frontEndSubnet.NetworkSecurityGroup = new NetworkSecurityGroupData { Id = frontEndNsgId };
            return new VirtualNetworkData
            {
                Location = location,
                AddressPrefixes = { "192.168.0.0/16" },
                Subnets =
                {
                    frontEndSubnet,
                    new SubnetData
                    {
                        Name = backEndSubnetName,
                        AddressPrefix = "192.168.2.0/24",
                        NetworkSecurityGroup = new NetworkSecurityGroupData { Id = backEndNsgId }
                    }
                }
            };
        }

        private static NetworkInterfaceData CreateNetworkInterfaceData(AzureLocation location,
            ResourceIdentifier subnetId, ResourceIdentifier publicIPAddressId)
        {
            var configuration = new NetworkInterfaceIPConfigurationData
            {
                Name = "Primary",
                Primary = true,
                Subnet = new SubnetData { Id = subnetId },
                PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic
            };
            if (publicIPAddressId != null) configuration.PublicIPAddress = new PublicIPAddressData { Id = publicIPAddressId };
            return new NetworkInterfaceData { Location = location, IPConfigurations = { configuration } };
        }

        private static VirtualMachineData CreateVirtualMachineData(AzureLocation location, string vmName, ResourceIdentifier nicId) =>
            new VirtualMachineData(location)
            {
                NetworkProfile = new VirtualMachineNetworkProfile
                {
                    NetworkInterfaces = { new VirtualMachineNetworkInterfaceReference { Id = nicId, Primary = true } }
                },
                OSProfile = new VirtualMachineOSProfile
                {
                    ComputerName = vmName,
                    AdminUsername = UserName,
                    LinuxConfiguration = new LinuxConfiguration
                    {
                        DisablePasswordAuthentication = true,
                        SshPublicKeys = { new SshPublicKeyConfiguration { Path = $"/home/{UserName}/.ssh/authorized_keys", KeyData = SshKey } }
                    }
                },
                StorageProfile = new VirtualMachineStorageProfile
                {
                    ImageReference = new ImageReference
                    {
                        Publisher = "Canonical", Offer = "UbuntuServer", Sku = "16.04-LTS", Version = "latest"
                    }
                },
                HardwareProfile = new VirtualMachineHardwareProfile { VmSize = VirtualMachineSizeType.StandardD3V2 }
            };

        public static ArmClient CreateMockClient(string mockEndpoint, string subscriptionId)
        {
            var mockUri = new Uri(EnsureTrailingSlash(mockEndpoint));
            var tlsEndpoint = new UriBuilder(mockUri) { Scheme = Uri.UriSchemeHttps, Port = mockUri.Port }.Uri;
            var options = new ArmClientOptions
            {
                Environment = new ArmEnvironment(tlsEndpoint, "https://management.azure.com/"),
                Transport = new HttpClientTransport(new HttpClient(new MockEndpointHandler()))
            };
            options.Retry.MaxRetries = 0;
            return new ArmClient(new MockTokenCredential(), subscriptionId, options);
        }

        public static async Task Main(string[] args)
        {
            try
            {
                string subscriptionId = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000";
                string mockEndpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT");
                ArmClient client = string.IsNullOrEmpty(mockEndpoint)
                    ? new ArmClient(new ClientSecretCredential(
                        Environment.GetEnvironmentVariable("TENANT_ID"),
                        Environment.GetEnvironmentVariable("CLIENT_ID"),
                        Environment.GetEnvironmentVariable("CLIENT_SECRET")), subscriptionId)
                    : CreateMockClient(mockEndpoint, subscriptionId);
                await RunSample(client, subscriptionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string EnsureTrailingSlash(string endpoint) => endpoint.EndsWith("/", StringComparison.Ordinal) ? endpoint : endpoint + "/";

        private sealed class MockEndpointHandler : DelegatingHandler
        {
            public MockEndpointHandler()
                : base(new HttpClientHandler())
            {
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = request.RequestUri.Port }.Uri;
                return base.SendAsync(request, cancellationToken);
            }
        }

        private sealed class MockTokenCredential : TokenCredential
        {
            private static readonly AccessToken Token = new AccessToken("mock-token", DateTimeOffset.MaxValue);
            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => Token;
            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => new ValueTask<AccessToken>(Token);
        }
    }
}
