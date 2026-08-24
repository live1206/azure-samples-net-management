// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ManageIPAddress
{
    public class Program
    {
        private const string UserName = "tirekicker";
        private const string Password = "<password>"; // Replace with a password following the policy.

        public static async Task RunSample(ArmClient client)
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            string publicIPAddressName1 = $"pip1-{suffix}";
            string publicIPAddressName2 = $"pip2-{suffix}";
            string publicIPAddressLeafDNS1 = $"pipdns1-{suffix}";
            string publicIPAddressLeafDNS2 = $"pipdns2-{suffix}";
            string virtualNetworkName = $"vnet-{suffix}";
            string subnetName = "mySubnet";
            string networkInterfaceName = $"nic-{suffix}";
            string vmName = $"vm{suffix}";
            string resourceGroupName = $"rgNEMP-{suffix}";
            AzureLocation location = AzureLocation.EastUS;
            ResourceGroupResource resourceGroup = null;

            try
            {
                Console.WriteLine("Creating a resource group...");
                resourceGroup = (await client.GetDefaultSubscription()
                    .GetResourceGroups()
                    .CreateOrUpdateAsync(
                        Azure.WaitUntil.Completed,
                        resourceGroupName,
                        new ResourceGroupData(location))).Value;

                var publicIPAddressContainer = resourceGroup.GetPublicIPAddresses();
                Console.WriteLine("Creating the first public IP address...");
                var publicIPAddress1 = (await publicIPAddressContainer.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    publicIPAddressName1,
                    CreatePublicIPAddressData(location, publicIPAddressLeafDNS1))).Value;
                Console.WriteLine($"Created public IP address: {publicIPAddress1.Id}");

                Console.WriteLine("Creating a virtual network...");
                var virtualNetworkData = new VirtualNetworkData
                {
                    Location = location,
                    AddressPrefixes = { "10.0.0.0/16" },
                    Subnets =
                    {
                        new SubnetData
                        {
                            Name = subnetName,
                            AddressPrefix = "10.0.0.0/28"
                        }
                    }
                };
                var virtualNetwork = (await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    virtualNetworkName,
                    virtualNetworkData)).Value;
                Console.WriteLine($"Created virtual network: {virtualNetwork.Id}");

                Console.WriteLine("Creating a network interface...");
                var networkInterfaceContainer = resourceGroup.GetNetworkInterfaces();
                var networkInterface = (await networkInterfaceContainer.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    networkInterfaceName,
                    CreateNetworkInterfaceData(location, virtualNetwork.Data.Subnets.First().Id, publicIPAddress1.Id))).Value;
                Console.WriteLine($"Created network interface: {networkInterface.Id}");

                Console.WriteLine("Creating a Windows VM...");
                var vmStartedAt = DateTime.UtcNow;
                var vmData = new VirtualMachineData(location)
                {
                    NetworkProfile = new VirtualMachineNetworkProfile
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference
                            {
                                Id = networkInterface.Id,
                                Primary = true
                            }
                        }
                    },
                    OSProfile = new VirtualMachineOSProfile
                    {
                        ComputerName = vmName,
                        AdminUsername = UserName,
                        AdminPassword = Password
                    },
                    StorageProfile = new VirtualMachineStorageProfile
                    {
                        ImageReference = new ImageReference
                        {
                            Offer = "WindowsServer",
                            Publisher = "MicrosoftWindowsServer",
                            Sku = "2016-Datacenter",
                            Version = "latest"
                        }
                    },
                    HardwareProfile = new VirtualMachineHardwareProfile
                    {
                        VmSize = VirtualMachineSizeType.StandardD3V2
                    }
                };
                var vm = (await resourceGroup.GetVirtualMachines().CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    vmName,
                    vmData)).Value;
                var vmFinishedAt = DateTime.UtcNow;
                Console.WriteLine($"Created VM: (took {(vmFinishedAt - vmStartedAt).TotalSeconds} seconds) {vm.Id}");

                networkInterface = (await networkInterface.GetAsync()).Value;
                var associatedIPAddress = await GetPrimaryPublicIPAddressAsync(client, networkInterface);
                Console.WriteLine($"Public IP address associated with the VM after create: {associatedIPAddress.Id}");

                Console.WriteLine("Creating the second public IP address...");
                var publicIPAddress2 = (await publicIPAddressContainer.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    publicIPAddressName2,
                    CreatePublicIPAddressData(location, publicIPAddressLeafDNS2))).Value;
                Console.WriteLine($"Created public IP address: {publicIPAddress2.Id}");

                Console.WriteLine("Updating the VM's primary NIC with the second public IP address...");
                networkInterface = (await networkInterfaceContainer.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    networkInterfaceName,
                    CreateNetworkInterfaceData(location, virtualNetwork.Data.Subnets.First().Id, publicIPAddress2.Id))).Value;

                networkInterface = (await networkInterface.GetAsync()).Value;
                associatedIPAddress = await GetPrimaryPublicIPAddressAsync(client, networkInterface);
                Console.WriteLine($"Public IP address associated with the VM after update: {associatedIPAddress.Id}");

                Console.WriteLine("Removing the public IP address associated with the VM...");
                networkInterface = (await networkInterfaceContainer.CreateOrUpdateAsync(
                    Azure.WaitUntil.Completed,
                    networkInterfaceName,
                    CreateNetworkInterfaceData(location, virtualNetwork.Data.Subnets.First().Id, null))).Value;
                Console.WriteLine("Removed the public IP address associated with the VM.");

                Console.WriteLine("Deleting both public IP addresses...");
                await publicIPAddress1.DeleteAsync(Azure.WaitUntil.Completed);
                await publicIPAddress2.DeleteAsync(Azure.WaitUntil.Completed);
                Console.WriteLine("Deleted both public IP addresses.");
            }
            finally
            {
                if (resourceGroup != null)
                {
                    try
                    {
                        Console.WriteLine($"Deleting resource group: {resourceGroupName}");
                        await resourceGroup.DeleteAsync(Azure.WaitUntil.Completed);
                        Console.WriteLine($"Deleted resource group: {resourceGroupName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not delete resource group {resourceGroupName}: {ex}");
                    }
                }
            }
        }

        private static PublicIPAddressData CreatePublicIPAddressData(AzureLocation location, string domainNameLabel)
        {
            return new PublicIPAddressData
            {
                PublicIPAddressVersion = NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                Location = location,
                DnsSettings = new PublicIPAddressDnsSettings
                {
                    DomainNameLabel = domainNameLabel
                }
            };
        }

        private static NetworkInterfaceData CreateNetworkInterfaceData(
            AzureLocation location,
            ResourceIdentifier subnetId,
            ResourceIdentifier publicIPAddressId)
        {
            var ipConfiguration = new NetworkInterfaceIPConfigurationData
            {
                Name = "Primary",
                Primary = true,
                Subnet = new SubnetData { Id = subnetId },
                PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic
            };

            if (publicIPAddressId != null)
            {
                ipConfiguration.PublicIPAddress = new PublicIPAddressData { Id = publicIPAddressId };
            }

            return new NetworkInterfaceData
            {
                Location = location,
                IPConfigurations = { ipConfiguration }
            };
        }

        private static async Task<PublicIPAddressResource> GetPrimaryPublicIPAddressAsync(
            ArmClient client,
            NetworkInterfaceResource networkInterface)
        {
            ResourceIdentifier publicIPAddressId = networkInterface.Data.IPConfigurations
                .Single(configuration => configuration.Primary == true)
                .PublicIPAddress.Id;

            return (await client.GetPublicIPAddressResource(publicIPAddressId).GetAsync()).Value;
        }

        public static async Task Main(string[] args)
        {
            try
            {
                var credential = new ClientSecretCredential(
                    Environment.GetEnvironmentVariable("TENANT_ID"),
                    Environment.GetEnvironmentVariable("CLIENT_ID"),
                    Environment.GetEnvironmentVariable("CLIENT_SECRET"));
                var client = new ArmClient(
                    credential,
                    Environment.GetEnvironmentVariable("SUBSCRIPTION_ID"));

                await RunSample(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
