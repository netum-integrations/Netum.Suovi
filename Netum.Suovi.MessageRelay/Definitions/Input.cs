using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Netum.Suovi.MessageRelay.Definitions;

/// <summary>Essential parameters.</summary>
public class Input
{
    /// <summary>Operation to execute.</summary>
    /// <example>Ping</example>
    [DefaultValue(Operation.Ping)]
    public Operation Operation { get; set; } = Operation.Ping;

    /// <summary>Receiver's personal identity code or business identity code.</summary>
    /// <example>123456-7890</example>
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(Operation), "", Operation.CheckMailbox, Operation.SendMessage)]
    public string ReceiverIdentity { get; set; } = string.Empty;

    /// <summary>Receiver's name.</summary>
    /// <example>Essi Esimerkki</example>
    [MaxLength(50)]
    [UIHint(nameof(Operation), "", Operation.SendMessage)]
    public string ReceiverName { get; set; } = string.Empty;

    /// <summary>Message subject.</summary>
    /// <example>Message subject</example>
    [MaxLength(100)]
    [UIHint(nameof(Operation), "", Operation.SendMessage)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>Message body.</summary>
    /// <example>Message body content</example>
    [MaxLength(50000)]
    [DisplayFormat(DataFormatString = "Text")]
    [UIHint(nameof(Operation), "", Operation.SendMessage)]
    public string Message { get; set; } = string.Empty;

    /// <summary>External reference identifier.</summary>
    /// <example>your-system-reference-123</example>
    [MaxLength(100)]
    [UIHint(nameof(Operation), "", Operation.SendMessage, Operation.GetMessageStatus)]
    public string ExternalIdentifier { get; set; }

    /// <summary>Whether the receiver may respond.</summary>
    /// <example>false</example>
    [DefaultValue(false)]
    [UIHint(nameof(Operation), "", Operation.SendMessage)]
    public bool AllowResponse { get; set; }

    /// <summary>Optional message attachments.</summary>
    /// <example>new [] { new Attachment { Name = "document.pdf", Base64 = "JVBERi0xLjQK..." } }</example>
    [DisplayFormat(DataFormatString = "Expression")]
    [UIHint(nameof(Operation), "", Operation.SendMessage)]
    public Attachment[] Attachments { get; set; } = [];

    /// <summary>Internal message ID.</summary>
    /// <example>12345</example>
    [UIHint(nameof(Operation), "", Operation.GetMessageStatus)]
    public long? MessageId { get; set; }
}
