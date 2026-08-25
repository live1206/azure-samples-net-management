using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Network;
using Microsoft.Azure.Management.Network.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ManageIPAddress
{
    public class GeneratedTrack1Program
    {
        const string UserName = "tirekicker", Password = "Benchmark!Passw0rd123";
        public sealed class Clients
        {
            public ResourceManagementClient Resources { get; }
            public NetworkManagementClient Network { get; }
            public ComputeManagementClient Compute { get; }
            public Clients(ResourceManagementClient resources, NetworkManagementClient network, ComputeManagementClient compute) { Resources = resources; Network = network; Compute = compute; }
        }

        public static void RunSample(Clients clients)
        {
            string s=Guid.NewGuid().ToString("N").Substring(0,8),rg=$"rgNEMP-{s}",vnetName=$"vnet-{s}",nicName=$"nic-{s}",vmName=$"vm{s}",pip1Name=$"pip1-{s}",pip2Name=$"pip2-{s}";
            const string location="eastus";
            try
            {
                clients.Resources.ResourceGroups.CreateOrUpdate(rg,new ResourceGroup(location));
                var pip1=clients.Network.PublicIPAddresses.CreateOrUpdate(rg,pip1Name,new PublicIPAddress(location:location,publicIPAllocationMethod:IPAllocationMethod.Dynamic,publicIPAddressVersion:Microsoft.Azure.Management.Network.Models.IPVersion.IPv4,dnsSettings:new PublicIPAddressDnsSettings(domainNameLabel:$"pipdns1-{s}")));
                var vnet=clients.Network.VirtualNetworks.CreateOrUpdate(rg,vnetName,new VirtualNetwork(location:location,addressSpace:new AddressSpace(new[]{"10.0.0.0/16"}),subnets:new[]{new Subnet(name:"default",addressPrefix:"10.0.0.0/28")}));
                var nic=clients.Network.NetworkInterfaces.CreateOrUpdate(rg,nicName,Nic(location,vnet.Subnets.Single().Id,pip1.Id));
                clients.Compute.VirtualMachines.CreateOrUpdate(rg,vmName,new VirtualMachine(location:location,hardwareProfile:new HardwareProfile("Standard_D3_v2"),storageProfile:new StorageProfile(imageReference:new ImageReference("MicrosoftWindowsServer","WindowsServer","2016-Datacenter","latest")),osProfile:new OSProfile(computerName:vmName,adminUsername:UserName,adminPassword:Password),networkProfile:new Microsoft.Azure.Management.Compute.Models.NetworkProfile(new[]{new NetworkInterfaceReference(nic.Id,primary:true)})));
                nic=clients.Network.NetworkInterfaces.Get(rg,nicName);
                _=clients.Network.PublicIPAddresses.Get(rg,pip1Name).Id;
                var pip2=clients.Network.PublicIPAddresses.CreateOrUpdate(rg,pip2Name,new PublicIPAddress(location:location,publicIPAllocationMethod:IPAllocationMethod.Dynamic,publicIPAddressVersion:Microsoft.Azure.Management.Network.Models.IPVersion.IPv4,dnsSettings:new PublicIPAddressDnsSettings(domainNameLabel:$"pipdns2-{s}")));
                clients.Network.NetworkInterfaces.CreateOrUpdate(rg,nicName,Nic(location,vnet.Subnets.Single().Id,pip2.Id));
                nic=clients.Network.NetworkInterfaces.Get(rg,nicName);
                _=clients.Network.PublicIPAddresses.Get(rg,pip2Name).Id;
                clients.Network.NetworkInterfaces.CreateOrUpdate(rg,nicName,Nic(location,vnet.Subnets.Single().Id,null));
                clients.Network.PublicIPAddresses.Delete(rg,pip1Name);
                clients.Network.PublicIPAddresses.Delete(rg,pip2Name);
            }
            finally { try { clients.Resources.ResourceGroups.Delete(rg); } catch(Exception ex){Console.WriteLine(ex);} }
        }

        static NetworkInterface Nic(string location,string subnetId,string pipId)
        {
            var config=new NetworkInterfaceIPConfiguration(name:"Primary",privateIPAllocationMethod:IPAllocationMethod.Dynamic,subnet:new Subnet(id:subnetId),primary:true);
            if(pipId!=null)config.PublicIPAddress=new PublicIPAddress(id:pipId);
            return new NetworkInterface(location:location,ipConfigurations:new[]{config});
        }

        public static Clients CreateMockClient(string endpoint,string subscription)
        {
            var u=new Uri(Ensure(endpoint));string tls=new UriBuilder(u){Scheme=Uri.UriSchemeHttps,Port=u.Port}.Uri.AbsoluteUri;var credentials=new MockCredentials();
            var resources=new ResourceManagementClient(credentials,new MockHandler()){SubscriptionId=subscription,BaseUri=new Uri(tls)};
            var network=new NetworkManagementClient(credentials,new MockHandler()){SubscriptionId=subscription,BaseUri=new Uri(tls)};
            var compute=new ComputeManagementClient(credentials,new MockHandler()){SubscriptionId=subscription,BaseUri=new Uri(tls)};
            return new Clients(resources,network,compute);
        }
        public static void Main(string[] args){string e=Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"),s=Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")??"00000000-0000-0000-0000-000000000000";if(string.IsNullOrEmpty(e))throw new InvalidOperationException("Set MOCK_ARM_ENDPOINT.");RunSample(CreateMockClient(e,s));}
        static string Ensure(string v)=>v.EndsWith("/",StringComparison.Ordinal)?v:v+"/";
        sealed class MockHandler:DelegatingHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t){r.RequestUri=new UriBuilder(r.RequestUri){Scheme=Uri.UriSchemeHttp,Port=r.RequestUri.Port}.Uri;return base.SendAsync(r,t);}}
        sealed class MockCredentials:ServiceClientCredentials{public override Task ProcessHttpRequestAsync(HttpRequestMessage r,CancellationToken t)=>Task.CompletedTask;}
    }
}
