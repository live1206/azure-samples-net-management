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

namespace CreateVMSample
{
    public class Program
    {
        private const string AdminUsername = "sampleuser";
        private const string AdminPassword = "Benchmark!Passw0rd123";

        public static async Task RunSample(ArmClient client, string subscriptionId)
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            string resourceGroupName = $"quickstart-{suffix}";
            string vnetName = $"vnet-{suffix}";
            string nicName = $"nic-{suffix}";
            string vmName = $"vm{suffix}";
            AzureLocation location = AzureLocation.WestUS2;
            ResourceGroupResource resourceGroup = null;
            try
            {
                var subscription = client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
                resourceGroup = (await subscription.GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, resourceGroupName, new ResourceGroupData(location))).Value;
                var network = (await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(Azure.WaitUntil.Completed, vnetName,
                    new VirtualNetworkData
                    {
                        Location = location,
                        AddressPrefixes = { "10.0.0.0/16" },
                        Subnets = { new SubnetData { Name = "default", AddressPrefix = "10.0.0.0/28" } }
                    })).Value;
                var nic = (await resourceGroup.GetNetworkInterfaces().CreateOrUpdateAsync(Azure.WaitUntil.Completed, nicName,
                    new NetworkInterfaceData
                    {
                        Location = location,
                        IPConfigurations =
                        {
                            new NetworkInterfaceIPConfigurationData
                            {
                                Name = "Primary", Primary = true,
                                Subnet = new SubnetData { Id = network.Data.Subnets.Single().Id },
                                PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic
                            }
                        }
                    })).Value;
                await resourceGroup.GetVirtualMachines().CreateOrUpdateAsync(Azure.WaitUntil.Completed, vmName,
                    new VirtualMachineData(location)
                    {
                        HardwareProfile = new VirtualMachineHardwareProfile { VmSize = VirtualMachineSizeType.StandardF2 },
                        OSProfile = new VirtualMachineOSProfile { ComputerName = vmName, AdminUsername = AdminUsername, AdminPassword = AdminPassword },
                        NetworkProfile = new VirtualMachineNetworkProfile
                        {
                            NetworkInterfaces = { new VirtualMachineNetworkInterfaceReference { Id = nic.Id, Primary = true } }
                        },
                        StorageProfile = new VirtualMachineStorageProfile
                        {
                            ImageReference = new ImageReference { Publisher = "MicrosoftWindowsServer", Offer = "WindowsServer", Sku = "2016-Datacenter", Version = "latest" }
                        }
                    });
            }
            finally
            {
                if (resourceGroup != null)
                {
                    try { await resourceGroup.DeleteAsync(Azure.WaitUntil.Completed); }
                    catch (Exception ex) { Console.WriteLine(ex); }
                }
            }
        }

        public static ArmClient CreateMockClient(string endpoint, string subscriptionId)
        {
            var uri = new Uri(EnsureSlash(endpoint));
            var tls = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port }.Uri;
            var options = new ArmClientOptions
            {
                Environment = new ArmEnvironment(tls, "https://management.azure.com/"),
                Transport = new HttpClientTransport(new HttpClient(new MockEndpointHandler()))
            };
            options.Retry.MaxRetries = 0;
            return new ArmClient(new MockTokenCredential(), subscriptionId, options);
        }

        public static async Task Main(string[] args)
        {
            try
            {
                string endpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT");
                string subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000";
                ArmClient client = string.IsNullOrEmpty(endpoint)
                    ? new ArmClient(new ClientSecretCredential(Environment.GetEnvironmentVariable("TENANT_ID"), Environment.GetEnvironmentVariable("CLIENT_ID"), Environment.GetEnvironmentVariable("CLIENT_SECRET")), subscription)
                    : CreateMockClient(endpoint, subscription);
                await RunSample(client, subscription);
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private static string EnsureSlash(string value) => value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
        private sealed class MockEndpointHandler : DelegatingHandler
        {
            public MockEndpointHandler() : base(new HttpClientHandler()) { }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = request.RequestUri.Port }.Uri;
                return base.SendAsync(request, token);
            }
        }
        private sealed class MockTokenCredential : TokenCredential
        {
            private static readonly AccessToken Token = new AccessToken("mock-token", DateTimeOffset.MaxValue);
            public override AccessToken GetToken(TokenRequestContext context, CancellationToken token) => Token;
            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken token) => new ValueTask<AccessToken>(Token);
        }
    }
}
