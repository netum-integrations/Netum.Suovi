using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Netum.Suovi.MessageRelay.Definitions;

/// <summary>Connection parameters.</summary>
public class Connection
{
    /// <summary>Base URL of the Suovi API.</summary>
    /// <example>https://api.example.fi</example>
    [Required]
    [DefaultValue("https://api.example.fi")]
    [DisplayFormat(DataFormatString = "Text")]
    public string BaseUrl { get; set; } = "https://api.example.fi";

    /// <summary>JWT access token used by all operations except GetAccessToken.</summary>
    /// <example>eyJhbGciOi...</example>
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>OAuth client ID used by GetAccessToken.</summary>
    /// <example>client-id</example>
    [DisplayFormat(DataFormatString = "Text")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth client secret used by GetAccessToken.</summary>
    /// <example>client-secret</example>
    [PasswordPropertyText]
    [DisplayFormat(DataFormatString = "Text")]
    public string ClientSecret { get; set; } = string.Empty;
}
