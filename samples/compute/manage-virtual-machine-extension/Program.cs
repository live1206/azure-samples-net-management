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

namespace ManageVirtualMachineExtension
{
    public class Program
    {
        const string User="tirekicker",Password="Benchmark!Passw0rd123";
        public static async Task RunSample(ArmClient client,string subscriptionId)
        {
            string s=Guid.NewGuid().ToString("N").Substring(0,8),rg=$"rgCOVE-{s}",linux=$"lvm{s}",windows=$"wvm{s}";ResourceGroupResource g=null;
            try
            {
                var sub=client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));g=(await sub.GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed,rg,new ResourceGroupData(AzureLocation.EastUS))).Value;
                var net=(await g.GetVirtualNetworks().CreateOrUpdateAsync(Azure.WaitUntil.Completed,$"vnet-{s}",new VirtualNetworkData{Location=AzureLocation.EastUS,AddressPrefixes={"10.0.0.0/16"},Subnets={new SubnetData{Name="default",AddressPrefix="10.0.0.0/28"}}})).Value;
                var lvm=await CreateVm(g,net,linux,$"lnic-{s}",true);var le=lvm.GetVirtualMachineExtensions();
                var access=(await le.CreateOrUpdateAsync(Azure.WaitUntil.Completed,"VMAccessForLinux",Ext("Microsoft.OSTCExtensions","VMAccessForLinux","1.4","seconduser"))).Value;
                access=(await le.CreateOrUpdateAsync(Azure.WaitUntil.Completed,"VMAccessForLinux",Ext("Microsoft.OSTCExtensions","VMAccessForLinux","1.4","thirduser"))).Value;
                access=(await le.CreateOrUpdateAsync(Azure.WaitUntil.Completed,"VMAccessForLinux",Ext("Microsoft.OSTCExtensions","VMAccessForLinux","1.4",User))).Value;
                var script=(await le.CreateOrUpdateAsync(Azure.WaitUntil.Completed,"CustomScriptForLinux",Ext("Microsoft.OSTCExtensions","CustomScriptForLinux","1.4",null))).Value;
                _ = script; _ = access;
                var wvm=await CreateVm(g,net,windows,$"wnic-{s}",false);var we=wvm.GetVirtualMachineExtensions();
                var wa=(await we.CreateOrUpdateAsync(Azure.WaitUntil.Completed,"VMAccessAgent",Ext("Microsoft.Compute","VMAccessAgent","2.3","seconduser"))).Value;
                wa=(await we.CreateOrUpdateAsync(Azure.WaitUntil.Completed,"VMAccessAgent",Ext("Microsoft.Compute","VMAccessAgent","2.3","thirduser"))).Value;
                wa=(await we.CreateOrUpdateAsync(Azure.WaitUntil.Completed,"VMAccessAgent",Ext("Microsoft.Compute","VMAccessAgent","2.3",User))).Value;
                _ = wa;
            }
            finally{if(g!=null)try{await g.DeleteAsync(Azure.WaitUntil.Completed);}catch(Exception ex){Console.WriteLine(ex);}}
        }
        static VirtualMachineExtensionData Ext(string p,string t,string v,string user)=>new VirtualMachineExtensionData(AzureLocation.EastUS){Publisher=p,ExtensionType=t,TypeHandlerVersion=v,AutoUpgradeMinorVersion=true,ProtectedSettings=BinaryData.FromObjectAsJson(user==null?new{commandToExecute="echo benchmark"}:(object)new{username=user,password=Password})};
        static async Task<VirtualMachineResource> CreateVm(ResourceGroupResource g,VirtualNetworkResource net,string name,string nicName,bool linux){var nic=(await g.GetNetworkInterfaces().CreateOrUpdateAsync(Azure.WaitUntil.Completed,nicName,new NetworkInterfaceData{Location=AzureLocation.EastUS,IPConfigurations={new NetworkInterfaceIPConfigurationData{Name="Primary",Primary=true,PrivateIPAllocationMethod=NetworkIPAllocationMethod.Dynamic,Subnet=new SubnetData{Id=net.Data.Subnets.Single().Id}}}})).Value;var d=new VirtualMachineData(AzureLocation.EastUS){HardwareProfile=new VirtualMachineHardwareProfile{VmSize=VirtualMachineSizeType.StandardD3V2},OSProfile=new VirtualMachineOSProfile{ComputerName=name,AdminUsername=User,AdminPassword=Password},NetworkProfile=new VirtualMachineNetworkProfile{NetworkInterfaces={new VirtualMachineNetworkInterfaceReference{Id=nic.Id,Primary=true}}},StorageProfile=new VirtualMachineStorageProfile{ImageReference=linux?new ImageReference{Publisher="Canonical",Offer="UbuntuServer",Sku="18.04-LTS",Version="latest"}:new ImageReference{Publisher="MicrosoftWindowsServer",Offer="WindowsServer",Sku="2016-Datacenter",Version="latest"}}};if(linux)d.OSProfile.LinuxConfiguration=new LinuxConfiguration{DisablePasswordAuthentication=false,ProvisionVmAgent=true};return(await g.GetVirtualMachines().CreateOrUpdateAsync(Azure.WaitUntil.Completed,name,d)).Value;}
        public static ArmClient CreateMockClient(string e,string sub){var u=new Uri(Ensure(e));var tls=new UriBuilder(u){Scheme=Uri.UriSchemeHttps,Port=u.Port}.Uri;var o=new ArmClientOptions{Environment=new ArmEnvironment(tls,"https://management.azure.com/"),Transport=new HttpClientTransport(new HttpClient(new MH()))};o.Retry.MaxRetries=0;return new ArmClient(new MC(),sub,o);}
        public static async Task Main(string[] a){try{string e=Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"),s=Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")??"00000000-0000-0000-0000-000000000000";var c=string.IsNullOrEmpty(e)?new ArmClient(new ClientSecretCredential(Environment.GetEnvironmentVariable("TENANT_ID"),Environment.GetEnvironmentVariable("CLIENT_ID"),Environment.GetEnvironmentVariable("CLIENT_SECRET")),s):CreateMockClient(e,s);await RunSample(c,s);}catch(Exception ex){Console.WriteLine(ex);}}
        static string Ensure(string v)=>v.EndsWith("/",StringComparison.Ordinal)?v:v+"/";
        sealed class MH:DelegatingHandler{public MH():base(new HttpClientHandler()){}protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t){r.RequestUri=new UriBuilder(r.RequestUri){Scheme=Uri.UriSchemeHttp,Port=r.RequestUri.Port}.Uri;return base.SendAsync(r,t);}}
        sealed class MC:TokenCredential{static readonly AccessToken T=new AccessToken("mock",DateTimeOffset.MaxValue);public override AccessToken GetToken(TokenRequestContext c,CancellationToken t)=>T;public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext c,CancellationToken t)=>new ValueTask<AccessToken>(T);}
    }
}
