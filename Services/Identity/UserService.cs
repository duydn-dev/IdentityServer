using IdentityServerHost.Data;
using IdentityServerHost.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Services.Identity;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var query = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.Email != null && u.Email.ToLower().Contains(searchLower)));
        }

        return await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(string? search = null)
    {
        var query = _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.Email != null && u.Email.ToLower().Contains(searchLower)));
        }

        return await query.CountAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName);
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(ApplicationUser user, string password, IEnumerable<string>? roles = null)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        if (roles != null && roles.Any())
        {
            var addRolesResult = await _userManager.AddToRolesAsync(user, roles);
            if (!addRolesResult.Succeeded)
                return (false, addRolesResult.Errors.Select(e => e.Description));
        }

        return (true, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(ApplicationUser user, string? newPassword, IEnumerable<string>? roles = null)
    {
        var existing = await _userManager.FindByIdAsync(user.Id.ToString());
        if (existing == null)
            return (false, new[] { "Người dùng không tồn tại." });

        existing.UserName = user.UserName;
        existing.NormalizedUserName = user.UserName?.ToUpperInvariant();
        existing.Email = user.Email;
        existing.NormalizedEmail = user.Email?.ToUpperInvariant();
        existing.PhoneNumber = user.PhoneNumber;
        existing.EmailConfirmed = user.EmailConfirmed;
        existing.PhoneNumberConfirmed = user.PhoneNumberConfirmed;
        existing.TwoFactorEnabled = user.TwoFactorEnabled;
        existing.LockoutEnd = user.LockoutEnd;
        existing.LockoutEnabled = user.LockoutEnabled;
        existing.AccessFailedCount = user.AccessFailedCount;

        var result = await _userManager.UpdateAsync(existing);
        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        if (!string.IsNullOrEmpty(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await _userManager.ResetPasswordAsync(existing, token, newPassword);
            if (!resetResult.Succeeded)
                return (false, resetResult.Errors.Select(e => e.Description));
        }

        if (roles != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(existing);
            var removeResult = await _userManager.RemoveFromRolesAsync(existing, currentRoles);
            if (!removeResult.Succeeded)
                return (false, removeResult.Errors.Select(e => e.Description));

            if (roles.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(existing, roles);
                if (!addResult.Succeeded)
                    return (false, addResult.Errors.Select(e => e.Description));
            }
        }

        return (true, Enumerable.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return (false, new[] { "Người dùng không tồn tại." });

        var result = await _userManager.DeleteAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }
}
