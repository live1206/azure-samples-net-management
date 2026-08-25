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

namespace ManageVirtualMachineExtension
{
    public class Program
    {
        const string User = "tirekicker", Password = "Benchmark!Passw0rd123";
        public static void RunSample(IAzure azure)
        {
            string s=Guid.NewGuid().ToString("N").Substring(0,8),rg=$"rgCOVE-{s}",vnet=$"vnet-{s}",linux=$"lvm{s}",windows=$"wvm{s}";
            try
            {
                azure.ResourceGroups.Define(rg).WithRegion(Region.USEast).Create();
                var net=azure.Networks.Define(vnet).WithRegion(Region.USEast).WithExistingResourceGroup(rg).WithAddressSpace("10.0.0.0/16").WithSubnet("default","10.0.0.0/28").Create();
                var lnic=azure.NetworkInterfaces.Define($"lnic-{s}").WithRegion(Region.USEast).WithExistingResourceGroup(rg).WithExistingPrimaryNetwork(net).WithSubnet("default").WithPrimaryPrivateIPAddressDynamic().Create();
                var lvm=azure.VirtualMachines.Define(linux).WithRegion(Region.USEast).WithExistingResourceGroup(rg).WithExistingPrimaryNetworkInterface(lnic).WithSpecificLinuxImageVersion(new ImageReference{Publisher="Canonical",Offer="UbuntuServer",Sku="18.04-LTS",Version="latest"}).WithRootUsername(User).WithRootPassword(Password).WithSize(VirtualMachineSizeTypes.Parse("Standard_D3_v2")).Create();
                lvm.Update().DefineNewExtension("VMAccessForLinux").WithPublisher("Microsoft.OSTCExtensions").WithType("VMAccessForLinux").WithVersion("1.4").WithProtectedSetting("username","seconduser").WithProtectedSetting("password",Password).Attach().Apply();
                lvm.Update().DefineNewExtension("VMAccessForLinux").WithPublisher("Microsoft.OSTCExtensions").WithType("VMAccessForLinux").WithVersion("1.4").WithProtectedSetting("username","thirduser").WithProtectedSetting("password",Password).Attach().Apply();
                lvm.Update().DefineNewExtension("VMAccessForLinux").WithPublisher("Microsoft.OSTCExtensions").WithType("VMAccessForLinux").WithVersion("1.4").WithProtectedSetting("username",User).WithProtectedSetting("password",Password).WithProtectedSetting("reset_ssh","true").Attach().Apply();
                lvm.Update().DefineNewExtension("CustomScriptForLinux").WithPublisher("Microsoft.OSTCExtensions").WithType("CustomScriptForLinux").WithVersion("1.4").WithMinorVersionAutoUpgrade().WithPublicSetting("commandToExecute","echo benchmark").Attach().Apply();

                var wnic=azure.NetworkInterfaces.Define($"wnic-{s}").WithRegion(Region.USEast).WithExistingResourceGroup(rg).WithExistingPrimaryNetwork(net).WithSubnet("default").WithPrimaryPrivateIPAddressDynamic().Create();
                var wvm=azure.VirtualMachines.Define(windows).WithRegion(Region.USEast).WithExistingResourceGroup(rg).WithExistingPrimaryNetworkInterface(wnic).WithSpecificWindowsImageVersion(new ImageReference{Publisher="MicrosoftWindowsServer",Offer="WindowsServer",Sku="2016-Datacenter",Version="latest"}).WithAdminUsername(User).WithAdminPassword(Password).WithSize(VirtualMachineSizeTypes.Parse("Standard_D3_v2")).Create();
                wvm.Update().DefineNewExtension("VMAccessAgent").WithPublisher("Microsoft.Compute").WithType("VMAccessAgent").WithVersion("2.3").WithProtectedSetting("username","seconduser").WithProtectedSetting("password",Password).Attach().Apply();
                wvm.Update().DefineNewExtension("VMAccessAgent").WithPublisher("Microsoft.Compute").WithType("VMAccessAgent").WithVersion("2.3").WithProtectedSetting("username","thirduser").WithProtectedSetting("password",Password).Attach().Apply();
                wvm.Update().DefineNewExtension("VMAccessAgent").WithPublisher("Microsoft.Compute").WithType("VMAccessAgent").WithVersion("2.3").WithProtectedSetting("username",User).WithProtectedSetting("password",Password).Attach().Apply();
            }
            finally{try{azure.ResourceGroups.DeleteByName(rg);}catch(Exception ex){Console.WriteLine(ex);}}
        }
        public static IAzure CreateMockClient(string e,string sub){var u=new Uri(Ensure(e));string tls=new UriBuilder(u){Scheme=Uri.UriSchemeHttps,Port=u.Port}.Uri.AbsoluteUri;var env=new AzureEnvironment{Name="Mock",AuthenticationEndpoint=tls,ResourceManagerEndpoint=tls,GraphEndpoint=tls,ManagementEndpoint=tls,StorageEndpointSuffix="mock.local",KeyVaultSuffix="mock.local"};var c=new AzureCredentials(new MC(),new MC(),"mock",env).WithDefaultSubscription(sub);var r=RestClient.Configure().WithEnvironment(env).WithCredentials(c).WithDelegatingHandler(new MH()).WithLogLevel(HttpLoggingDelegatingHandler.Level.None).Build();return Azure.Authenticate(r,"mock").WithSubscription(sub);}
        public static void Main(string[] a){try{string e=Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"),s=Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")??"00000000-0000-0000-0000-000000000000";IAzure z;if(string.IsNullOrEmpty(e)){var c=SdkContext.AzureCredentialsFactory.FromServicePrincipal(Environment.GetEnvironmentVariable("CLIENT_ID"),Environment.GetEnvironmentVariable("CLIENT_SECRET"),Environment.GetEnvironmentVariable("TENANT_ID"),AzureEnvironment.AzureGlobalCloud);z=Azure.Configure().Authenticate(c).WithSubscription(s);}else z=CreateMockClient(e,s);RunSample(z);}catch(Exception ex){Console.WriteLine(ex);}}
        static string Ensure(string v)=>v.EndsWith("/",StringComparison.Ordinal)?v:v+"/";
        sealed class MH:DelegatingHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t){r.RequestUri=new UriBuilder(r.RequestUri){Scheme=Uri.UriSchemeHttp,Port=r.RequestUri.Port}.Uri;return base.SendAsync(r,t);}}
        sealed class MC:ServiceClientCredentials{public override Task ProcessHttpRequestAsync(HttpRequestMessage r,CancellationToken t)=>Task.CompletedTask;}
    }
}
