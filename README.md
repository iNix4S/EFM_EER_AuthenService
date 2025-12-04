# EXAT EFM EER Authentication Service

## ภาพรวมโปรเจกต์
โปรเจกต์ Web API สำหรับระบบ Session Token Management ที่พัฒนาด้วย .NET 9.0 โดยรองรับการทำงานร่วมกับ K2 SmartObject พร้อมดึงข้อมูล Server Hardware (Hostname, MAC Address, Network Interfaces) มาแสดงใน Response

## เทคโนโลยีที่ใช้
- **.NET 9.0** - Framework หลัก
- **ASP.NET Core Minimal APIs** - สำหรับสร้าง RESTful API
- **Swashbuckle.AspNetCore** - สำหรับ Swagger UI Documentation
- **C# 12** - ภาษาโปรแกรมมิ่ง
- **In-Memory Storage** - ConcurrentDictionary สำหรับเก็บ Session Token

## โครงสร้างโปรเจกต์
```
EXAT_EFM_EER_AuthenService/
├── Models/
│   ├── K2Response.cs          # K2 SmartObject Response Models
│   └── DeviceInfo.cs          # Device และ Session Models
├── Services/
│   └── DeviceService.cs       # Session Token Management Service
├── Helpers/
│   ├── DeviceInfoExtractor.cs # Device Information Extraction Utilities
│   └── ClientDeviceInfo.cs    # Server Hardware Information Retrieval
├── Properties/
│   └── launchSettings.json    # Launch configuration
├── chat-log/
│   └── chat-log-*.md          # Development chat logs
├── Program.cs                  # Main entry point & API Endpoints
├── appsettings.json           # Configuration (Token Expiration)
├── EXAT_EFM_EER_AuthenService.http  # HTTP Request Tests
└── README.md                   # เอกสารนี้
```

## K2 SmartObject Response Structure

### 1. K2Response<T> - สำหรับข้อมูลเดี่ยว
```json
{
  "statusCode": 0,
  "message": "Success",
  "data": {
    // ข้อมูลที่ต้องการส่งกลับ
  },
  "totalRecords": null,
  "metadata": null
}
```

### 2. K2ListResponse<T> - สำหรับรายการข้อมูล (รองรับ Pagination)
```json
{
  "statusCode": 0,
  "message": "Success",
  "items": [
    // รายการข้อมูล
  ],
  "totalRecords": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

### Status Code
- **0** = Success (สำเร็จ)
- **1** = Error (ข้อผิดพลาดทั่วไป)
- **400** = Bad Request (ข้อมูลไม่ถูกต้อง)
- **404** = Not Found (ไม่พบข้อมูล)
- **อื่นๆ** = Custom error codes ตามความต้องการ

## API Endpoints

### 📌 Session Management APIs

#### 1. สร้าง/ดึง Session Token
```
GET /api/session/create?clientId={required}
```

**คุณสมบัติ:**
- **ต้องส่ง `clientId`** (REQUIRED) - Unique Identifier จาก Client (เช่น GUID ที่สร้างใน Browser)
- **Idempotent** - เรียกซ้ำได้ โดยจะคืนค่า Session เดิมถ้ายังไม่หมดอายุ
- อายุ Token กำหนดใน `appsettings.json` (default: 24 ชั่วโมง)
- รองรับ VPN Detection
- **รวมข้อมูล Server Hardware** - Hostname, MAC Address, Network Interfaces

**Parameters:**
- `clientId` (required) - Unique Client Identifier (เช่น GUID จาก `crypto.randomUUID()`)

**K2 SmartObject Implementation:**
```javascript
// สร้าง/ดึง Client ID จาก localStorage
var clientId = localStorage.getItem('k2_client_id');
if (!clientId) {
    clientId = crypto.randomUUID();
    localStorage.setItem('k2_client_id', clientId);
}
// เรียก API
GET /api/session/create?clientId={clientId}
```

**Response (Success - New Session):**
```json
{
  "statusCode": 0,
  "message": "Session token retrieved successfully...",
  "data": {
    "sessionToken": "e4f5a6b7-c8d9-e0f1-a2b3-c4d5e6f7a8b9",
    "expiresAt": "2025-12-02T10:30:00Z",
    "isNewSession": true,
    "deviceInfo": {
      "ipAddress": "192.168.1.100",
      "realIpAddress": "203.154.1.1",
      "userAgent": "Mozilla/5.0...",
      "sessionToken": "e4f5a6b7-c8d9-e0f1-a2b3-c4d5e6f7a8b9",
      "isVpnConnection": false,
      "registeredAt": "2025-12-01T10:30:00",
      "lastConnectedAt": "2025-12-01T10:30:00",
      "status": "Active"
    },
    "serverDeviceInfo": {
      "hostname": "DESKTOP-NK8KGKO",
      "primaryMacAddress": "E8-80-88-54-6C-08",
      "networkInterfaces": [
        {
          "name": "Ethernet",
          "description": "Intel(R) Ethernet Connection (16) I219-V",
          "type": "Ethernet",
          "status": "Up",
          "macAddress": "E8-80-88-54-6C-08",
          "isActive": true,
          "dhcpEnabled": true,
          "ipv4Addresses": ["192.168.1.100"],
          "ipv6Addresses": ["fe80::a1b2:c3d4:e5f6:7890"],
          "subnetMasks": ["255.255.255.0"],
          "defaultGateways": ["192.168.1.1"],
          "dnsServers": ["8.8.8.8", "8.8.4.4"],
          "dnsSuffix": "lan"
        }
      ],
      "retrievedAt": "2025-12-01T10:30:00"
    }
  }
}
```

**Response (Success - Existing Session):**
```json
{
  "statusCode": 0,
  "message": "Session token retrieved successfully...",
  "data": {
    "sessionToken": "e4f5a6b7-c8d9-e0f1-a2b3-c4d5e6f7a8b9",
    "expiresAt": "2025-12-02T10:30:00Z",
    "isNewSession": false,
    "deviceInfo": { ... },
    "serverDeviceInfo": { ... }
  }
}
```

**Response (Error - No clientId):**
```json
{
  "statusCode": 400,
  "message": "clientId is required. Please provide a unique client identifier (e.g., GUID generated on client-side). Example: /api/session/create?clientId=12345678-1234-1234-1234-123456789abc",
  "data": null
}
```

#### 2. ตรวจสอบ Session Token
```
GET /api/session/validate?token=xxx
```

**Parameters:**
- `token` (required) - Session Token ที่ต้องการตรวจสอบ

**Response:**
```json
{
  "statusCode": 0,
  "message": "Session validated successfully",
  "data": {
    "sessionToken": "e4f5a6b7-c8d9-e0f1-a2b3-c4d5e6f7a8b9",
    "clientIp": "192.168.1.100",
    "realIp": "203.154.1.1",
    "userAgent": "Mozilla/5.0...",
    "deviceName": "Windows (Edge)",
    "isVpnConnection": false,
    "validatedAt": "2025-11-26T10:30:00"
  }
}
```

#### 3. ลบ Session Token (Logout)
```
GET /api/session/clear?token=xxx
DELETE /api/session/clear?token=xxx
```

**คุณสมบัติ:**
- รองรับทั้ง GET และ DELETE method
- GET method สำหรับเรียกจาก Browser หรือ K2 SmartObject
- DELETE method เป็น REST standard

**Parameters:**
- `token` (required) - Token ที่ต้องการลบ

**Response (Success):**
```json
{
  "statusCode": 0,
  "message": "Session token cleared successfully",
  "data": {
    "token": "e4f5a6b7-c8d9-e0f1-a2b3-c4d5e6f7a8b9",
    "clearedAt": "2025-11-26T10:30:00"
  }
}
```

**Response (Error - Not Found):**
```json
{
  "statusCode": 404,
  "message": "Session token not found or already expired",
  "data": null
}
```

#### 4. ลบ Token ทั้งหมด (Clear All)
```
GET /api/session/clear-all
DELETE /api/session/clear-all
```

**คุณสมบัติ:**
- รองรับทั้ง GET และ DELETE method
- ใช้สำหรับ Development/Testing
- ลบ Token ทั้งหมดในระบบพร้อมกัน

**Response:**
```json
{
  "statusCode": 0,
  "message": "Cleared 5 session token(s) successfully",
  "data": {
    "clearedCount": 5,
    "clearedAt": "2025-12-01T10:30:00"
  }
}
```

#### 5. ดึงข้อมูล Server Device
```
GET /api/device/info
```

**คุณสมบัติ:**
- ดึงข้อมูล Hostname และ Network Interfaces ของเซิร์ฟเวอร์
- แสดงเฉพาะ Network Interfaces ที่ Active (Status = Up)
- รวมข้อมูล IP, MAC Address, DNS, Gateway

**Response:**
```json
{
  "statusCode": 0,
  "message": "Device information retrieved successfully...",
  "data": {
    "hostname": "DESKTOP-NK8KGKO",
    "primaryMacAddress": "E8-80-88-54-6C-08",
    "networkInterfaces": [
      {
        "name": "Ethernet",
        "description": "Intel(R) Ethernet Connection...",
        "type": "Ethernet",
        "status": "Up",
        "macAddress": "E8-80-88-54-6C-08",
        "isActive": true,
        "dhcpEnabled": true,
        "ipv4Addresses": ["192.168.1.100"],
        "ipv6Addresses": ["fe80::..."],
        "subnetMasks": ["255.255.255.0"],
        "defaultGateways": ["192.168.1.1"],
        "dnsServers": ["8.8.8.8"],
        "dnsSuffix": "lan"
      }
    ],
    "totalInterfaces": 1,
    "activeInterfaces": 1,
    "retrievedAt": "2025-12-01T10:30:00"
  }
}
```

## Configuration (appsettings.json)

### Token Expiration Settings
```json
{
  "SessionSettings": {
    "TokenExpirationHours": 24
  }
}
```

**Parameters:**
- `TokenExpirationHours` - อายุของ Session Token (ชั่วโมง)
  - Default: 24 ชั่วโมง
  - แนะนำ: 1-168 ชั่วโมง (1 ชม. - 7 วัน)

## Device ID Generation

ระบบใช้วิธีการสร้าง Device ID อัตโนมัติดังนี้:

1. **รวบรวมข้อมูล Client:**
   - IP Address (รองรับ VPN Detection จาก X-Forwarded-For Header)
   - User-Agent (Browser/Device Information)

2. **สร้าง Unique ID:**
   - ใช้ SHA-256 Hash จาก `{IP}_{UserAgent}`
   - ตัด Hash เป็น 16 ตัวอักษรแรก (Hex)
   - เครื่องเดียวกัน (IP + Browser เดียวกัน) = Device ID เดียวกัน

3. **Token Reuse:**
   - Device ID เดียวกันจะได้ Token เดิมจนกว่าจะหมดอายุ
   - ประหยัดการสร้าง Token ซ้ำซ้อน

## การติดตั้งและรันโปรเจกต์

### ข้อกำหนดเบื้องต้น
- .NET 9.0 SDK หรือสูงกว่า
- Visual Studio Code หรือ Visual Studio 2022

### วิธีการรัน

1. **เปิดโปรเจกต์**
```powershell
cd EXAT_EFM_EER_AuthenService
```

2. **Restore dependencies**
```powershell
dotnet restore
```

3. **Build โปรเจกต์**
```powershell
dotnet build
```

4. **รันโปรเจกต์**
```powershell
dotnet run
```

5. **เข้าถึง API**
   - HTTP: http://localhost:5185
   - OpenAPI JSON: http://localhost:5185/openapi/v1.json
   - Swagger UI (interactive docs): http://localhost:5185/swagger/index.html
   - OpenAPI Spec (alternative): http://localhost:5185/swagger/v1/swagger.json

### ทดสอบด้วย HTTP Request File
เปิดไฟล์ `EXAT_EFM_EER_AuthenService.http` และคลิก "Send Request" เพื่อทดสอบแต่ละ API

### ทดสอบด้วย PowerShell
```powershell
# สร้าง Token
$result = Invoke-RestMethod -Uri "http://localhost:5185/api/session/create" -Method GET
$token = $result.data.sessionToken

# ตรวจสอบ Token
Invoke-RestMethod -Uri "http://localhost:5185/api/session/validate?token=$token" -Method GET

# ดึงข้อมูล Server Device
Invoke-RestMethod -Uri "http://localhost:5185/api/device/info" -Method GET

# ลบ Token
Invoke-RestMethod -Uri "http://localhost:5185/api/session/clear?token=$token" -Method DELETE
```
Invoke-RestMethod -Uri "http://localhost:5185/api/session/validate?token=$token" -Method GET

# ลบ Token
Invoke-RestMethod -Uri "http://localhost:5185/api/session/clear?token=$token" -Method DELETE
```

## CORS Configuration
โปรเจกต์มีการตั้งค่า CORS Policy ชื่อ "K2Policy" สำหรับรองรับการเรียกใช้จาก K2 SmartObject โดย:
- อนุญาตทุก Origin (`AllowAnyOrigin`)
- อนุญาตทุก HTTP Method (`AllowAnyMethod`)
- อนุญาตทุก Header (`AllowAnyHeader`)

**หมายเหตุ:** ในระบบ Production ควรกำหนด Origin ที่ชัดเจนเพื่อความปลอดภัย

## Architecture & Design

### Session Storage
- **In-Memory Storage**: ใช้ `ConcurrentDictionary` เก็บ Session Token
- **Dual-Key Storage**: 
  - `_sessionsByDeviceId` - ค้นหา Token จาก Device ID
  - `_sessionsByToken` - ตรวจสอบ Token
- **⚠️ Production Note**: ควรเปลี่ยนเป็น Database (Redis, SQL Server, etc.)

### Services Architecture
```
ISessionService (Interface)
  └── SessionService (Implementation)
      ├── CreateSessionAsync()      - สร้าง/ดึง Token
      ├── GetSessionByDeviceIdAsync() - ค้นหาจาก Device ID
      ├── GetSessionByTokenAsync()   - ค้นหาจาก Token
      ├── ClearSessionAsync()        - ลบ Token เฉพาะ
      └── ClearAllSessionsAsync()    - ลบ Token ทั้งหมด
```

### Helper Utilities
**DeviceInfoExtractor** - ดึงข้อมูลจาก HTTP Request:
- `GetClientIpAddress()` - IP Address
- `GetRealIpAddress()` - Real IP (VPN Support)
- `GetUserAgent()` - User-Agent String
- `GetDeviceName()` - Device Type จาก User-Agent
- `GetUniqueDeviceId()` - Generate Device ID (SHA-256)
- `IsVpnConnection()` - ตรวจจับ VPN

## Security Considerations

### ⚠️ สิ่งที่ควรระวัง
1. **In-Memory Storage**
   - Session จะหายเมื่อ Restart Service
   - ไม่รองรับ Multiple Instances (Load Balancing)
   - แนะนำใช้ Redis หรือ Database ใน Production

2. **CORS Policy**
   - ปัจจุบันอนุญาตทุก Origin
   - Production ควรจำกัด Origin ที่เฉพาะเจาะจง

3. **Device ID Generation**
   - ใช้ IP + User-Agent (สามารถ Spoof ได้)
   - NAT/Proxy จะทำให้หลายเครื่องมี IP เดียวกัน
   - แนะนำเพิ่ม Client-side Fingerprint ใน Production

4. **Token Expiration**
   - ตั้งเวลาให้เหมาะสมกับ Use Case
   - ไม่มี Token Refresh Mechanism

## K2 SmartObject Integration

### การเรียกใช้จาก K2 SmartObject

1. **สร้าง Session Token**
```javascript
// K2 SmartObject - HTTP Service Call
var response = CallHTTPService("GET", "http://localhost:5185/api/session/create");
var token = response.data.sessionToken;
// เก็บ token ไว้ใช้งานต่อ
```

2. **ใช้ Token ในการเรียก API อื่น**
```javascript
// ส่ง Token ใน Query String
var validateUrl = "http://localhost:5185/api/session/validate?token=" + token;
var result = CallHTTPService("GET", validateUrl);
```

3. **Logout (ลบ Token)**
```javascript
var clearUrl = "http://localhost:5185/api/session/clear?token=" + token;
CallHTTPService("DELETE", clearUrl);
```

### Response Format (K2 Compatible)
ทุก Response ใช้ K2Response format เพื่อความสม่ำเสมอ:
- `statusCode: 0` = สำเร็จ
- `statusCode: อื่นๆ` = ผิดพลาด
- `message` = รายละเอียด
- `data` = ข้อมูลผลลัพธ์

## Development & Testing

### HTTP Request Tests
ใช้ไฟล์ `EXAT_EFM_EER_AuthenService.http` สำหรับทดสอบ:
- ทดสอบ 1: สร้าง Token ครั้งแรก
- ทดสอบ 2: เรียกซ้ำ (ควรได้ Token เดิม)
- ทดสอบ 3: ตรวจสอบ Token
- ทดสอบ 4: ลบ Token เฉพาะ
- ทดสอบ 5: ลบ Token ทั้งหมด

### Chat Logs
ดูประวัติการพัฒนาได้ที่ `chat-log/chat-log-*.md`

## Future Enhancements

### แนะนำสำหรับ Production
1. **Database Integration**
   - Entity Framework Core + SQL Server
   - Redis สำหรับ Session Storage

2. **Authentication & Authorization**
   - JWT Token Authentication
   - Role-based Access Control

3. **Logging & Monitoring**
   - Serilog สำหรับ Logging
   - Application Insights

4. **Rate Limiting**
   - ป้องกัน API Abuse

5. **Health Checks**
   - Endpoint สำหรับตรวจสอบสถานะระบบ

## การพัฒนาต่อ

### เพิ่ม Controller ใหม่
1. สร้างโฟลเดอร์ `Controllers` ถ้ายังไม่มี
2. เพิ่มไฟล์ Controller ใหม่
3. ใช้ `K2Response<T>` หรือ `K2ListResponse<T>` สำหรับ Response

### ตัวอย่างการใช้งาน K2Response ใน Endpoint
```csharp
app.MapGet("/api/example", () =>
{
    try
    {
        var data = new { Id = 1, Name = "Example" };
        return Results.Ok(K2Response<object>.Success(data, "Success"));
    }
    catch (Exception ex)
    {
        return Results.Ok(K2Response<object>.Error(1, $"Error: {ex.Message}"));
    }
});
```

## Troubleshooting

### ปัญหาที่พบบ่อย

**1. Port Already in Use**
```powershell
# หา Process ที่ใช้ Port 5185
netstat -ano | findstr :5185
# Kill Process
taskkill /PID <PID> /F
```

**2. CORS Error จาก K2 SmartObject**
- ตรวจสอบว่า K2Policy ถูก Apply แล้ว
- ตรวจสอบ Origin ใน Request Header

**3. Device ID ไม่คงที่**
- ตรวจสอบว่า IP Address ไม่เปลี่ยน (DHCP)
- ตรวจสอบ User-Agent ไม่ถูกแก้ไข

## Performance Considerations

### In-Memory Storage Limitations
- **Memory Usage**: แต่ละ Session ใช้ ~1-2 KB
- **Capacity**: 10,000 Sessions ≈ 10-20 MB RAM
- **Restart**: Session หายทั้งหมดเมื่อ Restart

### Recommendations
1. **< 1,000 Users**: In-Memory OK
2. **1,000-10,000 Users**: พิจารณา Redis
3. **> 10,000 Users**: ใช้ Database + Distributed Cache

## API Documentation

### Swagger/OpenAPI
เข้าถึง API Documentation ที่:
- OpenAPI JSON: http://localhost:5185/openapi/v1.json
- ใช้เครื่องมืออย่าง Swagger UI หรือ Postman เพื่อดู Interactive Documentation

## ประวัติการพัฒนา

### Version 1.0.0 (26/11/2025)
- ✅ สร้างโปรเจกต์ .NET 9.0 Web API
- ✅ เพิ่ม K2 SmartObject Response Models
- ✅ ตั้งค่า CORS Policy สำหรับ K2
- ✅ เพิ่ม Session Token Management APIs
- ✅ Device ID Auto-generation (IP + User-Agent)
- ✅ Token Reuse per Device
- ✅ Configurable Token Expiration
- ✅ VPN Detection Support
- ✅ Clear Session APIs (Logout & Clear All)
- ✅ HTTP Request Test File

### Changes from Initial Design
- ❌ ลบ MAC Address Validation (K2 SmartObject ไม่สามารถส่งได้)
- ❌ ลบ Device Fingerprint (ลดความซับซ้อน)
- ✅ เพิ่ม Auto Device ID Generation แทน
- ✅ เพิ่ม Configuration-based Settings

## Contact & Support

สำหรับคำถามหรือปัญหาในการใช้งาน:
- **Documentation**: ดูที่ `README.md` (ไฟล์นี้)
- **Chat Logs**: ดูประวัติการพัฒนาที่ `chat-log/`
- **HTTP Tests**: ใช้ `EXAT_EFM_EER_AuthenService.http` เพื่อทดสอบ

## ผู้พัฒนา
- EXAT Development Team

## License
Proprietary - EXAT (การทางพิเศษแห่งประเทศไทย)

---
**เอกสารนี้อัพเดทล่าสุด:** 26 พฤศจิกายน 2568
