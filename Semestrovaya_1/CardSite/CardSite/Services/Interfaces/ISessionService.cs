namespace CardSite.Services.Interfaces;

public interface ISessionService
{
    string CreateSession(string username);

    string? GetUsername(string token);

    void RemoveSession(string token);
}