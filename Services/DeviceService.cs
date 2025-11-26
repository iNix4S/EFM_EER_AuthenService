using System.Collections.Concurrent;
using EXAT_EFM_EER_AuthenService.Models;

namespace EXAT_EFM_EER_AuthenService.Services;

/// <summary>
/// Service for managing device registrations and validations
/// </summary>
public interface IDeviceService
{
}

/// <summary>
/// In-memory implementation of device service
/// TODO: Replace with database implementation in production
/// </summary>
public class DeviceService : IDeviceService
{
}

/// <summary>
/// Service for managing session tokens
/// </summary>
public interface ISessionService
{
    Task<SessionTokenResponse> CreateSessionAsync(string deviceId, string? deviceName, DeviceInfo deviceInfo);
    Task<SessionTokenResponse?> GetSessionByDeviceIdAsync(string deviceId);
    Task<SessionTokenResponse?> GetSessionByTokenAsync(string token);
    Task<bool> ClearSessionAsync(string token);
    Task<int> ClearAllSessionsAsync();
}

/// <summary>
/// In-memory implementation of session service
/// TODO: Replace with database implementation in production
/// </summary>
public class SessionService : ISessionService
{
    // Storage: Key = DeviceId, Value = SessionTokenResponse
    private readonly ConcurrentDictionary<string, SessionTokenResponse> _sessionsByDeviceId = new();
    
    // Storage: Key = SessionToken, Value = SessionTokenResponse
    private readonly ConcurrentDictionary<string, SessionTokenResponse> _sessionsByToken = new();
    
    // Token expiration hours from configuration
    private readonly int _tokenExpirationHours;

    public SessionService(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _tokenExpirationHours = configuration.GetValue<int>("SessionSettings:TokenExpirationHours", 24);
    }

    public Task<SessionTokenResponse> CreateSessionAsync(string deviceId, string? deviceName, DeviceInfo deviceInfo)
    {
        // Check if device already has an active token
        if (_sessionsByDeviceId.TryGetValue(deviceId, out var existingSession))
        {
            // Check if token is still valid
            if (existingSession.ExpiresAt > DateTime.Now)
            {
                // Cannot create new token - device already has an active token
                throw new InvalidOperationException($"Device already has an active session token. Please clear the existing token before creating a new one.");
            }
            
            // Token expired, remove old entries
            _sessionsByToken.TryRemove(existingSession.SessionToken, out _);
            _sessionsByDeviceId.TryRemove(deviceId, out _);
        }

        // Create new session token
        var sessionToken = Guid.NewGuid().ToString();
        var expiresAt = DateTime.Now.AddHours(_tokenExpirationHours);

        deviceInfo.SessionToken = sessionToken;
        deviceInfo.MacAddress = deviceId; // Store deviceId in MacAddress field
        
        var response = new SessionTokenResponse
        {
            SessionToken = sessionToken,
            ExpiresAt = expiresAt,
            DeviceInfo = deviceInfo
        };

        // Store in both dictionaries
        _sessionsByDeviceId.TryAdd(deviceId, response);
        _sessionsByToken.TryAdd(sessionToken, response);

        return Task.FromResult(response);
    }

    public Task<SessionTokenResponse?> GetSessionByDeviceIdAsync(string deviceId)
    {
        _sessionsByDeviceId.TryGetValue(deviceId, out var session);
        
        // Check if expired
        if (session != null && session.ExpiresAt <= DateTime.Now)
        {
            // Remove expired session
            _sessionsByToken.TryRemove(session.SessionToken, out _);
            _sessionsByDeviceId.TryRemove(deviceId, out _);
            return Task.FromResult<SessionTokenResponse?>(null);
        }
        
        return Task.FromResult(session);
    }

    public Task<SessionTokenResponse?> GetSessionByTokenAsync(string token)
    {
        _sessionsByToken.TryGetValue(token, out var session);
        
        // Check if expired
        if (session != null && session.ExpiresAt <= DateTime.Now)
        {
            // Remove expired session
            if (session.DeviceInfo?.MacAddress != null)
            {
                _sessionsByDeviceId.TryRemove(session.DeviceInfo.MacAddress, out _);
            }
            _sessionsByToken.TryRemove(token, out _);
            return Task.FromResult<SessionTokenResponse?>(null);
        }
        
        return Task.FromResult(session);
    }

    public Task<bool> ClearSessionAsync(string token)
    {
        if (_sessionsByToken.TryGetValue(token, out var session))
        {
            // Remove from both dictionaries
            if (session.DeviceInfo?.MacAddress != null)
            {
                _sessionsByDeviceId.TryRemove(session.DeviceInfo.MacAddress, out _);
            }
            _sessionsByToken.TryRemove(token, out _);
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }

    public Task<int> ClearAllSessionsAsync()
    {
        var count = _sessionsByToken.Count;
        _sessionsByToken.Clear();
        _sessionsByDeviceId.Clear();
        return Task.FromResult(count);
    }
}
