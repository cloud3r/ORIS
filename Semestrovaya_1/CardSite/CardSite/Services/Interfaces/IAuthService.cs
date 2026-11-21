using CardSite.Models;

namespace CardSite.Services.Interfaces;

public interface IAuthService
{
    Task<AdminUser?> AuthenticateAsync(
        string username,
        string password);

    Task CreateAdminAsync(
        string username,
        string password);

    Task EnsureInitialAdminAsync();
}