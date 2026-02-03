# Hướng dẫn Migration - PostgreSQL

Tài liệu này mô tả các lệnh để chạy Entity Framework Core migrations cho dự án IdentityServer với PostgreSQL.

## Yêu cầu

- .NET 10.0 SDK
- PostgreSQL đã cài đặt và đang chạy
- Chuỗi kết nối đã cấu hình trong `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=5432;Database=DTI_Identity;Username=postgres;Password=YOUR_PASSWORD;"
}
```

## Cài đặt EF Core Tools (nếu chưa có)

```powershell
dotnet tool install --global dotnet-ef
```

Hoặc cập nhật nếu đã cài:

```powershell
dotnet tool update --global dotnet-ef
```

## Di chuyển vào thư mục dự án

```powershell
cd IdentityHost
```

## Các lệnh Migration

### 1. Tạo migration đầu tiên cho ApplicationDbContext (ASP.NET Identity)

```powershell
dotnet ef migrations add InitialIdentity -c ApplicationDbContext -o Data/Migrations
```

### 2. Tạo migration cho PersistedGrantDbContext (IdentityServer Operational Store)

```powershell
dotnet ef migrations add InitialPersistedGrantDb -c PersistedGrantDbContext -o Migrations/PersistedGrantDb
```

### 3. Tạo migration cho ConfigurationDbContext (IdentityServer Configuration Store)

```powershell
dotnet ef migrations add InitialConfigurationDb -c ConfigurationDbContext -o Migrations/ConfigurationDb
```

### 4. Áp dụng tất cả migrations vào database

```powershell
dotnet ef database update -c ApplicationDbContext
dotnet ef database update -c PersistedGrantDbContext
dotnet ef database update -c ConfigurationDbContext
```

Hoặc áp dụng tất cả trong một lệnh (nếu dùng chung database):

```powershell
dotnet ef database update
```

### 5. Xóa migration gần nhất (chưa áp dụng)

```powershell
dotnet ef migrations remove -c ApplicationDbContext
```

### 6. Xem danh sách migrations

```powershell
dotnet ef migrations list -c ApplicationDbContext
```

### 7. Tạo script SQL (không áp dụng trực tiếp)

```powershell
# Script cho ApplicationDbContext
dotnet ef migrations script -c ApplicationDbContext -o scripts/ApplicationDb.sql

# Script cho PersistedGrantDbContext
dotnet ef migrations script -c PersistedGrantDbContext -o scripts/PersistedGrantDb.sql

# Script cho ConfigurationDbContext
dotnet ef migrations script -c ConfigurationDbContext -o scripts/ConfigurationDb.sql
```

### 8. Rollback về migration cụ thể

```powershell
dotnet ef database update PreviousMigrationName -c ApplicationDbContext
```

## Lưu ý quan trọng

1. **MigrationsAssembly**: Đã cấu hình `MigrationsAssembly("IdentityServerHost")` trong `Program.cs` cho ConfigurationStore và OperationalStore để migrations được tạo trong project IdentityServerHost thay vì assembly IdentityServer4.EntityFramework.Storage.

2. **Thứ tự chạy**: Nên chạy migration theo thứ tự: ApplicationDbContext → PersistedGrantDbContext → ConfigurationDbContext.

3. **Tự động migrate khi startup**: Hiện tại `SeedUsers.StartSeedAsync` đã gọi `context.Database.MigrateAsync()` cho ApplicationDbContext. Cần đảm bảo các DbContext của IdentityServer cũng được migrate nếu cần.

## Package Manager Console (Visual Studio)

Nếu sử dụng Visual Studio với Package Manager Console:

```powershell
# Default project: IdentityHost
Add-Migration InitialIdentity -Context ApplicationDbContext -OutputDir Data/Migrations
Add-Migration InitialPersistedGrantDb -Context PersistedGrantDbContext -OutputDir Migrations/PersistedGrantDb
Add-Migration InitialConfigurationDb -Context ConfigurationDbContext -OutputDir Migrations/ConfigurationDb

Update-Database -Context ApplicationDbContext
Update-Database -Context PersistedGrantDbContext
Update-Database -Context ConfigurationDbContext
```

## Tạo database mới (PostgreSQL)

Nếu cần tạo database trước khi chạy migration:

```sql
-- Kết nối vào PostgreSQL và chạy:
CREATE DATABASE "DTI_Identity"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;
```

Hoặc dùng `psql`:

```powershell
psql -U postgres -c "CREATE DATABASE \"DTI_Identity\";"
```
