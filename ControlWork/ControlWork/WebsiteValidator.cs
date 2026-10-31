namespace ControlWork;

public static class WebsiteValidator
{
    public static string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Name is required";

        if (name.Trim().Length < 3 || name.Trim().Length > 80)
            return "Name must contain from 3 to 80 characters";

        return null;
    }

    public static string? ValidateType(string type)
    {
        string[] allowedTypes = { "blog", "store", "portal" };

        if (!allowedTypes.Contains(type.ToLower()))
            return "Type must be blog, store or portal";

        return null;
    }

    public static string? ValidateStatus(string status)
    {
        string[] allowedStatuses = { "unverified", "verified" };

        if (!allowedStatuses.Contains(status.ToLower()))
            return "Status must be unverified or verified";

        return null;
    }

    public static string? ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return "URL must be an absolute address";

        if (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps)
            return "URL must use HTTP or HTTPS";

        return null;
    }
}