using System.ComponentModel.DataAnnotations;

namespace Netum.Suovi.MessageRelay.Definitions;

/// <summary>Message attachment.</summary>
public class Attachment
{
    /// <summary>Attachment filename.</summary>
    /// <example>document.pdf</example>
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Base64-encoded file content.</summary>
    /// <example>JVBERi0xLjQK...</example>
    [Required]
    public string Base64 { get; set; } = string.Empty;
}
