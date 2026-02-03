using IdentityServerHost.Attributes;
using IdentityServerHost.Data;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace IdentityServerHost.Controllers;

[ApiController]
[Route("scim/v2")]
[ScimAuthorize]
public class ScimController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public ScimController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    #region Users

    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers([FromQuery] int startIndex = 1, [FromQuery] int count = 100, [FromQuery] string? filter = null)
    {
        var query = _db.Users.AsQueryable();
        query = ApplyUserFilter(query, filter);

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip(Math.Max(0, startIndex - 1))
            .Take(Math.Min(count, 100))
            .Select(u => new ScimUser
            {
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                Id = u.Id.ToString(),
                UserName = u.UserName,
                Emails = u.Email != null ? new[] { new ScimEmail { Value = u.Email, Primary = true } } : Array.Empty<ScimEmail>(),
                Active = !u.LockoutEnabled || (u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.UtcNow),
                Meta = new ScimMeta { ResourceType = "User", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{u.Id}" }
            })
            .ToListAsync();

        return Ok(new ScimListResponse<ScimUser>
        {
            Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            TotalResults = total,
            StartIndex = startIndex,
            ItemsPerPage = count,
            Resources = users
        });
    }

    [HttpGet("Users/{id}")]
    public async Task<IActionResult> GetUser(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound();

        var user = await _db.Users.FindAsync(guid);
        if (user == null) return NotFound();

        return Ok(new ScimUser
        {
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Emails = user.Email != null ? new[] { new ScimEmail { Value = user.Email, Primary = true } } : Array.Empty<ScimEmail>(),
            Active = !user.LockoutEnabled || (user.LockoutEnd == null || user.LockoutEnd < DateTimeOffset.UtcNow),
            Meta = new ScimMeta { ResourceType = "User", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{user.Id}" }
        });
    }

    [HttpPost("Users")]
    public async Task<IActionResult> CreateUser([FromBody] ScimUserCreateRequest request)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Emails?.FirstOrDefault()?.Value,
            EmailConfirmed = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ScimUser
        {
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Emails = user.Email != null ? new[] { new ScimEmail { Value = user.Email, Primary = true } } : Array.Empty<ScimEmail>(),
            Active = true,
            Meta = new ScimMeta { ResourceType = "User", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{user.Id}" }
        });
    }

    [HttpPut("Users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] ScimUserUpdateRequest request)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound();

        var user = await _db.Users.FindAsync(guid);
        if (user == null) return NotFound();

        if (request.Active.HasValue)
            user.LockoutEnabled = !request.Active.Value;
        if (request.Emails?.Any() == true)
            user.Email = request.Emails.First().Value;

        await _db.SaveChangesAsync();
        return Ok(new ScimUser
        {
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            Id = user.Id.ToString(),
            UserName = user.UserName,
            Emails = user.Email != null ? new[] { new ScimEmail { Value = user.Email, Primary = true } } : Array.Empty<ScimEmail>(),
            Active = !user.LockoutEnabled || (user.LockoutEnd == null || user.LockoutEnd < DateTimeOffset.UtcNow),
            Meta = new ScimMeta { ResourceType = "User", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Users/{user.Id}" }
        });
    }

    [HttpDelete("Users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound();

        var user = await _db.Users.FindAsync(guid);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    #endregion

    #region Groups (map to Roles)

    [HttpGet("Groups")]
    public async Task<IActionResult> GetGroups([FromQuery] int startIndex = 1, [FromQuery] int count = 100, [FromQuery] string? filter = null)
    {
        var query = _db.Roles.AsQueryable();
        query = ApplyGroupFilter(query, filter);

        var total = await query.CountAsync();
        var roles = await query
            .OrderBy(r => r.Name)
            .Skip(Math.Max(0, startIndex - 1))
            .Take(Math.Min(count, 100))
            .Select(r => new ScimGroup
            {
                Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
                Id = r.Id.ToString(),
                DisplayName = r.Name ?? "",
                Meta = new ScimMeta { ResourceType = "Group", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{r.Id}" }
            })
            .ToListAsync();

        return Ok(new ScimListResponse<ScimGroup>
        {
            Schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" },
            TotalResults = total,
            StartIndex = startIndex,
            ItemsPerPage = count,
            Resources = roles
        });
    }

    [HttpGet("Groups/{id}")]
    public async Task<IActionResult> GetGroup(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound();

        var role = await _db.Roles.FindAsync(guid);
        if (role == null) return NotFound();

        var memberIds = await _db.UserRoles.Where(ur => ur.RoleId == guid).Select(ur => ur.UserId.ToString()).ToListAsync();

        return Ok(new ScimGroup
        {
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Id = role.Id.ToString(),
            DisplayName = role.Name ?? "",
            Members = memberIds.Select(m => new ScimGroupMember { Value = m }).ToList(),
            Meta = new ScimMeta { ResourceType = "Group", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{role.Id}" }
        });
    }

    [HttpPost("Groups")]
    public async Task<IActionResult> CreateGroup([FromBody] ScimGroupCreateRequest request)
    {
        var role = new IdentityServerHost.Models.IdentityRole
        {
            Id = Guid.NewGuid(),
            Name = request.DisplayName,
            NormalizedName = (request.DisplayName ?? "").ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGroup), new { id = role.Id }, new ScimGroup
        {
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Id = role.Id.ToString(),
            DisplayName = role.Name ?? "",
            Meta = new ScimMeta { ResourceType = "Group", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{role.Id}" }
        });
    }

    [HttpPut("Groups/{id}")]
    public async Task<IActionResult> UpdateGroup(string id, [FromBody] ScimGroupUpdateRequest request)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound();

        var role = await _db.Roles.FindAsync(guid);
        if (role == null) return NotFound();

        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            role.Name = request.DisplayName;
            role.NormalizedName = request.DisplayName.ToUpperInvariant();
        }
        if (request.Members != null)
        {
            var existing = await _db.UserRoles.Where(ur => ur.RoleId == guid).ToListAsync();
            _db.UserRoles.RemoveRange(existing);
            foreach (var m in request.Members.Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                if (Guid.TryParse(m.Value, out var userId))
                    _db.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<Guid> { UserId = userId, RoleId = guid });
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new ScimGroup
        {
            Schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
            Id = role.Id.ToString(),
            DisplayName = role.Name ?? "",
            Meta = new ScimMeta { ResourceType = "Group", Location = $"{Request.Scheme}://{Request.Host}/scim/v2/Groups/{role.Id}" }
        });
    }

    [HttpDelete("Groups/{id}")]
    public async Task<IActionResult> DeleteGroup(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            return NotFound();

        var role = await _db.Roles.FindAsync(guid);
        if (role == null) return NotFound();

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    #endregion

    private static IQueryable<ApplicationUser> ApplyUserFilter(IQueryable<ApplicationUser> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return query;
        var f = filter.Trim();
        var val = ExtractQuotedValue(f);
        if (val == null) return query;
        if (f.StartsWith("userName eq ", StringComparison.OrdinalIgnoreCase))
            return query.Where(u => u.UserName == val);
        if (f.StartsWith("userName co ", StringComparison.OrdinalIgnoreCase))
            return query.Where(u => u.UserName != null && u.UserName.Contains(val));
        if (f.StartsWith("userName sw ", StringComparison.OrdinalIgnoreCase))
            return query.Where(u => u.UserName != null && u.UserName.StartsWith(val));
        if (f.StartsWith("userName ew ", StringComparison.OrdinalIgnoreCase))
            return query.Where(u => u.UserName != null && u.UserName.EndsWith(val));
        if (f.StartsWith("emails.value eq ", StringComparison.OrdinalIgnoreCase))
            return query.Where(u => u.Email == val);
        if (f.StartsWith("emails.value co ", StringComparison.OrdinalIgnoreCase))
            return query.Where(u => u.Email != null && u.Email.Contains(val));
        return query;
    }

    private static IQueryable<IdentityServerHost.Models.IdentityRole> ApplyGroupFilter(IQueryable<IdentityServerHost.Models.IdentityRole> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return query;
        var f = filter.Trim();
        var val = ExtractQuotedValue(f);
        if (val == null) return query;
        if (f.StartsWith("displayName eq ", StringComparison.OrdinalIgnoreCase))
            return query.Where(r => r.Name == val);
        if (f.StartsWith("displayName co ", StringComparison.OrdinalIgnoreCase))
            return query.Where(r => r.Name != null && r.Name.Contains(val));
        if (f.StartsWith("displayName sw ", StringComparison.OrdinalIgnoreCase))
            return query.Where(r => r.Name != null && r.Name.StartsWith(val));
        if (f.StartsWith("displayName ew ", StringComparison.OrdinalIgnoreCase))
            return query.Where(r => r.Name != null && r.Name.EndsWith(val));
        return query;
    }

    private static string? ExtractQuotedValue(string filter)
    {
        var start = filter.IndexOf('"');
        if (start < 0) return null;
        var end = filter.IndexOf('"', start + 1);
        if (end < 0) return null;
        return filter[(start + 1)..end];
    }
}

#region SCIM Models

public class ScimUser
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = Array.Empty<string>();
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }
    [JsonPropertyName("emails")]
    public ScimEmail[] Emails { get; set; } = Array.Empty<ScimEmail>();
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("meta")]
    public ScimMeta? Meta { get; set; }
}

public class ScimGroup
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = Array.Empty<string>();
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    [JsonPropertyName("members")]
    public List<ScimGroupMember> Members { get; set; } = new();
    [JsonPropertyName("meta")]
    public ScimMeta? Meta { get; set; }
}

public class ScimGroupMember
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

public class ScimEmail
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}

public class ScimMeta
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "User";
    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

public class ScimListResponse<T>
{
    [JsonPropertyName("schemas")]
    public string[] Schemas { get; set; } = Array.Empty<string>();
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; }
    [JsonPropertyName("itemsPerPage")]
    public int ItemsPerPage { get; set; }
    [JsonPropertyName("Resources")]
    public List<T> Resources { get; set; } = new();
}

public class ScimUserCreateRequest
{
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;
    [JsonPropertyName("emails")]
    public ScimEmail[]? Emails { get; set; }
}

public class ScimUserUpdateRequest
{
    [JsonPropertyName("active")]
    public bool? Active { get; set; }
    [JsonPropertyName("emails")]
    public ScimEmail[]? Emails { get; set; }
}

public class ScimGroupCreateRequest
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
}

public class ScimGroupUpdateRequest
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    [JsonPropertyName("members")]
    public List<ScimGroupMember>? Members { get; set; }
}

#endregion
