using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CreateVirtualMachinesInParallel
{
    public class Program
    {
        private const string Username = "tirekicker";
        private const string Password = "Benchmark!Passw0rd123";
        public static void RunSample(IAzure azure)
        {
            string s = Guid.NewGuid().ToString("N").Substring(0, 8), rg = $"rgCOP-{s}";
            try
            {
                azure.ResourceGroups.Define(rg).WithRegion(Region.USWest2).Create();
                var definitions = new List<ICreatable<IVirtualMachine>>();
                AddRegion(azure, definitions, rg, Region.USEast, "east", s);
                AddRegion(azure, definitions, rg, Region.USSouthCentral, "south", s);
                azure.VirtualMachines.Create(definitions.ToArray());
            }
            finally { try { azure.ResourceGroups.DeleteByName(rg); } catch (Exception ex) { Console.WriteLine(ex); } }
        }
        private static void AddRegion(IAzure azure, List<ICreatable<IVirtualMachine>> definitions, string rg, Region region, string prefix, string suffix)
        {
            var network = azure.Networks.Define($"vnet-{prefix}-{suffix}").WithRegion(region).WithExistingResourceGroup(rg).WithAddressSpace("172.16.0.0/16").WithSubnet("default", "172.16.0.0/24");
            for (int i = 1; i <= 5; i++)
            {
                string name = $"vm-{prefix}-{i}-{suffix}";
                var pip = azure.PublicIPAddresses.Define($"pip-{prefix}-{i}-{suffix}").WithRegion(region).WithExistingResourceGroup(rg).WithDynamicIP().WithLeafDomainLabel($"pip-{prefix}-{i}-{suffix}");
                definitions.Add(azure.VirtualMachines.Define(name).WithRegion(region).WithExistingResourceGroup(rg).WithNewPrimaryNetwork(network).WithPrimaryPrivateIPAddressDynamic().WithNewPrimaryPublicIPAddress(pip)
                    .WithSpecificLinuxImageVersion(new ImageReference { Publisher = "Canonical", Offer = "UbuntuServer", Sku = "16.04-LTS", Version = "latest" })
                    .WithRootUsername(Username).WithRootPassword(Password).WithSize(VirtualMachineSizeTypes.Parse("Standard_D2a_v4")));
            }
        }
        public static IAzure CreateMockClient(string endpoint, string subscription) { var uri = new Uri(Ensure(endpoint)); string tls = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port }.Uri.AbsoluteUri; var env = new AzureEnvironment { Name = "Mock", AuthenticationEndpoint = tls, ResourceManagerEndpoint = tls, GraphEndpoint = tls, ManagementEndpoint = tls, StorageEndpointSuffix = "mock.local", KeyVaultSuffix = "mock.local" }; var c = new AzureCredentials(new MockCredentials(), new MockCredentials(), "mock", env).WithDefaultSubscription(subscription); var r = RestClient.Configure().WithEnvironment(env).WithCredentials(c).WithDelegatingHandler(new MockHandler()).WithLogLevel(HttpLoggingDelegatingHandler.Level.None).Build(); return Azure.Authenticate(r, "mock").WithSubscription(subscription); }
        public static void Main(string[] args) { try { string e = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"), s = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000"; IAzure a; if (string.IsNullOrEmpty(e)) { var c = SdkContext.AzureCredentialsFactory.FromServicePrincipal(Environment.GetEnvironmentVariable("CLIENT_ID"), Environment.GetEnvironmentVariable("CLIENT_SECRET"), Environment.GetEnvironmentVariable("TENANT_ID"), AzureEnvironment.AzureGlobalCloud); a = Azure.Configure().Authenticate(c).WithSubscription(s); } else a = CreateMockClient(e, s); RunSample(a); } catch (Exception ex) { Console.WriteLine(ex); } }
        private static string Ensure(string v) => v.EndsWith("/", StringComparison.Ordinal) ? v : v + "/";
        private sealed class MockHandler : DelegatingHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken t) { r.RequestUri = new UriBuilder(r.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = r.RequestUri.Port }.Uri; return base.SendAsync(r, t); } }
        private sealed class MockCredentials : ServiceClientCredentials { public override Task ProcessHttpRequestAsync(HttpRequestMessage r, CancellationToken t) => Task.CompletedTask; }
    }
}
