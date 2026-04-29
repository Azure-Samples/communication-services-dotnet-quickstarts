namespace CallAutomationOpenAI;

public static class GuidUrlExtensions
{
    /// <summary>
    /// Extension method to encode a GUID to shortened, encoded, url-safe version.
    /// </summary>
    public static string ToEncodedUrlString(this Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray())
                .Replace('/', '_')
                .Replace('+', '-')
                .TrimEnd('=');
    }

    /// <summary>
    /// Extension method to decode an encoded GUID back to the original unencoded structure.
    /// </summary>
    public static bool TryDecodeUrlString(this string urlString, out Guid guid)
    {
        if (string.IsNullOrEmpty(urlString))
        {
            guid = Guid.Empty;
            return false;
        }

        try
        {
            var guidBytes = Convert.FromBase64String(urlString
                                    .Replace('_', '/')
                                    .Replace('-', '+')
                                    .PadRight(24, '=')
                                    );

            guid = new Guid(guidBytes);
        }
        catch (FormatException)
        {
            guid = Guid.Empty;
            return false;
        }
        catch (ArgumentException)
        {
            guid = Guid.Empty;
            return false;
        }

        return true;
    }
}
