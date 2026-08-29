namespace ARSPlatform.SERVICE;

public class OpenAlexSettings
{
    public string BaseUrl { get; set; } = "https://api.openalex.org";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 15;

    public int MaxWorks { get; set; } = 100;

    public int WorkCacheSeconds { get; set; } = 300;

    public int WorkLookupPermitLimit { get; set; } = 30;

    public int WorkLookupWindowSeconds { get; set; } = 60;
}
