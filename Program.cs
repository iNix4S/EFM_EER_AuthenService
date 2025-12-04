using EXAT_EFM_EER_AuthenService.Models;
using EXAT_EFM_EER_AuthenService.Helpers;
using EXAT_EFM_EER_AuthenService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Minimal OpenAPI JSON + Swagger UI (Swashbuckle)
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    // keep the existing OpenAPI JSON endpoint
    app.MapOpenApi();

    // serve Swagger UI in development for interactive docs
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Point the UI to the same OpenAPI JSON used by MapOpenApi
        options.SwaggerEndpoint("/openapi/v1.json", "EXAT_EFM_EER_AuthenService | v1");
        options.RoutePrefix = "swagger"; // serve at /swagger
    });
}

// Comment out HTTPS redirection for development
// app.UseHttpsRedirection();

app.UseCors("K2Policy");

#region Session Management APIs for K2 SmartObject

// Generate Session Token (for K2 SmartObject - no MAC Address needed)
// Creates a new token ONLY if client doesn't have an active token
// Client MUST provide clientId (unique ID from client-side, e.g. GUID)
app.MapGet("/api/session/create", async (HttpContext httpContext, ISessionService sessionService, string? clientId = null) =>
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
            MacAddress = deviceId, // Store clientId in MacAddress field (for internal use only, not returned)
            IpAddress = DeviceInfoExtractor.GetClientIpAddress(httpContext),
            RealIpAddress = DeviceInfoExtractor.GetRealIpAddress(httpContext),
            UserAgent = DeviceInfoExtractor.GetUserAgent(httpContext),
            IsVpnConnection = DeviceInfoExtractor.IsVpnConnection(httpContext),
            RegisteredAt = DateTime.Now,
            LastConnectedAt = DateTime.Now,
            Status = "Active"
        };

        // Get server device information (hostname and MAC addresses)
        var serverDeviceInfo = new
        {
            Hostname = ClientDeviceInfo.GetHostname(),
            PrimaryMacAddress = ClientDeviceInfo.GetPrimaryMacAddress(),
            NetworkInterfaces = ClientDeviceInfo.GetNetworkInterfaces(),
            RetrievedAt = DateTime.Now
        };

        // Create or get existing session token
        var response = await sessionService.CreateSessionAsync(deviceId, null, deviceInfo);
        
        // Add server device info to response
        response.ServerDeviceInfo = serverDeviceInfo;

        return Results.Ok(K2Response<SessionTokenResponse>.Success(
            response,
            "Session token retrieved successfully. Store this token in K2 SmartObject for future requests."
        ));
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<SessionTokenResponse>.Error(2, $"Error: {ex.Message}"));
    }
})
.WithName("CreateSessionToken")
.WithTags("Session Management")
.WithDescription("Create session token for K2 SmartObject. REQUIRED: clientId (unique identifier from client browser/device). Device name will be auto-extracted from User-Agent. Can only create ONE token per client - must clear existing token before creating new one. Example: GET /api/session/create?clientId=12345678-1234-1234-1234-123456789abc");

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

#region Client Device Information APIs

// Get client device information (hostname and network interfaces)
// This API must be called FROM the client machine to get its own information
// The client then sends this information to /api/session/create
app.MapGet("/api/device/info", () =>
{
    try
    {
        var hostname = ClientDeviceInfo.GetHostname();
        var networkInterfaces = ClientDeviceInfo.GetNetworkInterfaces();
        var primaryMac = ClientDeviceInfo.GetPrimaryMacAddress();

        var deviceInfo = new
        {
            Hostname = hostname,
            PrimaryMacAddress = primaryMac,
            NetworkInterfaces = networkInterfaces,
            TotalInterfaces = networkInterfaces.Count,
            ActiveInterfaces = networkInterfaces.Count(i => i.IsActive),
            RetrievedAt = DateTime.Now
        };

        return Results.Ok(K2Response<object>.Success(
            deviceInfo,
            "Device information retrieved successfully. NOTE: This is the information from the machine that calls this API (client-side application must call this API from user's machine)."
        ));
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<object>.Error(1, $"Error: {ex.Message}"));
    }
})
.WithName("GetDeviceInfo")
.WithTags("Device Information")
.WithDescription("Get device information (hostname and MAC addresses) from the machine that calls this API. CLIENT APPLICATION must call this endpoint from user's machine to get their device info, then send it to /api/session/create");

#endregion

app.Run();
