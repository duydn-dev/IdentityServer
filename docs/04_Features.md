# Tính năng đã bổ sung – DTI Identity Server

Tài liệu liệt kê các tính năng đã được triển khai trong dự án Identity Server.

---

## 1. Bảo mật & tuân thủ

### 1.1 Audit Logging
- **Service**: `IAuditService` / `AuditService`
- **Database**: Bảng `AuditLogs` (AuditDbContext)
- **Chức năng**: Ghi log mọi thao tác Create/Edit/Delete trên:
  - Users, Roles
  - Clients, ApiResources, ApiScopes, IdentityResources
- **UI**: `Views/AuditLogs/Index.cshtml` – xem lịch sử audit

### 1.2 MFA bắt buộc (RequireMfa)
- **Cấu hình**: `appsettings.json` → `AccountOptions:RequireMfa`
- **Hành vi**: Khi bật, user chưa kích hoạt 2FA sẽ bị chặn đăng nhập
- **Luồng**: Login → kiểm tra 2FA → chặn hoặc cho phép

### 1.3 Chính sách mật khẩu (Password Policy)
- **Cấu hình**: `appsettings.json` → `PasswordPolicy`
- **Tùy chọn**: MinLength, RequireDigit, RequireLowercase, RequireUppercase, RequireNonAlphanumeric
- **Service**: `PasswordPolicyValidator` – validate khi đổi mật khẩu / đăng ký

### 1.4 IP Whitelist
- **Cấu hình**: `appsettings.json` → `IpWhitelist`
- **Middleware**: `IpWhitelistMiddleware` – chặn request từ IP không trong danh sách
- **Hỗ trợ**: AllowedIps, AllowedCidrs

### 1.5 Security Headers (CSP)
- **Extension**: `ConfigureCspAllowHeaders` – Content Security Policy
- **IdentityServer**: Csp.Level = Two

---

## 2. Quản lý token & session

### 2.1 Revocation API
- **Controller**: `RevocationController`
- **Endpoints**:
  - `POST /api/admin/Revocation/by-subject-client` – thu hồi theo subject + client
  - `POST /api/admin/Revocation/by-key` – thu hồi theo key
- **Service**: `IPersistedGrantService` / `PersistedGrantService`

### 2.2 Refresh Token Rotation
- **Cấu hình**: `RefreshTokenUsage = OneTimeOnly`, `RefreshTokenExpiration = Sliding`
- **Áp dụng**: Clients có `AllowOfflineAccess` trong `Configuration/ClientsWeb.cs`, `ClientsConsole.cs`

### 2.3 Quản lý Persisted Grants
- **Controller**: `PersistedGrantsController`
- **UI**: `Views/PersistedGrants/Index.cshtml` – danh sách grants, phân trang

### 2.4 Quản lý Device Codes
- **Controller**: `DeviceCodesController`
- **Service**: `IDeviceCodeService` / `DeviceCodeService`
- **UI**: `Views/DeviceCodes/Index.cshtml`

### 2.5 Quản lý Session
- **Controller**: `SessionsController`
- **Service**: `ISessionService` / `SessionService`
- **UI**: `Views/Sessions/Index.cshtml` – danh sách session đang hoạt động

---

## 3. Mở rộng & hiệu năng

### 3.1 Redis Cache
- **Cấu hình**: `ConnectionStrings:Redis`
- **Package**: `StackExchange.Redis`, `Microsoft.Extensions.Caching.StackExchangeRedis`
- **Sử dụng**: `AddStackExchangeRedisCache`, `AddConfigurationStoreCache`

### 3.2 Health Checks
- **Endpoints**: `/health`
- **Kiểm tra**:
  - PostgreSQL (AddNpgSql)
  - ApplicationDbContext (AddDbContextCheck)
  - Redis (AddRedis, nếu cấu hình)
- **Package**: `AspNetCore.HealthChecks.NpgSql`, `AspNetCore.HealthChecks.Redis`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

### 3.3 OpenTelemetry & Prometheus Metrics
- **Endpoint**: `/metrics` – Prometheus scraping
- **Package**: `OpenTelemetry.Exporter.Prometheus.AspNetCore`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`
- **Resource**: Service name `IdentityServerHost`

### 3.4 Rate Limiting (tạm tắt)
- **Trạng thái**: Đã comment trong `Program.cs` do tương thích SDK
- **Ghi chú**: Có thể bật lại khi dùng .NET 9 SDK chính thức

---

## 4. Tích hợp & đồng bộ user

### 4.1 LDAP / Active Directory
- **Service**: `ILdapService` / `LdapService`
- **Cấu hình**: `appsettings.json` → `Ldap`
  - Enabled, Server, Port, BaseDn
  - BindDn, BindPassword (service account)
  - UserFilter
- **Luồng**: Login → thử LDAP trước → nếu thành công, tìm/tạo user trong DB
- **Package**: `Novell.Directory.Ldap.NETStandard`

### 4.2 SCIM 2.0
- **Controller**: `ScimController`
- **Resources**: Users, Groups (map tới Roles)
- **Thao tác**: GET, POST, PUT, DELETE
- **Filter**: userName, emails.value, displayName (eq, co, sw, ew)
- **Xác thực**: API Key qua header, cấu hình `Scim:ApiKey`
- **Attribute**: `ScimAuthorizeAttribute`

---

## 5. Xác thực hai yếu tố (2FA)

### 5.1 Luồng đăng nhập 2FA
- **AccountController**: Login → LoginWith2fa → LoginWithRecoveryCode → Lockout
- **Views**: `LoginWith2fa.cshtml`, `LoginWithRecoveryCode.cshtml`, `Lockout.cshtml`

### 5.2 Quản lý 2FA (Manage)
- **Controller**: `ManageController`
- **Actions**: Index, EnableAuthenticator, GenerateRecoveryCodes, Disable2fa
- **Views**: `Views/Manage/` – Index, EnableAuthenticator, GenerateRecoveryCodes, Disable2fa

---

## 6. Giám sát & cảnh báo

### 6.1 Alerting Service
- **Service**: `IAlertService` / `AlertService`
- **Chức năng**: Ghi nhận và truy xuất cảnh báo gần đây

### 6.2 IdentityServer Event Sink
- **Class**: `IdentityServerEventSink` – implement `IEventSink`
- **Chức năng**: Lắng nghe sự kiện IdentityServer (đăng nhập thất bại, lỗi) → tạo alert
- **Đăng ký**: `AddTransient<IEventSink, IdentityServerEventSink>`

### 6.3 Alerts Dashboard
- **Controller**: `AlertsController`
- **UI**: `Views/Alerts/Index.cshtml` – danh sách cảnh báo
- **Link**: Có trên sidebar / dashboard

---

## 7. Quản trị Identity (ASP.NET Identity)

### 7.1 Users
- **Controller**: `UsersController`
- **Service**: `IUserService` / `UserService`
- **UI**: Index (phân trang), Create, Edit
- **Chức năng**: CRUD, gán role, audit

### 7.2 Roles
- **Controller**: `RolesController`
- **Service**: `IRoleService` / `RoleService`
- **UI**: Index (phân trang), Create, Edit
- **Chức năng**: CRUD, audit

---

## 8. Quản trị IdentityServer Configuration

### 8.1 Clients
- **Controller**: `ClientsController`
- **Service**: `IClientConfigService` / `ClientConfigService`
- **UI**: Index (phân trang), Create, Edit
- **Chức năng**: CRUD, AllowedScopes, RedirectUris, PostLogoutRedirectUris, ClientSecrets, audit

### 8.2 API Resources
- **Controller**: `ApiResourcesController`
- **Service**: `IApiResourceConfigService` / `ApiResourceConfigService`
- **UI**: Index, Create, Edit

### 8.3 API Scopes
- **Controller**: `ApiScopesController`
- **Service**: `IApiScopeConfigService` / `ApiScopeConfigService`
- **UI**: Index, Create, Edit

### 8.4 Identity Resources
- **Controller**: `IdentityResourcesController`
- **Service**: `IIdentityResourceConfigService` / `IdentityResourceConfigService`
- **UI**: Index, Create, Edit

---

## 9. Luồng OAuth/OIDC chuẩn

### 9.1 Account
- Login, Logout, Register, ForgotPassword, ResetPassword
- External login (Google, Facebook...)
- **Controller**: `AccountController`, `ExternalController`

### 9.2 Consent
- **Controller**: `ConsentController`
- **UI**: `Views/Consent/Index.cshtml`

### 9.3 Device Flow
- **Controller**: `DeviceController`
- **UI**: UserCodeCapture, UserCodeConfirmation, Success

### 9.4 Grants
- **Controller**: `GrantsController`
- **UI**: `Views/Grants/Index.cshtml` – danh sách consent đã cấp

### 9.5 Diagnostics
- **Controller**: `DiagnosticsController`
- **UI**: `Views/Diagnostics/Index.cshtml` – thông tin token/claims

---

## 10. Dashboard & thống kê

### 10.1 Home / Dashboard
- **Controller**: `HomeController`
- **UI**: `Views/Home/Index.cshtml`
- **Service**: `IStatsService` / `StatsService`
- **Nội dung**: Số lượng Users, Roles, Clients, ApiResources, ApiScopes, IdentityResources
- **Biểu đồ**: AMCharts (nếu có)

---

## 11. Cấu hình & tiện ích

### 11.1 Serilog
- Console, File (Logs/log-.txt, rolling daily)
- Đọc cấu hình từ `appsettings.json`

### 11.2 Build scripts
- `build.ps1`, `build.bat` – build solution
- Dùng khi chạy `dotnet build` gặp lỗi MSB1011

### 11.3 Mutual TLS
- **IdentityServer**: `MutualTls.Enabled = true`, DomainName = "mtls"
- **AddMutualTlsSecretValidators**

---

## Tóm tắt cấu hình appsettings.json

| Section        | Mô tả                          |
|----------------|---------------------------------|
| ConnectionStrings | DefaultConnection, Redis     |
| IpWhitelist    | Enabled, AllowedIps, AllowedCidrs |
| Ldap           | Enabled, Server, Port, BaseDn, BindDn, BindPassword, UserFilter |
| AccountOptions | RequireMfa                     |
| Scim           | ApiKey, Enabled               |
| PasswordPolicy | MinLength, RequireDigit, ...   |

---

## Tài liệu liên quan

- `01_MIGRATION.md` – Hướng dẫn migration
- `02_Database` – Cấu trúc database
- `03_NuGet_Packages.md` – Tham chiếu NuGet & build
