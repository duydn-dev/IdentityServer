using IdentityServerHost.Attributes;
using IdentityServerHost.Constants;
using IdentityServerHost.Services.Audit;
using IdentityServerHost.Services.ClientKeys;
using IdentityServerHost.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerHost.Controllers;

/// <summary>
/// Controller quản lý RSA Key Pairs cho Clients (Admin only)
/// </summary>
[Authorize(Roles = Roles.Admin)]
[SecurityHeaders]
public class ClientKeysController : Controller
{
    private readonly IClientKeyService _keyService;
    private readonly IClientConfigService _clientService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ClientKeysController> _logger;

    public ClientKeysController(
        IClientKeyService keyService,
        IClientConfigService clientService,
        IAuditService auditService,
        ILogger<ClientKeysController> logger)
    {
        _keyService = keyService;
        _clientService = clientService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var keys = await _keyService.GetAllAsync();
        return View(keys);
    }

    [HttpGet]
    public async Task<IActionResult> Generate(int clientId)
    {
        var client = await _clientService.GetByIdAsync(clientId);
        if (client == null)
        {
            TempData["Error"] = "Client không tồn tại";
            return RedirectToAction("Index", "Clients");
        }

        ViewBag.Client = client;
        return View(new GenerateKeyViewModel { ClientId = client.ClientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateKeyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var keyPair = await _keyService.GenerateKeyPairAsync(
                model.ClientId,
                model.KeySize,
                model.Description,
                model.ExpiresAt);

            await _auditService.LogAsync(
                "GenerateClientKey",
                "ClientKeyPair",
                keyPair.Id.ToString(),
                $"ClientId: {model.ClientId}, KeySize: {model.KeySize}, User: {User.Identity?.Name}");

            TempData["Success"] = $"Đã tạo key pair cho client {model.ClientId}";
            TempData["PrivateKey"] = keyPair.PrivateKey;
            TempData["PublicKey"] = keyPair.PublicKey;
            TempData["ClientId"] = model.ClientId;

            return RedirectToAction(nameof(ShowKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating key for client {ClientId}", model.ClientId);
            ModelState.AddModelError("", "Lỗi khi tạo key: " + ex.Message);
            return View(model);
        }
    }

    public IActionResult ShowKey()
    {
        var privateKey = TempData["PrivateKey"] as string;
        var publicKey = TempData["PublicKey"] as string;
        var clientId = TempData["ClientId"] as string;

        if (string.IsNullOrEmpty(privateKey))
        {
            return RedirectToAction(nameof(Index));
        }

        ViewBag.PrivateKey = privateKey;
        ViewBag.PublicKey = publicKey;
        ViewBag.ClientId = clientId;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(string clientId)
    {
        var result = await _keyService.DeactivateKeyAsync(clientId);
        if (result)
        {
            await _auditService.LogAsync(
                "DeactivateClientKey",
                "ClientKeyPair",
                clientId,
                $"User: {User.Identity?.Name}");

            TempData["Success"] = $"Đã vô hiệu hóa key cho client {clientId}";
        }
        else
        {
            TempData["Error"] = "Không tìm thấy key để vô hiệu hóa";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string clientId)
    {
        var result = await _keyService.DeleteAsync(clientId);
        if (result)
        {
            await _auditService.LogAsync(
                "DeleteClientKey",
                "ClientKeyPair",
                clientId,
                $"User: {User.Identity?.Name}");

            TempData["Success"] = $"Đã xóa key cho client {clientId}";
        }
        else
        {
            TempData["Error"] = "Không tìm thấy key để xóa";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshCache(string clientId)
    {
        await _keyService.InvalidateCacheAsync(clientId);
        TempData["Success"] = $"Đã làm mới cache cho client {clientId}";
        return RedirectToAction(nameof(Index));
    }
}

public class GenerateKeyViewModel
{
    [System.ComponentModel.DataAnnotations.Required]
    public string ClientId { get; set; } = null!;

    public int KeySize { get; set; } = 2048;

    public string? Description { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
