// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;

namespace ManageIPAddress
{
    public class Program
    {
        private const string UserName = "tirekicker";
        private const string Password = "<password>"; // Replace with a password following the policy.

        public static void RunSample(IAzure azure)
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
            Region region = Region.USEast;

            try
            {
                Console.WriteLine("Creating a resource group...");
                azure.ResourceGroups.Define(resourceGroupName)
                    .WithRegion(region)
                    .Create();

                Console.WriteLine("Creating the first public IP address...");
                var publicIPAddress1 = azure.PublicIPAddresses.Define(publicIPAddressName1)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithDynamicIP()
                    .WithLeafDomainLabel(publicIPAddressLeafDNS1)
                    .Create();
                Console.WriteLine($"Created public IP address: {publicIPAddress1.Id}");

                Console.WriteLine("Creating a virtual network...");
                var virtualNetwork = azure.Networks.Define(virtualNetworkName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithAddressSpace("10.0.0.0/16")
                    .WithSubnet(subnetName, "10.0.0.0/28")
                    .Create();
                Console.WriteLine($"Created virtual network: {virtualNetwork.Id}");

                Console.WriteLine("Creating a network interface...");
                var networkInterface = azure.NetworkInterfaces.Define(networkInterfaceName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetwork(virtualNetwork)
                    .WithSubnet(subnetName)
                    .WithPrimaryPrivateIPAddressDynamic()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress1)
                    .Create();
                Console.WriteLine($"Created network interface: {networkInterface.Id}");

                Console.WriteLine("Creating a Windows VM...");
                var vmStartedAt = DateTime.UtcNow;
                var vm = azure.VirtualMachines.Define(vmName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithSpecificWindowsImageVersion(new ImageReference
                    {
                        Publisher = "MicrosoftWindowsServer",
                        Offer = "WindowsServer",
                        Sku = "2016-Datacenter",
                        Version = "latest"
                    })
                    .WithAdminUsername(UserName)
                    .WithAdminPassword(Password)
                    .WithSize(VirtualMachineSizeTypes.Parse("Standard_D3_v2"))
                    .Create();
                var vmFinishedAt = DateTime.UtcNow;
                Console.WriteLine($"Created VM: (took {(vmFinishedAt - vmStartedAt).TotalSeconds} seconds) {vm.Id}");

                networkInterface.Refresh();
                var associatedIPAddress = networkInterface.PrimaryIPConfiguration.GetPublicIPAddress();
                Console.WriteLine($"Public IP address associated with the VM after create: {associatedIPAddress.Id}");

                Console.WriteLine("Creating the second public IP address...");
                var publicIPAddress2 = azure.PublicIPAddresses.Define(publicIPAddressName2)
                    .WithRegion(region)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithDynamicIP()
                    .WithLeafDomainLabel(publicIPAddressLeafDNS2)
                    .Create();
                Console.WriteLine($"Created public IP address: {publicIPAddress2.Id}");

                Console.WriteLine("Updating the VM's primary NIC with the second public IP address...");
                networkInterface = networkInterface.Update()
                    .WithExistingPrimaryPublicIPAddress(publicIPAddress2)
                    .Apply();

                networkInterface.Refresh();
                associatedIPAddress = networkInterface.PrimaryIPConfiguration.GetPublicIPAddress();
                Console.WriteLine($"Public IP address associated with the VM after update: {associatedIPAddress.Id}");

                Console.WriteLine("Removing the public IP address associated with the VM...");
                networkInterface = networkInterface.Update()
                    .WithoutPrimaryPublicIPAddress()
                    .Apply();
                Console.WriteLine("Removed the public IP address associated with the VM.");

                Console.WriteLine("Deleting both public IP addresses...");
                azure.PublicIPAddresses.DeleteById(publicIPAddress1.Id);
                azure.PublicIPAddresses.DeleteById(publicIPAddress2.Id);
                Console.WriteLine("Deleted both public IP addresses.");
            }
            finally
            {
                try
                {
                    Console.WriteLine($"Deleting resource group: {resourceGroupName}");
                    azure.ResourceGroups.DeleteByName(resourceGroupName);
                    Console.WriteLine($"Deleted resource group: {resourceGroupName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not delete resource group {resourceGroupName}: {ex}");
                }
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                    Environment.GetEnvironmentVariable("CLIENT_ID"),
                    Environment.GetEnvironmentVariable("CLIENT_SECRET"),
                    Environment.GetEnvironmentVariable("TENANT_ID"),
                    AzureEnvironment.AzureGlobalCloud);

                var azure = Azure.Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.None)
                    .Authenticate(credentials)
                    .WithSubscription(Environment.GetEnvironmentVariable("SUBSCRIPTION_ID"));

                Console.WriteLine($"Selected subscription: {azure.SubscriptionId}");
                RunSample(azure);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
