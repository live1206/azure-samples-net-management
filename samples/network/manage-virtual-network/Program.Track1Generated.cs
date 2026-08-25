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

namespace ManageVirtualNetwork
{
    public class GeneratedTrack1Program
    {
        const string User="tirekicker",Password="Benchmark!Passw0rd123";
        public sealed class Clients{public ResourceManagementClient Resources{get;}public NetworkManagementClient Network{get;}public ComputeManagementClient Compute{get;}public Clients(ResourceManagementClient r,NetworkManagementClient n,ComputeManagementClient c){Resources=r;Network=n;Compute=c;}}
        public static void RunSample(Clients c)
        {
            string s=Guid.NewGuid().ToString("N").Substring(0,8),rg=$"rgNEMV-{s}",v1=$"vnet1-{s}",v2=$"vnet2-{s}",fe=$"fevm{s}",be=$"bevm{s}";const string loc="eastus";
            try
            {
                c.Resources.ResourceGroups.CreateOrUpdate(rg,new ResourceGroup(loc));
                var bn=c.Network.NetworkSecurityGroups.CreateOrUpdate(rg,$"backendnsg-{s}",Nsg(loc,false));
                var fn=c.Network.NetworkSecurityGroups.CreateOrUpdate(rg,$"frontendnsg-{s}",Nsg(loc,true));
                var network=c.Network.VirtualNetworks.CreateOrUpdate(rg,v1,Vnet(loc,null,bn.Id));
                network=c.Network.VirtualNetworks.CreateOrUpdate(rg,v1,Vnet(loc,fn.Id,bn.Id));
                var pip=c.Network.PublicIPAddresses.CreateOrUpdate(rg,$"{fe}-ip",new PublicIPAddress(location:loc,publicIPAllocationMethod:IPAllocationMethod.Dynamic,publicIPAddressVersion:Microsoft.Azure.Management.Network.Models.IPVersion.IPv4));
                var fnic=c.Network.NetworkInterfaces.CreateOrUpdate(rg,$"{fe}-nic",Nic(loc,network.Subnets.Single(x=>x.Name=="frontend").Id,pip.Id));
                var bnic=c.Network.NetworkInterfaces.CreateOrUpdate(rg,$"{be}-nic",Nic(loc,network.Subnets.Single(x=>x.Name=="backend").Id,null));
                c.Compute.VirtualMachines.CreateOrUpdate(rg,fe,Vm(loc,fe,fnic.Id));c.Compute.VirtualMachines.CreateOrUpdate(rg,be,Vm(loc,be,bnic.Id));
                var second=c.Network.VirtualNetworks.CreateOrUpdate(rg,v2,new VirtualNetwork(location:loc,addressSpace:new AddressSpace(new[]{"10.0.0.0/16"})));
                foreach(var n in c.Network.VirtualNetworks.List(rg))_=n.Id;
                c.Network.VirtualNetworks.Delete(rg,v2);
            }
            finally{try{c.Resources.ResourceGroups.Delete(rg);}catch(Exception ex){Console.WriteLine(ex);}}
        }
        static NetworkSecurityGroup Nsg(string loc,bool front)=>new NetworkSecurityGroup(location:loc,securityRules:new[]{new SecurityRule(name:front?"AllowHttpInComing":"DenyInternetInComing",protocol:SecurityRuleProtocol.Asterisk,sourcePortRange:"*",destinationPortRange:front?"80":"*",sourceAddressPrefix:"Internet",destinationAddressPrefix:"*",access:front?SecurityRuleAccess.Allow:SecurityRuleAccess.Deny,priority:700,direction:SecurityRuleDirection.Inbound),new SecurityRule(name:"DenyInternetOutGoing",protocol:SecurityRuleProtocol.Asterisk,sourcePortRange:"*",destinationPortRange:"*",sourceAddressPrefix:"*",destinationAddressPrefix:"Internet",access:SecurityRuleAccess.Deny,priority:701,direction:SecurityRuleDirection.Outbound)});
        static VirtualNetwork Vnet(string loc,string frontNsg,string backNsg){var f=new Subnet(name:"frontend",addressPrefix:"192.168.1.0/24");if(frontNsg!=null)f.NetworkSecurityGroup=new NetworkSecurityGroup(id:frontNsg);return new VirtualNetwork(location:loc,addressSpace:new AddressSpace(new[]{"192.168.0.0/16"}),subnets:new[]{f,new Subnet(name:"backend",addressPrefix:"192.168.2.0/24",networkSecurityGroup:new NetworkSecurityGroup(id:backNsg))});}
        static NetworkInterface Nic(string loc,string subnet,string pip){var x=new NetworkInterfaceIPConfiguration(name:"Primary",privateIPAllocationMethod:IPAllocationMethod.Dynamic,subnet:new Subnet(id:subnet),primary:true);if(pip!=null)x.PublicIPAddress=new PublicIPAddress(id:pip);return new NetworkInterface(location:loc,ipConfigurations:new[]{x});}
        static VirtualMachine Vm(string loc,string name,string nic)=>new VirtualMachine(location:loc,hardwareProfile:new HardwareProfile("Standard_D3_v2"),storageProfile:new StorageProfile(imageReference:new ImageReference("Canonical","UbuntuServer","18.04-LTS","latest")),osProfile:new OSProfile(computerName:name,adminUsername:User,adminPassword:Password,linuxConfiguration:new LinuxConfiguration(disablePasswordAuthentication:false)),networkProfile:new Microsoft.Azure.Management.Compute.Models.NetworkProfile(new[]{new NetworkInterfaceReference(nic,primary:true)}));
        public static Clients CreateMockClient(string e,string sub){var u=new Uri(Ensure(e));var tls=new Uri(new UriBuilder(u){Scheme=Uri.UriSchemeHttps,Port=u.Port}.Uri.AbsoluteUri);var cr=new MC();return new Clients(new ResourceManagementClient(cr,new MH()){SubscriptionId=sub,BaseUri=tls},new NetworkManagementClient(cr,new MH()){SubscriptionId=sub,BaseUri=tls},new ComputeManagementClient(cr,new MH()){SubscriptionId=sub,BaseUri=tls});}
        public static void Main(string[] a){string e=Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT"),s=Environment.GetEnvironmentVariable("SUBSCRIPTION_ID")??"00000000-0000-0000-0000-000000000000";RunSample(CreateMockClient(e,s));}
        static string Ensure(string v)=>v.EndsWith("/",StringComparison.Ordinal)?v:v+"/";sealed class MH:DelegatingHandler{protected override Task<HttpResponseMessage>SendAsync(HttpRequestMessage r,CancellationToken t){r.RequestUri=new UriBuilder(r.RequestUri){Scheme=Uri.UriSchemeHttp,Port=r.RequestUri.Port}.Uri;return base.SendAsync(r,t);}}sealed class MC:ServiceClientCredentials{public override Task ProcessHttpRequestAsync(HttpRequestMessage r,CancellationToken t)=>Task.CompletedTask;}
    }
}
