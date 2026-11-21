using System.Collections.Concurrent;
using System.Security.Cryptography;
using CardSite.Services.Interfaces;

namespace CardSite.Services;

public class SessionService : ISessionService
{
    private readonly ConcurrentDictionary<string, AdminSession> _sessions = new();

    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public string CreateSession(string username)
    {
        string token = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var session = new AdminSession
        {
            Username = username,
            ExpiresAt = DateTime.UtcNow.Add(SessionLifetime)
        };

        _sessions[token] = session;

        return token;
    }

    public string? GetUsername(string token)
    {
        if (!_sessions.TryGetValue(token, out var session))
            return null;

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        return session.Username;
    }

    public void RemoveSession(string token)
    {
        _sessions.TryRemove(token, out _);
    }

    private class AdminSession
    {
        public string Username { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}