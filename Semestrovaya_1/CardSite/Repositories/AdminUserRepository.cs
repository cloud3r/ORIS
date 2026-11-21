using CardSite.Models;
using CardSite.ORM;
using CardSite.Repositories.Interfaces;

namespace CardSite.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly CardSiteContext _context;

    public AdminUserRepository(CardSiteContext context)
    {
        _context = context;
    }

    public async Task<AdminUser?> GetByUsernameAsync(string username)
    {
        return await _context.AdminUsers
            .Where(user => user.Username == username)
            .FirstOrDefaultAsync();
    }

    public async Task<AdminUser?> GetFirstAsync()
    {
        return await _context.AdminUsers.FirstOrDefaultAsync();
    }

    public async Task AddAsync(AdminUser admin)
    {
        await _context.AdminUsers.AddAsync(admin);
    }
}