# Tham chiếu NuGet & Build cho Identity Server (.NET 9)

## Build

Thư mục có cả file `.sln` và `.csproj`, nên cần chỉ rõ file khi build:

```powershell
# Cách 1: Build theo project
dotnet build IdentityServerHost.csproj

# Cách 2: Build theo solution
dotnet build IdentityServer.sln
```

# Tham chiếu NuGet cho Identity Server (.NET 9)

Tài liệu này map các package **sai** (thường gây lỗi) sang package **đúng** cho dự án.

## Các lỗi thường gặp và cách sửa

### 1. Health Checks

| Package sai (không tồn tại) | Package đúng |
|-----------------------------|-------------|
| `Microsoft.AspNetCore.Diagnostics.HealthChecks` | Đã tích hợp sẵn trong ASP.NET Core, không cần package riêng |
| `Microsoft.Extensions.Diagnostics.HealthChecks.NpgSql` | `AspNetCore.HealthChecks.NpgSql` |
| `Microsoft.Extensions.Diagnostics.HealthChecks.Redis` | `AspNetCore.HealthChecks.Redis` |
| `Microsoft.Extensions.Diagnostics.HealthChecks.UI.Client` | `AspNetCore.HealthChecks.UI.Client` (nếu cần Health Checks UI) |

**Ví dụ cấu hình trong csproj:**
```xml
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="9.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="9.0.0" />
```

### 2. LDAP

| Package sai | Package đúng |
|-------------|-------------|
| `Novell.Directory.Ldap` (chỉ có 2.2.1, không có 3.6.0) | `Novell.Directory.Ldap.NETStandard` |

**Ví dụ:**
```xml
<PackageReference Include="Novell.Directory.Ldap.NETStandard" Version="3.6.0" />
```

### 3. OpenTelemetry Prometheus

| Yêu cầu | Giải pháp |
|---------|-----------|
| `OpenTelemetry.Exporter.Prometheus.AspNetCore` >= 1.8.0 (stable) | Phiên bản stable 1.8.0 chưa có, dùng **1.8.0-beta.1** hoặc **1.9.0-alpha.1** |

**Ví dụ:**
```xml
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.8.0-beta.1" />
```

## Danh sách package hiện tại của dự án

```xml
<!-- Identity & EF -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.1" />

<!-- IdentityServer4 (Reborn fork) -->
<PackageReference Include="Reborn.IdentityServer4" Version="9.0.0" />
<PackageReference Include="Reborn.IdentityServer4.AspNetIdentity" Version="9.0.0" />
<PackageReference Include="Reborn.IdentityServer4.EntityFramework" Version="9.0.0" />
<!-- ... -->

<!-- Health Checks -->
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="9.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="9.0.0" />

<!-- LDAP -->
<PackageReference Include="Novell.Directory.Ldap.NETStandard" Version="3.6.0" />

<!-- OpenTelemetry -->
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.8.0-beta.1" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.8.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.8.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.8.0" />
```

## Rate Limiting

`AddRateLimiting` / `UseRateLimiting` thường có sẵn trong ASP.NET Core 7+. Nếu gặp lỗi CS1061, có thể:
- Dùng .NET 9 SDK chính thức (không dùng .NET 10 preview)
- Hoặc tạm comment code Rate Limiting trong `Program.cs` (đã comment sẵn)

## Lưu ý

- **AspNetCore.HealthChecks.***: Bộ package từ [AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks), tương thích .NET 9.
- **Novell.Directory.Ldap.NETStandard**: Hỗ trợ .NET Standard 2.0+, .NET 5+, phù hợp với .NET 9.
- **OpenTelemetry**: Nếu cần phiên bản stable, có thể chờ bản 1.9.0 stable hoặc dùng 1.8.0-beta.1.
