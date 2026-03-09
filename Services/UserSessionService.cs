using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SnapdragonApi.DTOs;
using SnapdragonApi.Models;

namespace SnapdragonApi.Services;

public interface IUserSessionService
{
    Task<UserSessionContext> GetOrLoadAsync(string email);
    UserSessionContext? Get(string email);
    void Invalidate(string email);
    void InvalidateAll();
}

public class UserSessionService : IUserSessionService
{
    private readonly ConcurrentDictionary<string, UserSessionContext> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserSessionService> _logger;

    public UserSessionService(IServiceScopeFactory scopeFactory, ILogger<UserSessionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<UserSessionContext> GetOrLoadAsync(string email)
    {
        if (_cache.TryGetValue(email, out var cached))
        {
            _logger.LogDebug("Session context for {Email} loaded from cache", email);
            return cached;
        }

        var context = await LoadFromDatabaseAsync(email);
        _cache[email] = context;
        _logger.LogInformation("Session context for {Email} loaded from database", email);
        return context;
    }

    public UserSessionContext? Get(string email)
    {
        _cache.TryGetValue(email, out var cached);
        return cached;
    }

    public void Invalidate(string email)
    {
        _cache.TryRemove(email, out _);
        _logger.LogInformation("Session context invalidated for {Email}", email);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
        _logger.LogInformation("All session contexts invalidated");
    }

    private async Task<UserSessionContext> LoadFromDatabaseAsync(string email)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.User
            .Include(u => u.Office)
            .FirstOrDefaultAsync(u => u.Email == email && u.StatusId == 1);

        if (user?.Office == null)
            throw new InvalidOperationException($"Active user with email '{email}' not found");

        var groupMemberships = await db.UserGroupMember
            .Where(gm => gm.UserId == user.Id)
            .Join(db.UserGroup.Where(g => g.StatusId == 1),
                  gm => gm.UserGroupId,
                  g => g.Id,
                  (gm, g) => new { g.Id, g.Name })
            .ToListAsync();

        var officeAccessIds = await db.UserOfficeAccess
            .Where(oa => oa.UserId == user.Id)
            .Select(oa => oa.OfficeId)
            .ToListAsync();

        var warehouseAccessIds = await db.UserWarehouseAccess
            .Where(wa => wa.UserId == user.Id)
            .Select(wa => wa.WarehouseId)
            .ToListAsync();

        return new UserSessionContext
        {
            UserId = user.Id,
            Email = user.Email,
            CompanyId = user.Office.CompanyId,
            OfficeId = user.OfficeId,
            UserLevel = user.UserLevel,
            DefaultWarehouseId = user.DefaultWarehouseId,
            AccessibleOfficeIds = officeAccessIds,
            AccessibleWarehouseIds = warehouseAccessIds,
            GroupIds = groupMemberships.Select(g => g.Id).ToList(),
            GroupNames = groupMemberships.Select(g => g.Name).ToList(),
            LoadedAt = DateTime.UtcNow
        };
    }
}
