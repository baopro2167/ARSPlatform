namespace ARSPlatform.SERVICE;

public class OrcidSettings
{
    public string AuthorizationUrl { get; set; }
        = "https://orcid.org/oauth/authorize";

    public string TokenUrl { get; set; }
        = "https://orcid.org/oauth/token";

    public string ClientId { get; set; }
        = string.Empty;

    public string ClientSecret { get; set; }
        = string.Empty;

    public string RedirectUri { get; set; }
        = string.Empty;

    public string Scope { get; set; }
        = "/authenticate";

    public int TimeoutSeconds { get; set; } = 15;
}