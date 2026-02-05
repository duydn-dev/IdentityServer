using IdentityServerHost.Attributes;
using IdentityServerHost.Constants;
using IdentityServerHost.Models;
using IdentityServerHost.Services.Audit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers.Api;

/// <summary>
/// API cho external clients để tạo user (chỉ được tạo member)
/// Sử dụng RSA signature để xác thực
/// </summary>
[ApiController]
[Route("api/external/users")]
[ClientKeyAuthorize]
public class ExternalUserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<ExternalUserController> _logger;

    public ExternalUserController(
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        ILogger<ExternalUserController> logger)
    {
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo user mới với role member
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateExternalUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Invalid request", details = ModelState });
        }

        var clientId = User.FindFirst("client_id")?.Value ?? "unknown";

        // Kiểm tra email đã tồn tại chưa
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { error = "User with this email already exists" });
        }

        // Kiểm tra username đã tồn tại chưa
        if (!string.IsNullOrEmpty(request.UserName))
        {
            var existingByUsername = await _userManager.FindByNameAsync(request.UserName);
            if (existingByUsername != null)
            {
                return Conflict(new { error = "User with this username already exists" });
            }
        }

        // Tạo user mới
        var user = new ApplicationUser
        {
            UserName = request.UserName ?? request.Email,
            Email = request.Email,
            EmailConfirmed = request.EmailConfirmed,
            PhoneNumber = request.PhoneNumber,
            PhoneNumberConfirmed = request.PhoneNumberConfirmed
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create user {Email} from client {ClientId}: {Errors}",
                request.Email, clientId, string.Join(", ", result.Errors.Select(e => e.Description)));
            
            return BadRequest(new
            {
                error = "Failed to create user",
                details = result.Errors.Select(e => new { e.Code, e.Description })
            });
        }

        // Chỉ được gán role member
        var roleResult = await _userManager.AddToRoleAsync(user, Roles.Member);
        if (!roleResult.Succeeded)
        {
            _logger.LogWarning("Failed to add member role to user {Email}: {Errors}",
                request.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        // Audit log
        await _auditService.LogAsync(
            "CreateUser",
            "ApplicationUser",
            user.Id.ToString(),
            $"Email: {user.Email}, UserName: {user.UserName}, Role: {Roles.Member}, Source: ExternalAPI, ClientId: {clientId}");

        _logger.LogInformation("User {Email} created by external client {ClientId}", request.Email, clientId);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ExternalUserResponse
        {
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Role = Roles.Member,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Lấy thông tin user theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        if (!Guid.TryParse(id, out var userId))
        {
            return BadRequest(new { error = "Invalid user ID format" });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new ExternalUserResponse
        {
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Role = roles.FirstOrDefault() ?? Roles.Member,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Kiểm tra email đã tồn tại chưa
    /// </summary>
    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        var user = await _userManager.FindByEmailAsync(email);
        return Ok(new { exists = user != null });
    }
}

#region Request/Response Models

public class CreateExternalUserRequest
{
    /// <summary>
    /// Username (optional, defaults to email)
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Email (required)
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Password (required)
    /// </summary>
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(6)]
    public string Password { get; set; } = null!;

    /// <summary>
    /// Số điện thoại (optional)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Email đã xác nhận chưa
    /// </summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>
    /// Số điện thoại đã xác nhận chưa
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; } = false;
}

public class ExternalUserResponse
{
    public string Id { get; set; } = null!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public string Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

#endregion
