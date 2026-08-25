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

namespace ManageVirtualMachine
{
    public class Program
    {
        private const string UserName = "tirekicker";
        private const string Password = "Benchmark!Passw0rd123";
        public static async Task RunSample(ArmClient client, string subscriptionId)
        {
            string s = Guid.NewGuid().ToString("N").Substring(0, 8), rg = $"rgCOMV-{s}", vnetName = $"vnet-{s}", winNicName = $"wnic-{s}", linuxNicName = $"lnic-{s}", winName = $"wvm{s}", linuxName = $"lvm{s}";
            AzureLocation location = new AzureLocation("westcentralus"); ResourceGroupResource group = null;
            try
            {
                var subscription = client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                group = (await subscription.GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rg, new ResourceGroupData(location))).Value;
                var network = (await group.GetVirtualNetworks().CreateOrUpdateAsync(Azure.WaitUntil.Completed, vnetName, new VirtualNetworkData { Location = location, AddressPrefixes = { "10.0.0.0/16" }, Subnets = { new SubnetData { Name = "default", AddressPrefix = "10.0.0.0/28" } } })).Value;
                var nics = group.GetNetworkInterfaces();
                var winNic = (await nics.CreateOrUpdateAsync(Azure.WaitUntil.Completed, winNicName, Nic(location, network.Data.Subnets.Single().Id))).Value;
                var vms = group.GetVirtualMachines();
                var windows = (await vms.CreateOrUpdateAsync(Azure.WaitUntil.Completed, winName, WindowsVm(location, winName, winNic.Id))).Value;
                windows.Data.Tags.Add("who-rocks", "java");
                windows.Data.Tags.Add("where", "on azure");
                windows = (await vms.CreateOrUpdateAsync(Azure.WaitUntil.Completed, winName, windows.Data)).Value;
                windows.Data.StorageProfile.DataDisks.Add(new VirtualMachineDataDisk(2, DiskCreateOptionType.Empty) { DiskSizeGB = 10 });
                windows = (await vms.CreateOrUpdateAsync(Azure.WaitUntil.Completed, winName, windows.Data)).Value;
                windows.Data.StorageProfile.DataDisks.Remove(windows.Data.StorageProfile.DataDisks.First(d => d.Lun == 0));
                windows = (await vms.CreateOrUpdateAsync(Azure.WaitUntil.Completed, winName, windows.Data)).Value;
                await windows.RestartAsync(Azure.WaitUntil.Completed);
                await windows.PowerOffAsync(Azure.WaitUntil.Completed);
                var linuxNic = (await nics.CreateOrUpdateAsync(Azure.WaitUntil.Completed, linuxNicName, Nic(location, network.Data.Subnets.Single().Id))).Value;
                await vms.CreateOrUpdateAsync(Azure.WaitUntil.Completed, linuxName, LinuxVm(location, linuxName, linuxNic.Id));
                await foreach (var vm in vms.GetAllAsync()) _ = vm.Id;
                await windows.DeleteAsync(Azure.WaitUntil.Completed);
            }
            finally { if (group != null) try { await group.DeleteAsync(Azure.WaitUntil.Completed); } catch (Exception ex) { Console.WriteLine(ex); } }
        }
        private static NetworkInterfaceData Nic(AzureLocation l, ResourceIdentifier subnet) => new NetworkInterfaceData { Location = l, IPConfigurations = { new NetworkInterfaceIPConfigurationData { Name = "Primary", Primary = true, PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic, Subnet = new SubnetData { Id = subnet } } } };
        private static VirtualMachineData WindowsVm(AzureLocation l, string name, ResourceIdentifier nic) => new VirtualMachineData(l) { HardwareProfile = new VirtualMachineHardwareProfile { VmSize = VirtualMachineSizeType.StandardD3V2 }, OSProfile = new VirtualMachineOSProfile { ComputerName = name, AdminUsername = UserName, AdminPassword = Password }, NetworkProfile = new VirtualMachineNetworkProfile { NetworkInterfaces = { new VirtualMachineNetworkInterfaceReference { Id = nic, Primary = true } } }, StorageProfile = new VirtualMachineStorageProfile { ImageReference = new ImageReference { Publisher = "MicrosoftWindowsServer", Offer = "WindowsServer", Sku = "2016-Datacenter", Version = "latest" }, DataDisks = { new VirtualMachineDataDisk(0, DiskCreateOptionType.Empty) { DiskSizeGB = 100 }, new VirtualMachineDataDisk(1, DiskCreateOptionType.Empty) { DiskSizeGB = 10 } } } };
        private static VirtualMachineData LinuxVm(AzureLocation l, string name, ResourceIdentifier nic) => new VirtualMachineData(l) { HardwareProfile = new VirtualMachineHardwareProfile { VmSize = VirtualMachineSizeType.StandardD3V2 }, OSProfile = new VirtualMachineOSProfile { ComputerName = name, AdminUsername = UserName, AdminPassword = Password, LinuxConfiguration = new LinuxConfiguration { DisablePasswordAuthentication = false, ProvisionVmAgent = true } }, NetworkProfile = new VirtualMachineNetworkProfile { NetworkInterfaces = { new VirtualMachineNetworkInterfaceReference { Id = nic, Primary = true } } }, StorageProfile = new VirtualMachineStorageProfile { ImageReference = new ImageReference { Publisher = "Canonical", Offer = "UbuntuServer", Sku = "18.04-LTS", Version = "latest" } } };
        public static ArmClient CreateMockClient(string endpoint, string subscription) { var uri = new Uri(EnsureSlash(endpoint)); var tls = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port }.Uri; var o = new ArmClientOptions { Environment = new ArmEnvironment(tls, "https://management.azure.com/"), Transport = new HttpClientTransport(new HttpClient(new MockHandler())) }; o.Retry.MaxRetries = 0; return new ArmClient(new MockCredential(), subscription, o); }
        public static async Task Main(string[] args) { try { string ep = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"), sub = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000"; ArmClient c = string.IsNullOrEmpty(ep) ? new ArmClient(new ClientSecretCredential(Environment.GetEnvironmentVariable("TENANT_ID"), Environment.GetEnvironmentVariable("CLIENT_ID"), Environment.GetEnvironmentVariable("CLIENT_SECRET")), sub) : CreateMockClient(ep, sub); await RunSample(c, sub); } catch (Exception ex) { Console.WriteLine(ex); } }
        private static string EnsureSlash(string v) => v.EndsWith("/", StringComparison.Ordinal) ? v : v + "/";
        private sealed class MockHandler : DelegatingHandler { public MockHandler() : base(new HttpClientHandler()) { } protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken t) { r.RequestUri = new UriBuilder(r.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = r.RequestUri.Port }.Uri; return base.SendAsync(r, t); } }
        private sealed class MockCredential : TokenCredential { private static readonly AccessToken T = new AccessToken("mock", DateTimeOffset.MaxValue); public override AccessToken GetToken(TokenRequestContext c, CancellationToken t) => T; public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext c, CancellationToken t) => new ValueTask<AccessToken>(T); }
    }
}
