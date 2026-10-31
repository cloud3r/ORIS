namespace ControlWork;

class Program
{
    static async Task Main()
    {
        WebServer server = new WebServer(
            "http://localhost:8080/"
        );

        await server.Start();
    }
}