using System.Net;
using System.Net.NetworkInformation;

namespace EXAT_EFM_EER_AuthenService.Helpers;

/// <summary>
/// Helper class for getting local machine information (SERVER SIDE)
/// NOTE: This reads SERVER information, not CLIENT information
/// For client info, the client application must call this API and send data back
/// </summary>
public static class ClientDeviceInfo
{
    /// <summary>
    /// Get the hostname of the current machine
    /// </summary>
    public static string GetHostname()
    {
        try
        {
            return Dns.GetHostName();
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Get MAC addresses from all active network interfaces
    /// </summary>
    public static List<NetworkInterfaceInfo> GetNetworkInterfaces()
    {
        var interfaces = new List<NetworkInterfaceInfo>();

        try
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in nics)
            {
                // Skip Loopback and Tunnel interfaces
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback || 
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                // Only include active interfaces
                if (adapter.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                
                // Convert to XX-XX-XX-XX-XX-XX format
                string macAddress = string.Join("-", bytes.Select(b => b.ToString("X2")));

                if (!string.IsNullOrEmpty(macAddress) && macAddress != "00-00-00-00-00-00")
                {
                    var ipProps = adapter.GetIPProperties();
                    
                    // Get IPv4 addresses
                    var ipv4Addresses = ipProps.UnicastAddresses
                        .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(ip => ip.Address.ToString())
                        .ToList();
                    
                    // Get IPv6 addresses
                    var ipv6Addresses = ipProps.UnicastAddresses
                        .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        .Select(ip => ip.Address.ToString())
                        .ToList();
                    
                    // Get subnet masks
                    var subnetMasks = ipProps.UnicastAddresses
                        .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(ip => ip.IPv4Mask?.ToString() ?? "N/A")
                        .ToList();
                    
                    // Get default gateway
                    var defaultGateways = ipProps.GatewayAddresses
                        .Select(g => g.Address.ToString())
                        .ToList();
                    
                    // Get DNS servers
                    var dnsServers = ipProps.DnsAddresses
                        .Select(dns => dns.ToString())
                        .ToList();
                    
                    // DHCP enabled (Windows only)
                    bool dhcpEnabled = false;
                    try
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            dhcpEnabled = ipProps.GetIPv4Properties()?.IsDhcpEnabled ?? false;
                        }
                    }
                    catch { }
                    
                    // DNS suffix
                    string dnsSuffix = ipProps.DnsSuffix ?? string.Empty;

                    interfaces.Add(new NetworkInterfaceInfo
                    {
                        Name = adapter.Name,
                        Description = adapter.Description,
                        Type = adapter.NetworkInterfaceType.ToString(),
                        Status = adapter.OperationalStatus.ToString(),
                        MacAddress = macAddress,
                        IsActive = true,
                        DhcpEnabled = dhcpEnabled,
                        IPv4Addresses = ipv4Addresses,
                        IPv6Addresses = ipv6Addresses,
                        SubnetMasks = subnetMasks,
                        DefaultGateways = defaultGateways,
                        DnsServers = dnsServers,
                        DnsSuffix = dnsSuffix
                    });
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return interfaces;
    }

    /// <summary>
    /// Get the primary (active) MAC address
    /// </summary>
    public static string GetPrimaryMacAddress()
    {
        var interfaces = GetNetworkInterfaces();
        var activeInterface = interfaces.FirstOrDefault(i => i.IsActive);
        return activeInterface?.MacAddress ?? "00-00-00-00-00-00";
    }
}

/// <summary>
/// Network interface information
/// </summary>
public class NetworkInterfaceInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool DhcpEnabled { get; set; }
    public List<string> IPv4Addresses { get; set; } = new();
    public List<string> IPv6Addresses { get; set; } = new();
    public List<string> SubnetMasks { get; set; } = new();
    public List<string> DefaultGateways { get; set; } = new();
    public List<string> DnsServers { get; set; } = new();
    public string DnsSuffix { get; set; } = string.Empty;
}
