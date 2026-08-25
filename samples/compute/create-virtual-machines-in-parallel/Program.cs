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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CreateVirtualMachinesInParallel
{
    public class Program
    {
        private const string Username = "tirekicker";
        private const string Password = "Benchmark!Passw0rd123";
        public static async Task RunSample(ArmClient client, string subscriptionId)
        {
            string s = Guid.NewGuid().ToString("N").Substring(0, 8), rg = $"rgCOP-{s}"; ResourceGroupResource group = null;
            try
            {
                var subscription = client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                group = (await subscription.GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rg, new ResourceGroupData(AzureLocation.WestUS2))).Value;
                var east = CreateNetworkAsync(group, "eastus", "east", s);
                var south = CreateNetworkAsync(group, "southcentralus", "south", s);
                var networks = await Task.WhenAll(east, south);
                var tasks = new List<Task>();
                tasks.AddRange(CreateRegionTasks(group, networks[0], "eastus", "east", s));
                tasks.AddRange(CreateRegionTasks(group, networks[1], "southcentralus", "south", s));
                await Task.WhenAll(tasks);
            }
            finally { if (group != null) try { await group.DeleteAsync(Azure.WaitUntil.Completed); } catch (Exception ex) { Console.WriteLine(ex); } }
        }
        private static async Task<Azure.ResourceManager.Network.VirtualNetworkResource> CreateNetworkAsync(ResourceGroupResource group, string region, string prefix, string suffix) =>
            (await group.GetVirtualNetworks().CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"vnet-{prefix}-{suffix}", new VirtualNetworkData { Location = region, AddressPrefixes = { "172.16.0.0/16" }, Subnets = { new SubnetData { Name = "default", AddressPrefix = "172.16.0.0/24" } } })).Value;
        private static IEnumerable<Task> CreateRegionTasks(ResourceGroupResource group, Azure.ResourceManager.Network.VirtualNetworkResource network, string region, string prefix, string suffix)
        {
            for (int i = 1; i <= 5; i++) yield return CreateVmAsync(group, network, region, prefix, suffix, i);
        }
        private static async Task CreateVmAsync(ResourceGroupResource group, Azure.ResourceManager.Network.VirtualNetworkResource network, string region, string prefix, string suffix, int index)
        {
            string name = $"vm-{prefix}-{index}-{suffix}";
            var pip = (await group.GetPublicIPAddresses().CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"pip-{prefix}-{index}-{suffix}", new PublicIPAddressData { Location = region, PublicIPAddressVersion = NetworkIPVersion.IPv4, PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic, DnsSettings = new PublicIPAddressDnsSettings { DomainNameLabel = $"pip-{prefix}-{index}-{suffix}" } })).Value;
            var nic = (await group.GetNetworkInterfaces().CreateOrUpdateAsync(Azure.WaitUntil.Completed, $"nic-{prefix}-{index}-{suffix}", new NetworkInterfaceData { Location = region, IPConfigurations = { new NetworkInterfaceIPConfigurationData { Name = "Primary", Primary = true, PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic, Subnet = new SubnetData { Id = network.Data.Subnets.Single().Id }, PublicIPAddress = new PublicIPAddressData { Id = pip.Id } } } })).Value;
            await group.GetVirtualMachines().CreateOrUpdateAsync(Azure.WaitUntil.Completed, name, new VirtualMachineData(region) { HardwareProfile = new VirtualMachineHardwareProfile { VmSize = "Standard_D2a_v4" }, OSProfile = new VirtualMachineOSProfile { ComputerName = name, AdminUsername = Username, AdminPassword = Password, LinuxConfiguration = new LinuxConfiguration { DisablePasswordAuthentication = false } }, NetworkProfile = new VirtualMachineNetworkProfile { NetworkInterfaces = { new VirtualMachineNetworkInterfaceReference { Id = nic.Id, Primary = true } } }, StorageProfile = new VirtualMachineStorageProfile { ImageReference = new ImageReference { Publisher = "Canonical", Offer = "UbuntuServer", Sku = "16.04-LTS", Version = "latest" } } });
        }
        public static ArmClient CreateMockClient(string endpoint, string subscription) { var uri = new Uri(Ensure(endpoint)); var tls = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port }.Uri; var o = new ArmClientOptions { Environment = new ArmEnvironment(tls, "https://management.azure.com/"), Transport = new HttpClientTransport(new HttpClient(new MockHandler())) }; o.Retry.MaxRetries = 0; return new ArmClient(new MockCredential(), subscription, o); }
        public static async Task Main(string[] args) { try { string e = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"), s = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000"; var c = string.IsNullOrEmpty(e) ? new ArmClient(new ClientSecretCredential(Environment.GetEnvironmentVariable("TENANT_ID"), Environment.GetEnvironmentVariable("CLIENT_ID"), Environment.GetEnvironmentVariable("CLIENT_SECRET")), s) : CreateMockClient(e, s); await RunSample(c, s); } catch (Exception ex) { Console.WriteLine(ex); } }
        private static string Ensure(string v) => v.EndsWith("/", StringComparison.Ordinal) ? v : v + "/";
        private sealed class MockHandler : DelegatingHandler { public MockHandler() : base(new HttpClientHandler()) { } protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken t) { r.RequestUri = new UriBuilder(r.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = r.RequestUri.Port }.Uri; return base.SendAsync(r, t); } }
        private sealed class MockCredential : TokenCredential { static readonly AccessToken T = new AccessToken("mock", DateTimeOffset.MaxValue); public override AccessToken GetToken(TokenRequestContext c, CancellationToken t) => T; public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext c, CancellationToken t) => new ValueTask<AccessToken>(T); }
    }
}
