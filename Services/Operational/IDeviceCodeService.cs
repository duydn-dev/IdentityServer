using IdentityServer4.EntityFramework.Entities;

namespace IdentityServerHost.Services.Operational;

public interface IDeviceCodeService
{
    Task<(IEnumerable<DeviceFlowCodes> Items, int Total)> GetPagedAsync(int page, int pageSize, string? userCode, string? clientId);
    Task<bool> RemoveAsync(string userCode);
}
