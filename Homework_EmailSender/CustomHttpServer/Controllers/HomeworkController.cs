using System.Net;
using System.Net.Mail;
using CustomHttpServer.Attributes;
using CustomHttpServer.Helpers;

namespace CustomHttpServer.Controllers;

[Controller("home-work")]
public class HomeworkController
{
    private const string GroupNumber = "11-507";
    private const string FullName = "Багаутдинов Дамир Риядович";

    [Get("send")]
    public string Send(string to)
    {
        var recipient = new MailAddress(to);
        var smtp = SmtpSettings.Load();

        using var zip = ProjectArchiver.BuildProjectZip();

        using var message = new MailMessage(new MailAddress(smtp.User), recipient)
        {
            Subject = $"Домашняя работа: {FullName}, группа {GroupNumber}",
            Body = $"Группа: {GroupNumber}\nФИО: {FullName}"
        };
        message.Attachments.Add(new Attachment(zip, "project.zip", "application/zip"));

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtp.User, smtp.Pass)
        };
        client.Send(message);

        return "OK";
    }
}