using System.Net;
using System.Text;

namespace CardSite.Server;

public static class FormDataReader
{
    private const int MaxFormSize = 64 * 1024;

    public static async Task<Dictionary<string, string>> ReadFormAsync(
        HttpListenerRequest request)
    {
        var values = await ReadFormValuesAsync(request);

        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in values)
        {
            if (item.Value.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Duplicate form field: {item.Key}");
            }

            result[item.Key] = item.Value.FirstOrDefault() ?? string.Empty;
        }

        return result;
    }

    public static async Task<Dictionary<string, List<string>>> ReadFormValuesAsync(
        HttpListenerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? contentType = request.ContentType;

        if (contentType == null ||
            !contentType.StartsWith(
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Unsupported form content type.");
        }

        if (request.ContentLength64 > MaxFormSize)
        {
            throw new InvalidOperationException(
                "Form data is too large.");
        }

        Encoding encoding = request.ContentEncoding;

        using var reader = new StreamReader(
            request.InputStream,
            encoding,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true);

        var builder = new StringBuilder();
        var buffer = new char[4096];

        int read;

        while ((read = await reader.ReadAsync(buffer)) > 0)
        {
            if (builder.Length + read > MaxFormSize)
            {
                throw new InvalidOperationException(
                    "Form data is too large.");
            }

            builder.Append(buffer, 0, read);
        }

        var result = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase);

        string formData = builder.ToString();

        foreach (string pair in formData.Split('&'))
        {
            if (string.IsNullOrWhiteSpace(pair))
                continue;

            string[] parts = pair.Split('=', 2);

            string key = WebUtility.UrlDecode(parts[0]);

            string value = parts.Length > 1
                ? WebUtility.UrlDecode(parts[1])
                : string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!result.TryGetValue(key, out var values))
            {
                values = new List<string>();
                result[key] = values;
            }

            values.Add(value);
        }

        return result;
    }
}