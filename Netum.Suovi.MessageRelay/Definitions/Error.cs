using System;

namespace Netum.Suovi.MessageRelay.Definitions;

/// <summary>Task error.</summary>
public class Error
{
    /// <summary>Error message.</summary>
    /// <example>Suovi API returned status code 401.</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>Original exception.</summary>
    /// <example>Exception</example>
    public Exception AdditionalInfo { get; set; }
}
