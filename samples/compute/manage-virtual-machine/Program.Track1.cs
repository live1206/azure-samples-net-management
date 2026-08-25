using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ManageVirtualMachine
{
    public class Program
    {
        private const string UserName = "tirekicker";
        private const string Password = "Benchmark!Passw0rd123";
        public static void RunSample(IAzure azure)
        {
            string s = Guid.NewGuid().ToString("N").Substring(0, 8), rg = $"rgCOMV-{s}", vnetName = $"vnet-{s}", winNicName = $"wnic-{s}", linuxNicName = $"lnic-{s}", winName = $"wvm{s}", linuxName = $"lvm{s}";
            Region region = Region.USWestCentral;
            try
            {
                azure.ResourceGroups.Define(rg).WithRegion(region).Create();
                var network = azure.Networks.Define(vnetName).WithRegion(region).WithExistingResourceGroup(rg).WithAddressSpace("10.0.0.0/16").WithSubnet("default", "10.0.0.0/28").Create();
                var winNic = azure.NetworkInterfaces.Define(winNicName).WithRegion(region).WithExistingResourceGroup(rg).WithExistingPrimaryNetwork(network).WithSubnet("default").WithPrimaryPrivateIPAddressDynamic().Create();
                var windows = azure.VirtualMachines.Define(winName).WithRegion(region).WithExistingResourceGroup(rg).WithExistingPrimaryNetworkInterface(winNic)
                    .WithSpecificWindowsImageVersion(new ImageReference { Publisher = "MicrosoftWindowsServer", Offer = "WindowsServer", Sku = "2016-Datacenter", Version = "latest" })
                    .WithAdminUsername(UserName).WithAdminPassword(Password).WithNewDataDisk(100).WithNewDataDisk(10).WithSize(VirtualMachineSizeTypes.Parse("Standard_D3_v2")).Create();
                windows = windows.Update().WithTag("who-rocks", "java").WithTag("where", "on azure").Apply();
                windows = windows.Update().WithNewDataDisk(10).Apply();
                windows = windows.Update().WithoutDataDisk(0).Apply();
                windows.Restart();
                windows.PowerOff();
                var linuxNic = azure.NetworkInterfaces.Define(linuxNicName).WithRegion(region).WithExistingResourceGroup(rg).WithExistingPrimaryNetwork(network).WithSubnet("default").WithPrimaryPrivateIPAddressDynamic().Create();
                azure.VirtualMachines.Define(linuxName).WithRegion(region).WithExistingResourceGroup(rg).WithExistingPrimaryNetworkInterface(linuxNic)
                    .WithSpecificLinuxImageVersion(new ImageReference { Publisher = "Canonical", Offer = "UbuntuServer", Sku = "18.04-LTS", Version = "latest" })
                    .WithRootUsername(UserName).WithRootPassword(Password).WithSize(VirtualMachineSizeTypes.Parse("Standard_D3_v2")).Create();
                foreach (var vm in azure.VirtualMachines.ListByResourceGroup(rg)) _ = vm.Id;
                azure.VirtualMachines.DeleteById(windows.Id);
            }
            finally { try { azure.ResourceGroups.DeleteByName(rg); } catch (Exception ex) { Console.WriteLine(ex); } }
        }
        public static IAzure CreateMockClient(string endpoint, string subscription)
        {
            var uri = new Uri(EnsureSlash(endpoint)); string tls = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port }.Uri.AbsoluteUri;
            var env = new AzureEnvironment { Name = "Mock", AuthenticationEndpoint = tls, ResourceManagerEndpoint = tls, GraphEndpoint = tls, ManagementEndpoint = tls, StorageEndpointSuffix = "mock.local", KeyVaultSuffix = "mock.local" };
            var credentials = new AzureCredentials(new MockCredentials(), new MockCredentials(), "mock-tenant", env).WithDefaultSubscription(subscription);
            var rest = RestClient.Configure().WithEnvironment(env).WithCredentials(credentials).WithDelegatingHandler(new MockHandler()).WithLogLevel(HttpLoggingDelegatingHandler.Level.None).Build();
            return Azure.Authenticate(rest, "mock-tenant").WithSubscription(subscription);
        }
        public static void Main(string[] args)
        {
            try { string ep = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"), sub = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000"; IAzure azure; if (string.IsNullOrEmpty(ep)) { var c = SdkContext.AzureCredentialsFactory.FromServicePrincipal(Environment.GetEnvironmentVariable("CLIENT_ID"), Environment.GetEnvironmentVariable("CLIENT_SECRET"), Environment.GetEnvironmentVariable("TENANT_ID"), AzureEnvironment.AzureGlobalCloud); azure = Azure.Configure().Authenticate(c).WithSubscription(sub); } else azure = CreateMockClient(ep, sub); RunSample(azure); } catch (Exception ex) { Console.WriteLine(ex); }
        }
        private static string EnsureSlash(string v) => v.EndsWith("/", StringComparison.Ordinal) ? v : v + "/";
        private sealed class MockHandler : DelegatingHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken t) { r.RequestUri = new UriBuilder(r.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = r.RequestUri.Port }.Uri; return base.SendAsync(r, t); } }
        private sealed class MockCredentials : ServiceClientCredentials { public override Task ProcessHttpRequestAsync(HttpRequestMessage r, CancellationToken t) => Task.CompletedTask; }
    }
}
