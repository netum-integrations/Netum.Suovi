using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Netum.Suovi.MessageRelay.Definitions;

/// <summary>Additional parameters.</summary>
public class Options
{
    /// <summary>HTTP request timeout in seconds.</summary>
    /// <example>30</example>
    [DefaultValue(30)]
    [Range(1, int.MaxValue)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Whether to throw when the task fails.</summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>Optional replacement error message.</summary>
    /// <example>Suovi request failed.</example>
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
