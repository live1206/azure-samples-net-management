using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CreateVMSample
{
    public class GeneratedTrack1Program
    {
        const string User="sampleuser",Password="Benchmark!Passw0rd123";
        public sealed class Clients{public ResourceManagementClient Resources{get;}public NetworkManagementClient Network{get;}public ComputeManagementClient Compute{get;}public Clients(ResourceManagementClient r,NetworkManagementClient n,ComputeManagementClient c){Resources=r;Network=n;Compute=c;}}
        public static void RunSample(Clients c)
        {
            string s=Guid.NewGuid().ToString("N").Substring(0,8),rg=$"quickstart-{s}",vnet=$"vnet-{s}",nic=$"nic-{s}",vm=$"vm{s}";const string loc="westus2";
            try
            {
                c.Resources.ResourceGroups.CreateOrUpdate(rg,new ResourceGroup(loc));
                var n=c.Network.VirtualNetworks.CreateOrUpdate(rg,vnet,new VirtualNetwork(location:loc,addressSpace:new AddressSpace(new[]{"10.0.0.0/16"}),subnets:new[]{new Subnet(name:"default",addressPrefix:"10.0.0.0/28")}));
                var ni=c.Network.NetworkInterfaces.CreateOrUpdate(rg,nic,new NetworkInterface(location:loc,ipConfigurations:new[]{new NetworkInterfaceIPConfiguration(name:"Primary",privateIPAllocationMethod:IPAllocationMethod.Dynamic,subnet:new Subnet(id:n.Subnets.Single().Id),primary:true)}));
                c.Compute.VirtualMachines.CreateOrUpdate(rg,vm,new VirtualMachine(location:loc,hardwareProfile:new HardwareProfile("Standard_F2"),storageProfile:new StorageProfile(imageReference:new ImageReference("MicrosoftWindowsServer","WindowsServer","2016-Datacenter","latest")),osProfile:new OSProfile(computerName:vm,adminUsername:User,adminPassword:Password),networkProfile:new Microsoft.Azure.Management.Compute.Models.NetworkProfile(new[]{new NetworkInterfaceReference(ni.Id,primary:true)})));
            }
            finally{try{c.Resources.ResourceGroups.Delete(rg);}catch(Exception ex){Console.WriteLine(ex);}}
        }
        public static Clients CreateMockClient(string e,string sub){var u=new Uri(Ensure(e));var tls=new Uri(new UriBuilder(u){Scheme=Uri.UriSchemeHttps,Port=u.Port}.Uri.AbsoluteUri);var cr=new MC();return new Clients(new ResourceManagementClient(cr,new MH()){SubscriptionId=sub,BaseUri=tls},new NetworkManagementClient(cr,new MH()){SubscriptionId=sub,BaseUri=tls},new ComputeManagementClient(cr,new MH()){SubscriptionId=sub,BaseUri=tls});}
        public static void Main(string[] a){string e=Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"),s=Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")??"00000000-0000-0000-0000-000000000000";RunSample(CreateMockClient(e,s));}
        static string Ensure(string v)=>v.EndsWith("/",StringComparison.Ordinal)?v:v+"/";sealed class MH:DelegatingHandler{protected override Task<HttpResponseMessage>SendAsync(HttpRequestMessage r,CancellationToken t){r.RequestUri=new UriBuilder(r.RequestUri){Scheme=Uri.UriSchemeHttp,Port=r.RequestUri.Port}.Uri;return base.SendAsync(r,t);}}sealed class MC:ServiceClientCredentials{public override Task ProcessHttpRequestAsync(HttpRequestMessage r,CancellationToken t)=>Task.CompletedTask;}
    }
}
