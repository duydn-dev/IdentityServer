using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Operational;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

/// <summary>API quản trị thu hồi token/session theo subject hoặc client.</summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize]
[SecurityHeaders]
public class RevocationController : ControllerBase
{
    private readonly IdentityServerHost.Services.Operational.IPersistedGrantService _persistedGrantService;
    private readonly IAuditService _auditService;

    public RevocationController(IdentityServerHost.Services.Operational.IPersistedGrantService persistedGrantService, IAuditService auditService)
    {
        _persistedGrantService = persistedGrantService;
        _auditService = auditService;
    }

    /// <summary>Thu hồi tất cả grants của một user (subject) với một client cụ thể.</summary>
    [HttpPost("by-subject-client")]
    public async Task<IActionResult> RevokeBySubjectClient([FromBody] RevokeBySubjectClientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectId) || string.IsNullOrWhiteSpace(request.ClientId))
            return BadRequest(new { error = "SubjectId và ClientId là bắt buộc." });

        var count = await _persistedGrantService.RevokeBySubjectClientAsync(request.SubjectId, request.ClientId);
        await _auditService.LogAsync("Token.RevokeBySubjectClient", "PersistedGrant", null,
            $"SubjectId={request.SubjectId}, ClientId={request.ClientId}, Count={count}", true);
        return Ok(new { revoked = count });
    }

    /// <summary>Thu hồi grant theo key.</summary>
    [HttpPost("by-key")]
    public async Task<IActionResult> RevokeByKey([FromBody] RevokeByKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return BadRequest(new { error = "Key là bắt buộc." });

        var success = await _persistedGrantService.RevokeAsync(request.Key);
        if (success)
            await _auditService.LogAsync("Token.RevokeByKey", "PersistedGrant", request.Key, null, true);
        return success ? Ok(new { revoked = true }) : NotFound(new { error = "Grant không tồn tại." });
    }
}

public class RevokeBySubjectClientRequest
{
    public string SubjectId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}

public class RevokeByKeyRequest
{
    public string Key { get; set; } = string.Empty;
}
