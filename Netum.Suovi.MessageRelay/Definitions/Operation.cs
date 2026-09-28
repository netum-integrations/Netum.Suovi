namespace Netum.Suovi.MessageRelay.Definitions;

/// <summary>Suovi Message Relay operation.</summary>
public enum Operation
{
    /// <summary>Obtain an OAuth2 access token.</summary>
    GetAccessToken,

    /// <summary>Check API availability.</summary>
    Ping,

    /// <summary>Check whether a receiver has an active Suomi.fi mailbox.</summary>
    CheckMailbox,

    /// <summary>Send a message.</summary>
    SendMessage,

    /// <summary>Query message status.</summary>
    GetMessageStatus,
}
