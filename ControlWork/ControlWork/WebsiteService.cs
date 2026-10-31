namespace ControlWork;

public class WebsiteService
{
    private List<Website> websites = new()
    {
        new Website
        {
            Id = 1,
            Name = "My Blog",
            Url = "https://example.com",
            Type = "blog",
            Status = "verified"
        }
    };

    private int nextId = 2;

    public List<Website> GetAll(string? type)
    {
        if (string.IsNullOrEmpty(type))
            return websites;

        return websites
            .Where(w => w.Type == type.ToLower())
            .ToList();
    }

    public Website? GetById(int id)
    {
        return websites.FirstOrDefault(w => w.Id == id);
    }

    public Website Create(string name, string url, string type)
    {
        Website website = new Website
        {
            Id = nextId++,
            Name = name.Trim(),
            Url = url,
            Type = type.ToLower(),
            Status = "unverified"
        };

        websites.Add(website);

        return website;
    }

    public bool Update(
        int id,
        string name,
        string url,
        string type,
        string status)
    {
        Website? website = GetById(id);

        if (website == null)
            return false;

        website.Name = name.Trim();
        website.Url = url;
        website.Type = type.ToLower();
        website.Status = status.ToLower();

        return true;
    }
}