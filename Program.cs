using EXAT_EFM_EER_AuthenService.Models;
using EXAT_EFM_EER_AuthenService.Helpers;
using EXAT_EFM_EER_AuthenService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Register Session Service
builder.Services.AddSingleton<ISessionService, SessionService>();

// Add CORS for K2 SmartObject
builder.Services.AddCors(options =>
{
    options.AddPolicy("K2Policy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Comment out HTTPS redirection for development
// app.UseHttpsRedirection();

app.UseCors("K2Policy");

#region Session Management APIs for K2 SmartObject

// Generate Session Token (for K2 SmartObject - no MAC Address needed)
// Token will be the same for same deviceId (one token per device)
// Device ID is automatically generated from client IP + User-Agent
app.MapGet("/api/session/create", async (HttpContext httpContext, ISessionService sessionService, string? deviceName = null) =>
{
    try
    {
        // Auto-generate device ID from client information (IP + User-Agent)
        var deviceId = DeviceInfoExtractor.GetUniqueDeviceId(httpContext);

        // Create device info from HTTP request
        var deviceInfo = new DeviceInfo
        {
            MacAddress = deviceId, // Store deviceId in MacAddress field
            DeviceName = deviceName ?? DeviceInfoExtractor.GetDeviceName(httpContext),
            IpAddress = DeviceInfoExtractor.GetClientIpAddress(httpContext),
            RealIpAddress = DeviceInfoExtractor.GetRealIpAddress(httpContext),
            UserAgent = DeviceInfoExtractor.GetUserAgent(httpContext),
            IsVpnConnection = DeviceInfoExtractor.IsVpnConnection(httpContext),
            RegisteredAt = DateTime.Now,
            LastConnectedAt = DateTime.Now,
            Status = "Active"
        };

        // Get or create session token (same token for same deviceId)
        var response = await sessionService.CreateSessionAsync(deviceId, deviceName, deviceInfo);

        var message = response.DeviceInfo?.RegisteredAt.AddSeconds(2) > DateTime.Now
            ? "New session token created successfully. Store this token in K2 SmartObject for future requests."
            : "Existing session token returned. Same device returns same token until expiration.";

        return Results.Ok(K2Response<SessionTokenResponse>.Success(
            response,
            message
        ));
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<SessionTokenResponse>.Error(1, $"Error: {ex.Message}"));
    }
})
.WithName("CreateSessionToken")
.WithTags("Session Management")
.WithDescription("Create session token for K2 SmartObject (no parameters needed). Device ID is automatically generated from client IP and User-Agent. Same device will return same token until expiration (configurable in appsettings.json). Example: GET /api/session/create or GET /api/session/create?deviceName=My Computer");

// Validate with Session Token (for K2 SmartObject)
app.MapGet("/api/session/validate", (HttpContext httpContext, string token) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Ok(K2Response<object>.Error(
                400,
                "Session token is required (?token=xxx)"
            ));
        }

        var deviceInfo = new
        {
            SessionToken = token,
            ClientIp = DeviceInfoExtractor.GetClientIpAddress(httpContext),
            RealIp = DeviceInfoExtractor.GetRealIpAddress(httpContext),
            UserAgent = DeviceInfoExtractor.GetUserAgent(httpContext),
            DeviceName = DeviceInfoExtractor.GetDeviceName(httpContext),
            IsVpnConnection = DeviceInfoExtractor.IsVpnConnection(httpContext),
            ValidatedAt = DateTime.Now
        };

        return Results.Ok(K2Response<object>.Success(
            deviceInfo,
            "Session validated successfully"
        ));
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<object>.Error(1, $"Error: {ex.Message}"));
    }
})
.WithName("ValidateSessionToken")
.WithTags("Session Management")
.WithDescription("Validate session token from K2 SmartObject (?token=xxx). Returns current device info and validation timestamp.");

// Clear Session Token
app.MapDelete("/api/session/clear", async (string token, ISessionService sessionService) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.Ok(K2Response<object>.Error(
                400,
                "Session token is required (?token=xxx)"
            ));
        }

        var result = await sessionService.ClearSessionAsync(token);
        
        if (result)
        {
            return Results.Ok(K2Response<object>.Success(
                new { Token = token, ClearedAt = DateTime.Now },
                "Session token cleared successfully"
            ));
        }
        else
        {
            return Results.Ok(K2Response<object>.Error(
                404,
                "Session token not found or already expired"
            ));
        }
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<object>.Error(1, $"Error: {ex.Message}"));
    }
})
.WithName("ClearSessionToken")
.WithTags("Session Management")
.WithDescription("Clear specific session token (?token=xxx). Use this to logout or revoke a session.");

// Clear All Sessions (for development/testing)
app.MapDelete("/api/session/clear-all", async (ISessionService sessionService) =>
{
    try
    {
        var count = await sessionService.ClearAllSessionsAsync();
        
        return Results.Ok(K2Response<object>.Success(
            new { ClearedCount = count, ClearedAt = DateTime.Now },
            $"Cleared {count} session token(s) successfully"
        ));
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<object>.Error(1, $"Error: {ex.Message}"));
    }
})
.WithName("ClearAllSessions")
.WithTags("Session Management")
.WithDescription("Clear all session tokens (for development/testing only). Use with caution.");

#endregion

app.Run();
