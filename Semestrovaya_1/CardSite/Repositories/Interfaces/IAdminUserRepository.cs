using CardSite.Models;

namespace CardSite.Repositories.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username);

    Task<AdminUser?> GetFirstAsync();

    Task AddAsync(AdminUser admin);
}