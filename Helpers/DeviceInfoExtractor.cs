using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.NetworkInformation;

namespace EXAT_EFM_EER_AuthenService.Helpers;

/// <summary>
/// Helper class for extracting device information from HTTP request
/// </summary>
public static class DeviceInfoExtractor
{
    /// <summary>
    /// Extract client IP address from request
    /// </summary>
    public static string GetClientIpAddress(HttpContext context)
    {
        // Try X-Forwarded-For first (for proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Try X-Real-IP
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Get User Agent from request
    /// </summary>
    public static string GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
    }

    /// <summary>
    /// Detect if connection is through VPN/Proxy
    /// </summary>
    public static bool IsVpnConnection(HttpContext context)
    {
        // Check for proxy headers
        var hasXForwardedFor = context.Request.Headers.ContainsKey("X-Forwarded-For");
        var hasXRealIp = context.Request.Headers.ContainsKey("X-Real-IP");
        var hasVia = context.Request.Headers.ContainsKey("Via");
        
        return hasXForwardedFor || hasXRealIp || hasVia;
    }

    /// <summary>
    /// Try to get server's MAC address (limitation: can only get server's MAC, not client's)
    /// Note: Getting client MAC address from HTTP request is not possible for security reasons
    /// </summary>
    public static string GetServerMacAddress()
    {
        try
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(nic =>
                    nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            if (networkInterface != null)
            {
                var macAddress = networkInterface.GetPhysicalAddress().ToString();
                return FormatMacAddress(macAddress);
            }
        }
        catch
        {
            // Ignore errors
        }

        return "00:00:00:00:00:00";
    }

    /// <summary>
    /// Format MAC address with colons
    /// </summary>
    private static string FormatMacAddress(string mac)
    {
        if (string.IsNullOrEmpty(mac) || mac.Length != 12)
            return mac;

        return string.Join(":", Enumerable.Range(0, 6)
            .Select(i => mac.Substring(i * 2, 2)));
    }

    /// <summary>
    /// Get device name from request headers
    /// </summary>
    public static string GetDeviceName(HttpContext context)
    {
        // Try custom header first
        var deviceName = context.Request.Headers["X-Device-Name"].FirstOrDefault();
        if (!string.IsNullOrEmpty(deviceName))
        {
            return deviceName;
        }

        // Parse from User-Agent
        var userAgent = GetUserAgent(context);
        if (userAgent.Contains("Windows"))
            return "Windows Device";
        if (userAgent.Contains("Mac"))
            return "Mac Device";
        if (userAgent.Contains("Linux"))
            return "Linux Device";
        if (userAgent.Contains("Android"))
            return "Android Device";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            return "iOS Device";

        return "Unknown Device";
    }

    /// <summary>
    /// Get real IP address (for VPN scenarios)
    /// </summary>
    public static string GetRealIpAddress(HttpContext context)
    {
        // For VPN, try to get the original client IP
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            // Return the last IP in the chain (usually the real client IP)
            if (ips.Length > 1)
            {
                return ips[^1].Trim();
            }
        }

        return GetClientIpAddress(context);
    }

    /// <summary>
    /// Generate unique device ID from client information
    /// Combines IP address and User-Agent to create a semi-unique identifier
    /// </summary>
    public static string GetUniqueDeviceId(HttpContext context)
    {
        var clientIp = GetClientIpAddress(context);
        var userAgent = GetUserAgent(context);
        
        // Create a hash from IP + User-Agent
        var combinedInfo = $"{clientIp}_{userAgent}";
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(combinedInfo)
        );
        
        // Convert to hex string (take first 16 characters for readability)
        var deviceId = Convert.ToHexString(hash)[..16];
        
        return deviceId;
    }
}
