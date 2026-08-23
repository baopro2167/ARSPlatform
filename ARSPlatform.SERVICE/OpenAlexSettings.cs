namespace ARSPlatform.SERVICE;

public class OpenAlexSettings
{
    public string BaseUrl { get; set; } = "https://api.openalex.org";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 15;

    public int MaxWorks { get; set; } = 100;
}