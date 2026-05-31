using System.Security.Cryptography;
using System.Text;

static class AcsConnectionString
{
    public static (string endpoint, string accessKeyBase64) Parse(string connectionString)
    {
        // Example: "endpoint=https://<resource>.communication.azure.com/;accesskey=<base64>"
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        string endpoint = "";
        string accessKey = "";

        foreach (var p in parts)
        {
            var kv = p.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2)
            {
                var key = kv[0].Trim().ToLowerInvariant();
                var val = kv[1].Trim();
                if (key == "endpoint") endpoint = val.TrimEnd('/');
                if (key == "accesskey") accessKey = val;
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey))
            throw new InvalidOperationException("Invalid ACS connection string.");

        return (endpoint, accessKey);
    }
}

static class HmacAuth
{
    public static string ComputeContentHashBase64(string content)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
        return Convert.ToBase64String(bytes);
    }
}
