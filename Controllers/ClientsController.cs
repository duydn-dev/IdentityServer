using IdentityServer4.EntityFramework.Entities;
using IdentityServerHost.Models.ViewModels.Configuration;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerHost.Controllers;

[Authorize]
[SecurityHeaders]
public class ClientsController : Controller
{
    private readonly IClientConfigService _clientService;
    private readonly IApiScopeConfigService _apiScopeService;
    private readonly IIdentityResourceConfigService _identityResourceService;
    private readonly IdentityServer4.EntityFramework.DbContexts.ConfigurationDbContext _configDb;
    private readonly IAuditService _auditService;

    public ClientsController(
        IClientConfigService clientService,
        IApiScopeConfigService apiScopeService,
        IIdentityResourceConfigService identityResourceService,
        IdentityServer4.EntityFramework.DbContexts.ConfigurationDbContext configDb,
        IAuditService auditService)
    {
        _clientService = clientService;
        _apiScopeService = apiScopeService;
        _identityResourceService = identityResourceService;
        _configDb = configDb;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null)
    {
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var clients = await _clientService.GetAllAsync(page, pageSize, search);
        var total = await _clientService.GetCountAsync(search);
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(clients);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.AvailableScopes = await GetAllScopeNamesAsync();
        return View(new ClientViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientViewModel model)
    {
        ViewBag.AvailableScopes = await GetAllScopeNamesAsync();

        if (model.RequireClientSecret && string.IsNullOrEmpty(model.ClientSecret))
            ModelState.AddModelError(nameof(model.ClientSecret), "Client Secret là bắt buộc khi yêu cầu secret.");

        if (ModelState.IsValid)
        {
            var client = MapToEntity(model, isNew: true);
            if (model.RequireClientSecret && !string.IsNullOrEmpty(model.ClientSecret))
            {
                client.ClientSecrets.Add(new ClientSecret
                {
                    Value = SecretHasher.Sha256(model.ClientSecret!),
                    Type = "SharedSecret",
                    Created = DateTime.UtcNow
                });
            }

            if (await _clientService.CreateAsync(client))
            {
                await _auditService.LogAsync("Client.Create", "Client", client.ClientId, $"ClientName={model.ClientName}", true);
                TempData["Success"] = "Thêm client thành công.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, "Client ID đã tồn tại.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var client = await _clientService.GetByIdAsync(id);
        if (client == null) return NotFound();

        ViewBag.AvailableScopes = await GetAllScopeNamesAsync();
        return View(MapToViewModel(client));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClientViewModel model)
    {
        ViewBag.AvailableScopes = await GetAllScopeNamesAsync();

        var existing = await _clientService.GetByIdAsync(model.Id);
        if (existing == null) return NotFound();

        if (model.RequireClientSecret && existing.ClientSecrets.Count == 0 && string.IsNullOrEmpty(model.ClientSecret))
            ModelState.AddModelError(nameof(model.ClientSecret), "Client Secret là bắt buộc khi yêu cầu secret.");

        if (ModelState.IsValid)
        {
            var client = MapToEntity(model, isNew: false);
            client.ClientSecrets = existing.ClientSecrets;

            if (!string.IsNullOrEmpty(model.ClientSecret))
            {
                client.ClientSecrets.Add(new ClientSecret
                {
                    Value = SecretHasher.Sha256(model.ClientSecret),
                    Type = "SharedSecret",
                    Created = DateTime.UtcNow
                });
            }

            if (await _clientService.UpdateAsync(client))
            {
                await _auditService.LogAsync("Client.Update", "Client", client.ClientId, $"ClientName={model.ClientName}", true);
                TempData["Success"] = "Cập nhật client thành công.";
                return RedirectToAction(nameof(Index));
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var clientBeforeDelete = await _clientService.GetByIdAsync(id);
        if (await _clientService.DeleteAsync(id))
        {
            await _auditService.LogAsync("Client.Delete", "Client", clientBeforeDelete?.ClientId ?? id.ToString(), null, true);
            TempData["Success"] = "Xóa client thành công.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Error"] = "Không thể xóa client.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAjax(int id)
    {
        var clientBeforeDelete = await _clientService.GetByIdAsync(id);
        var success = await _clientService.DeleteAsync(id);
        if (success) await _auditService.LogAsync("Client.Delete", "Client", clientBeforeDelete?.ClientId ?? id.ToString(), null, true);
        return Json(new { success, message = success ? "Xóa thành công." : "Không thể xóa." });
    }

    private async Task<List<string>> GetAllScopeNamesAsync()
    {
        var apiScopes = await _configDb.ApiScopes.AsNoTracking().Select(s => s.Name).ToListAsync();
        var identityScopes = await _configDb.IdentityResources.AsNoTracking().Select(s => s.Name).ToListAsync();
        return apiScopes.Union(identityScopes).OrderBy(x => x).ToList();
    }

    private static IdentityServer4.EntityFramework.Entities.Client MapToEntity(ClientViewModel model, bool isNew)
    {
        var client = new IdentityServer4.EntityFramework.Entities.Client
        {
            Id = model.Id,
            ClientId = model.ClientId,
            ClientName = model.ClientName,
            Description = model.Description,
            ClientUri = model.ClientUri,
            LogoUri = model.LogoUri,
            Enabled = model.Enabled,
            RequireConsent = model.RequireConsent,
            AllowRememberConsent = model.AllowRememberConsent,
            RequirePkce = model.RequirePkce,
            AllowOfflineAccess = model.AllowOfflineAccess,
            RequireClientSecret = model.RequireClientSecret,
            ProtocolType = "oidc",
            AllowedScopes = model.AllowedScopes.Select(s => new ClientScope { Scope = s }).ToList(),
            RedirectUris = ParseLines(model.RedirectUrisText).Select(u => new ClientRedirectUri { RedirectUri = u }).ToList(),
            PostLogoutRedirectUris = ParseLines(model.PostLogoutRedirectUrisText).Select(u => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = u }).ToList(),
            AllowedCorsOrigins = ParseLines(model.AllowedCorsOriginsText).Select(o => new ClientCorsOrigin { Origin = o }).ToList(),
            AllowedGrantTypes = model.AllowedGrantTypes.Select(g => new ClientGrantType { GrantType = g }).ToList(),
            ClientSecrets = new List<ClientSecret>()
        };
        return client;
    }

    private static ClientViewModel MapToViewModel(IdentityServer4.EntityFramework.Entities.Client client)
    {
        return new ClientViewModel
        {
            Id = client.Id,
            ClientId = client.ClientId ?? string.Empty,
            ClientName = client.ClientName,
            Description = client.Description,
            ClientUri = client.ClientUri,
            LogoUri = client.LogoUri,
            Enabled = client.Enabled,
            RequireConsent = client.RequireConsent,
            AllowRememberConsent = client.AllowRememberConsent,
            RequirePkce = client.RequirePkce,
            AllowOfflineAccess = client.AllowOfflineAccess,
            RequireClientSecret = client.RequireClientSecret,
            AllowedGrantTypes = client.AllowedGrantTypes?.Select(g => g.GrantType ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string> { "authorization_code" },
            AllowedScopes = client.AllowedScopes?.Select(s => s.Scope ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList() ?? new List<string>(),
            RedirectUrisText = string.Join("\n", client.RedirectUris?.Select(u => u.RedirectUri) ?? Array.Empty<string>()),
            PostLogoutRedirectUrisText = string.Join("\n", client.PostLogoutRedirectUris?.Select(u => u.PostLogoutRedirectUri) ?? Array.Empty<string>()),
            AllowedCorsOriginsText = string.Join("\n", client.AllowedCorsOrigins?.Select(o => o.Origin) ?? Array.Empty<string>())
        };
    }

    private static List<string> ParseLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }
}
