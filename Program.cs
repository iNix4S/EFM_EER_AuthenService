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
// Creates a new token ONLY if client doesn't have an active token
// Client MUST provide clientId (unique ID from client-side, e.g. GUID)
app.MapGet("/api/session/create", async (HttpContext httpContext, ISessionService sessionService, string? clientId = null, string? deviceName = null) =>
{
    try
    {
        // Client must provide clientId (unique identifier from client browser/device)
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Results.Ok(K2Response<SessionTokenResponse>.Error(
                400,
                "clientId is required. Please provide a unique client identifier (e.g., GUID generated on client-side). Example: /api/session/create?clientId=12345678-1234-1234-1234-123456789abc"
            ));
        }

        // Use clientId as deviceId
        var deviceId = clientId.Trim();

        // Create device info from HTTP request
        var deviceInfo = new DeviceInfo
        {
            MacAddress = deviceId, // Store clientId in MacAddress field
            DeviceName = deviceName ?? DeviceInfoExtractor.GetDeviceName(httpContext),
            IpAddress = DeviceInfoExtractor.GetClientIpAddress(httpContext),
            RealIpAddress = DeviceInfoExtractor.GetRealIpAddress(httpContext),
            UserAgent = DeviceInfoExtractor.GetUserAgent(httpContext),
            IsVpnConnection = DeviceInfoExtractor.IsVpnConnection(httpContext),
            RegisteredAt = DateTime.Now,
            LastConnectedAt = DateTime.Now,
            Status = "Active"
        };

        // Create new session token (only if client doesn't have active token)
        var response = await sessionService.CreateSessionAsync(deviceId, deviceName, deviceInfo);

        return Results.Ok(K2Response<SessionTokenResponse>.Success(
            response,
            "New session token created successfully. Store this token in K2 SmartObject for future requests. Cannot create new token until this one is cleared."
        ));
    }
    catch (InvalidOperationException ex)
    {
        // Client already has an active token
        return Results.Ok(K2Response<SessionTokenResponse>.Error(1, ex.Message));
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<SessionTokenResponse>.Error(2, $"Error: {ex.Message}"));
    }
})
.WithName("CreateSessionToken")
.WithTags("Session Management")
.WithDescription("Create session token for K2 SmartObject. REQUIRED: clientId (unique identifier from client browser/device). Can only create ONE token per client - must clear existing token before creating new one. Example: GET /api/session/create?clientId=12345678-1234-1234-1234-123456789abc&deviceName=My Computer");

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
// Support both DELETE (REST standard) and GET (browser friendly)
app.MapMethods("/api/session/clear", new[] { "DELETE", "GET" }, async (string token, ISessionService sessionService) =>
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
.WithDescription("Clear specific session token (?token=xxx). Supports both DELETE and GET methods. Use this to logout or revoke a session.");

// Clear All Sessions (for development/testing)
// Support both DELETE (REST standard) and GET (browser friendly)
app.MapMethods("/api/session/clear-all", new[] { "DELETE", "GET" }, async (ISessionService sessionService) =>
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
.WithDescription("Clear all session tokens (for development/testing only). Supports both DELETE and GET methods. Use with caution.");

#endregion

app.Run();
