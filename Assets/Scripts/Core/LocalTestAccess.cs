using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace CyberExtraction.Core
{
    // Server-side policy for direct UTP connections. A private address alone is insufficient:
    // it must belong to an active local LAN subnet. Unknown endpoints fail closed.
    public static class LocalTestAccess
    {
        static bool Private(IPAddress address)
        {
            var b=address.GetAddressBytes();
            return b.Length==4 && (b[0]==10 || b[0]==172 && b[1]>=16 && b[1]<=31 || b[0]==192 && b[1]==168);
        }
        public static bool Allows(string remote)
        {
            if(!IPAddress.TryParse(remote,out var address)) return false;
            if(address.IsIPv4MappedToIPv6) address=address.MapToIPv4();
            if(IPAddress.IsLoopback(address)) return true;
            if(address.AddressFamily!=AddressFamily.InterNetwork || !Private(address)) return false;
            try
            {
                var peer=address.GetAddressBytes();
                foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if(adapter.OperationalStatus!=OperationalStatus.Up ||
                        (adapter.NetworkInterfaceType!=NetworkInterfaceType.Ethernet && adapter.NetworkInterfaceType!=NetworkInterfaceType.Wireless80211)) continue;
                    foreach(var local in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if(local.Address.AddressFamily!=AddressFamily.InterNetwork || !Private(local.Address) || local.IPv4Mask==null) continue;
                        var host=local.Address.GetAddressBytes(); var mask=local.IPv4Mask.GetAddressBytes();
                        bool same=true,hasMask=false;
                        for(int i=0;i<4;i++) { hasMask|=mask[i]!=0; if((host[i]&mask[i])!=(peer[i]&mask[i])) same=false; }
                        if(same && hasMask) return true;
                    }
                }
            }
            catch(Exception) { return false; }
            return false;
        }
    }
}
