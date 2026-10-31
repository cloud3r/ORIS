namespace ControlWork ;

public class CreateWebsiteRequest
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Type { get; set; } = "";
}

public class UpdateWebsiteRequest
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
}