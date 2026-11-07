using CustomHttpServer.Framework.Attributes;
using CustomHttpServer.Framework.Results;

namespace CustomHttpServer.Controllers;

[Controller]
public class UserController : BaseController
{
    [Get]
    public IHttpResult Profile()
    {
        var courses = new[]
        {
            new { Name = "ORIS", Hours = 72, Passed = true },
            new { Name = "Базы данных", Hours = 54, Passed = false },
            new { Name = "Алгоритмы", Hours = 90, Passed = true }
        };

        var model = new
        {
            Title = "Профиль пользователя",
            User = new
            {
                Name = "Тестовый Студент",
                Email = "student@example.com",
                Role = "Студент",
                IsActive = true
            },
            Courses = courses,
            HasCourses = courses.Length > 0
        };

        return Page("Profile", model);
    }

    [Get]
    public IHttpResult List()
    {
        var model = new
        {
            Title = "Список пользователей",
            Users = new[]
            {
                new { Name = "Алия", IsActive = true,  Roles = new[] { "Студент", "Староста" } },
                new { Name = "Борис", IsActive = false, Roles = new string[0] },
                // Проверка HTML-кодирования: тег не должен выполниться
                new { Name = "<script>alert('xss')</script>", IsActive = true, Roles = new[] { "Гость" } }
            }
        };

        return Page("List", model);
    }
}