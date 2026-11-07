using System.Net;
using System.Net.Mail;
using CustomHttpServer.Framework.Configuration;

namespace CustomHttpServer.Services;

public class EmailService : IEmailService
{
    public void Send(string to, string subject, string body)
    {
        SmtpSettings settings = SmtpSettings.Load();

        using var message = new MailMessage(settings.User, to, subject, body);
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(settings.User, settings.Pass)
        };

        client.Send(message);
    }
}