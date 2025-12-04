namespace EXAT_EFM_EER_AuthenService.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Device information model
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// MAC Address of the device (internal use only - not serialized)
    /// </summary>
    [JsonIgnore]
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Device name/hostname (internal use only - not serialized)
    /// </summary>
    [JsonIgnore]
    public string? DeviceName { get; set; }

    /// <summary>
    /// IP Address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Real client IP (for VPN/Proxy scenarios)
    /// </summary>
    public string? RealIpAddress { get; set; }

    /// <summary>
    /// User Agent information
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session token
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Is this device connecting via VPN/Proxy
    /// </summary>
    public bool IsVpnConnection { get; set; }

    /// <summary>
    /// Device registration timestamp
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Last connection timestamp
    /// </summary>
    public DateTime? LastConnectedAt { get; set; }

    /// <summary>
    /// Device status (Active, Blocked, etc.)
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Additional device information
    /// </summary>
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Request model for device validation
/// </summary>
public class DeviceValidationRequest
{
    /// <summary>
    /// MAC Address to validate
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// IP Address of the device (from X-Forwarded-For if behind proxy/VPN)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Real client IP (X-Forwarded-For header for VPN/Proxy detection)
    /// </summary>
    public string? RealIpAddress { get; set; }

    /// <summary>
    /// Device name
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// User Agent string for device fingerprinting
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session token for additional validation
    /// </summary>
    public string? SessionToken { get; set; }

    /// <summary>
    /// Is connecting via VPN/Proxy
    /// </summary>
    public bool IsVpnConnection { get; set; }
}

/// <summary>
/// Response model for device validation
/// </summary>
public class DeviceValidationResponse
{
    /// <summary>
    /// Whether the device is valid/allowed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether this is a new device registration
    /// </summary>
    public bool IsNewDevice { get; set; }

    /// <summary>
    /// Device information
    /// </summary>
    public DeviceInfo? DeviceInfo { get; set; }

    /// <summary>
    /// Validation message
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Request model for device registration from client
/// </summary>
public class DeviceRegistrationRequest
{
    /// <summary>
    /// MAC Address from client device (required)
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Device name/hostname (optional)
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Additional device information (optional)
    /// </summary>
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// Response model for session token generation
/// </summary>
public class SessionTokenResponse
{
    /// <summary>
    /// Generated session token (unique identifier)
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates if this is a newly created session (true) or existing session (false)
    /// </summary>
    public bool IsNewSession { get; set; }

    /// <summary>
    /// Device information associated with this token
    /// </summary>
    public DeviceInfo? DeviceInfo { get; set; }

    /// <summary>
    /// Server device information (hostname and network interfaces from the machine running the API)
    /// </summary>
    public object? ServerDeviceInfo { get; set; }
}
