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

namespace CreateVMSample
{
    public class Program
    {
        private const string AdminUsername = "sampleuser";
        private const string AdminPassword = "Benchmark!Passw0rd123";

        public static void RunSample(IAzure azure)
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            string resourceGroupName = $"quickstart-{suffix}";
            string vnetName = $"vnet-{suffix}";
            string nicName = $"nic-{suffix}";
            string vmName = $"vm{suffix}";
            Region region = Region.USWest2;
            try
            {
                azure.ResourceGroups.Define(resourceGroupName).WithRegion(region).Create();
                var network = azure.Networks.Define(vnetName)
                    .WithRegion(region).WithExistingResourceGroup(resourceGroupName)
                    .WithAddressSpace("10.0.0.0/16").WithSubnet("default", "10.0.0.0/28").Create();
                var nic = azure.NetworkInterfaces.Define(nicName)
                    .WithRegion(region).WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetwork(network).WithSubnet("default")
                    .WithPrimaryPrivateIPAddressDynamic().Create();
                azure.VirtualMachines.Define(vmName)
                    .WithRegion(region).WithExistingResourceGroup(resourceGroupName)
                    .WithExistingPrimaryNetworkInterface(nic)
                    .WithSpecificWindowsImageVersion(new ImageReference
                    {
                        Publisher = "MicrosoftWindowsServer", Offer = "WindowsServer",
                        Sku = "2016-Datacenter", Version = "latest"
                    })
                    .WithAdminUsername(AdminUsername).WithAdminPassword(AdminPassword)
                    .WithSize(VirtualMachineSizeTypes.Parse("Standard_F2")).Create();
            }
            finally
            {
                try { azure.ResourceGroups.DeleteByName(resourceGroupName); }
                catch (Exception ex) { Console.WriteLine(ex); }
            }
        }

        public static IAzure CreateMockClient(string endpoint, string subscriptionId)
        {
            var uri = new Uri(EnsureSlash(endpoint));
            string tls = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.Port }.Uri.AbsoluteUri;
            var environment = new AzureEnvironment { Name = "Mock", AuthenticationEndpoint = tls, ResourceManagerEndpoint = tls, GraphEndpoint = tls, ManagementEndpoint = tls, StorageEndpointSuffix = "mock.local", KeyVaultSuffix = "mock.local" };
            var credentials = new AzureCredentials(new MockCredentials(), new MockCredentials(), "mock-tenant", environment).WithDefaultSubscription(subscriptionId);
            var restClient = RestClient.Configure().WithEnvironment(environment).WithCredentials(credentials)
                .WithDelegatingHandler(new MockEndpointHandler()).WithLogLevel(HttpLoggingDelegatingHandler.Level.None).Build();
            return Azure.Authenticate(restClient, "mock-tenant").WithSubscription(subscriptionId);
        }

        public static void Main(string[] args)
        {
            try
            {
                string endpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT");
                string subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID") ?? "00000000-0000-0000-0000-000000000000";
                IAzure azure;
                if (string.IsNullOrEmpty(endpoint))
                {
                    var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(Environment.GetEnvironmentVariable("CLIENT_ID"), Environment.GetEnvironmentVariable("CLIENT_SECRET"), Environment.GetEnvironmentVariable("TENANT_ID"), AzureEnvironment.AzureGlobalCloud);
                    azure = Azure.Configure().Authenticate(credentials).WithSubscription(subscription);
                }
                else azure = CreateMockClient(endpoint, subscription);
                RunSample(azure);
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private static string EnsureSlash(string value) => value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
        private sealed class MockEndpointHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttp, Port = request.RequestUri.Port }.Uri;
                return base.SendAsync(request, token);
            }
        }
        private sealed class MockCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken token) => Task.CompletedTask;
        }
    }
}
