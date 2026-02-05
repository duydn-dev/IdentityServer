namespace IdentityServerHost.Constants;

/// <summary>
/// Các role constants trong hệ thống
/// </summary>
public static class Roles
{
    /// <summary>
    /// Quản trị viên - có toàn quyền trên hệ thống
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// Người dùng thông thường - chỉ có quyền cơ bản
    /// </summary>
    public const string Member = "member";

    /// <summary>
    /// Tất cả roles có thể gán cho user
    /// </summary>
    public static readonly string[] AllRoles = { Admin, Member };
}
