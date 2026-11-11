using System.Collections.Generic;

namespace Netum.Suovi.MessageRelay.Definitions;

/// <summary>Task result.</summary>
public class Result
{
    /// <summary>Whether the request completed successfully.</summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>Raw HTTP response text on success. Parse it as JSON when the operation returns JSON.</summary>
    /// <example>{ "mailboxEnabled": true }</example>
    public string Body { get; set; } = string.Empty;

    /// <summary>Response content type.</summary>
    /// <example>application/json</example>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Response headers.</summary>
    /// <example>{ "content-type": "application/json" }</example>
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>HTTP status code.</summary>
    /// <example>200</example>
    public int StatusCode { get; set; }

    /// <summary>Error details when the task fails and ThrowErrorOnFailure is false.</summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}
