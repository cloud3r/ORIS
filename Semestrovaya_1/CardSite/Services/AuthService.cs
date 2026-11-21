using CardSite.Models;
using CardSite.Repositories.Interfaces;
using CardSite.Services.Interfaces;

namespace CardSite.Services;

public class AuthService : IAuthService
{
    private readonly IAdminUserRepository _adminUserRepository;

    public AuthService(IAdminUserRepository adminUserRepository)
    {
        _adminUserRepository = adminUserRepository;
    }

    public async Task<AdminUser?> AuthenticateAsync(
        string username,
        string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        username = username.Trim();

        AdminUser? admin =
            await _adminUserRepository.GetByUsernameAsync(username);

        if (admin == null)
            return null;

        bool passwordIsValid = PasswordHasher.VerifyPassword(
            password,
            admin.PasswordHash);

        return passwordIsValid ? admin : null;
    }

    public async Task CreateAdminAsync(
        string username,
        string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        username = username.Trim();

        if (password.Length < 8)
        {
            throw new ArgumentException(
                "Administrator password must contain at least 8 characters.");
        }

        string email =
            Environment.GetEnvironmentVariable("CARDSITE_ADMIN_EMAIL")
            ?? throw new InvalidOperationException(
                "CARDSITE_ADMIN_EMAIL is not configured.");

        email = email.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "CARDSITE_ADMIN_EMAIL cannot be empty.");
        }

        AdminUser? existingAdmin =
            await _adminUserRepository.GetByUsernameAsync(username);

        if (existingAdmin != null)
        {
            throw new InvalidOperationException(
                "Administrator with this username already exists.");
        }

        var admin = new AdminUser
        {
            Username = username,
            PasswordHash = PasswordHasher.HashPassword(password),
            Email = email
        };

        await _adminUserRepository.AddAsync(admin);
    }

    public async Task EnsureInitialAdminAsync()
    {
        AdminUser? existingAdmin =
            await _adminUserRepository.GetFirstAsync();

        if (existingAdmin != null)
        {
            Console.WriteLine("Administrator account already exists.");
            return;
        }

        string username =
            Environment.GetEnvironmentVariable("CARDSITE_ADMIN_USERNAME")
            ?? throw new InvalidOperationException(
                "CARDSITE_ADMIN_USERNAME is not configured.");

        string password =
            Environment.GetEnvironmentVariable("CARDSITE_ADMIN_PASSWORD")
            ?? throw new InvalidOperationException(
                "CARDSITE_ADMIN_PASSWORD is not configured.");

        await CreateAdminAsync(username, password);

        Console.WriteLine("Initial administrator created successfully.");
    }
}