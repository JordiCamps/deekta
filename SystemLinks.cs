using System.Diagnostics;

namespace Deekta;

/// <summary>Opens URLs in the user's default browser.</summary>
internal static class SystemLinks
{
    public const string OpenAiKeysUrl = "https://platform.openai.com/api-keys";
    public const string OpenAiPricingUrl = "https://openai.com/api/pricing";
    public const string RepoUrl = "https://github.com/JordiCamps/deekta";

    public static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Error($"Could not open URL: {url}", ex);
        }
    }
}
