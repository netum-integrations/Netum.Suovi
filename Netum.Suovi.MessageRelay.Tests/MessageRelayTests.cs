using System.Text.Json;
using Netum.Suovi.MessageRelay.Definitions;
using NUnit.Framework;

namespace Netum.Suovi.MessageRelay.Tests;

[TestFixture]
public class MessageRelayTests
{
    [Test]
    public async Task GetAccessTokenSendsClientCredentialsAsFormData()
    {
        await using var server = new TestHttpServer("{\"access_token\":\"token-value\"}");

        var result = await Suovi.MessageRelay(
            new Input { Operation = Operation.GetAccessToken },
            new Connection
            {
                BaseUrl = server.BaseUrl,
                ClientId = "client id",
                ClientSecret = "client+secret",
            },
            DefaultOptions(),
            CancellationToken.None);
        var request = await server.Request;

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(200));
            Assert.That(result.Body, Is.EqualTo("{\"access_token\":\"token-value\"}"));
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Path, Is.EqualTo("/message-relay/oauth/token"));
            Assert.That(request.Headers["Content-Type"], Does.StartWith("application/x-www-form-urlencoded"));
            Assert.That(request.Body, Does.Contain("grant_type=client_credentials"));
            Assert.That(request.Body, Does.Contain("client_id=client+id"));
            Assert.That(request.Body, Does.Contain("client_secret=client%2Bsecret"));
        }));
    }

    [Test]
    public async Task PingSendsGetRequestWithBearerToken()
    {
        await using var server = new TestHttpServer("pong", contentType: "text/plain");

        var result = await Execute(server, new Input { Operation = Operation.Ping });
        var request = await server.Request;

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Body, Is.EqualTo("pong"));
            Assert.That(result.ContentType, Does.StartWith("text/plain"));
            Assert.That(request.Method, Is.EqualTo("GET"));
            Assert.That(request.Path, Is.EqualTo("/message-relay/v1/ping"));
            Assert.That(request.Headers["Authorization"], Is.EqualTo("Bearer access-token"));
        }));
    }

    [Test]
    public async Task CheckMailboxSendsReceiverIdentityAsJson()
    {
        await using var server = new TestHttpServer();

        await Execute(server, new Input
        {
            Operation = Operation.CheckMailbox,
            ReceiverIdentity = "123456789",
        });
        var request = await server.Request;
        using var body = JsonDocument.Parse(request.Body);

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(request.Path, Is.EqualTo("/message-relay/v1/check-mailbox"));
            Assert.That(request.Headers["Content-Type"], Does.StartWith("application/json"));
            Assert.That(body.RootElement.GetProperty("receiverIdentity").GetString(), Is.EqualTo("123456789"));
        }));
    }

    [Test]
    public async Task SendMessageSendsAllMessageFieldsAndAttachment()
    {
        await using var server = new TestHttpServer("{\"messageId\":12345}");

        await Execute(server, new Input
        {
            Operation = Operation.SendMessage,
            ReceiverName = "Essi Esimerkki",
            ReceiverIdentity = "123456-7890",
            Subject = "Subject",
            Message = "Message body",
            ExternalIdentifier = "external-123",
            AllowResponse = true,
            Attachments = [new Attachment { Name = "document.txt", Base64 = "SGVsbG8=" }],
        });
        var request = await server.Request;
        using var body = JsonDocument.Parse(request.Body);
        var root = body.RootElement;

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(request.Path, Is.EqualTo("/message-relay/v1/send-message"));
            Assert.That(root.GetProperty("receiverName").GetString(), Is.EqualTo("Essi Esimerkki"));
            Assert.That(root.GetProperty("receiverIdentity").GetString(), Is.EqualTo("123456-7890"));
            Assert.That(root.GetProperty("subject").GetString(), Is.EqualTo("Subject"));
            Assert.That(root.GetProperty("message").GetString(), Is.EqualTo("Message body"));
            Assert.That(root.GetProperty("externalIdentifier").GetString(), Is.EqualTo("external-123"));
            Assert.That(root.GetProperty("allowResponse").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("attachments")[0].GetProperty("name").GetString(), Is.EqualTo("document.txt"));
            Assert.That(root.GetProperty("attachments")[0].GetProperty("base64").GetString(), Is.EqualTo("SGVsbG8="));
        }));
    }

    [TestCase(12345, null)]
    [TestCase(null, "external-123")]
    public async Task GetMessageStatusAcceptsEitherIdentifier(long? messageId, string externalIdentifier)
    {
        await using var server = new TestHttpServer();

        await Execute(server, new Input
        {
            Operation = Operation.GetMessageStatus,
            MessageId = messageId,
            ExternalIdentifier = externalIdentifier,
        });
        var request = await server.Request;
        using var body = JsonDocument.Parse(request.Body);

        Assert.That(request.Path, Is.EqualTo("/message-relay/v1/message-status"));
        if (messageId.HasValue)
            Assert.That(body.RootElement.GetProperty("messageId").GetInt64(), Is.EqualTo(messageId.Value));
        else
            Assert.That(body.RootElement.GetProperty("externalIdentifier").GetString(), Is.EqualTo(externalIdentifier));
    }

    [Test]
    public async Task ValidationFailureIsReturnedWhenThrowingIsDisabled()
    {
        var result = await Suovi.MessageRelay(
            new Input { Operation = Operation.GetMessageStatus },
            DefaultConnection("http://127.0.0.1"),
            new Options { ThrowErrorOnFailure = false },
            CancellationToken.None);

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Message, Does.Contain("MessageId or ExternalIdentifier"));
            Assert.That(result.Error.AdditionalInfo, Is.TypeOf<ArgumentException>());
        }));
    }

    [Test]
    public async Task ApiFailureIsReturnedWhenThrowingIsDisabled()
    {
        await using var server = new TestHttpServer("{\"error\":\"unauthorized\"}", 401);

        var result = await Suovi.MessageRelay(
            new Input { Operation = Operation.Ping },
            DefaultConnection(server.BaseUrl),
            new Options { ThrowErrorOnFailure = false },
            CancellationToken.None);

        Assert.Multiple((TestDelegate)(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error.Message, Does.Contain("status code 401"));
            Assert.That(result.Error.Message, Does.Contain("unauthorized"));
        }));
    }

    private static Task<Result> Execute(TestHttpServer server, Input input) =>
        Suovi.MessageRelay(input, DefaultConnection(server.BaseUrl), DefaultOptions(), CancellationToken.None);

    private static Connection DefaultConnection(string baseUrl) => new()
    {
        BaseUrl = baseUrl,
        AccessToken = "access-token",
    };

    private static Options DefaultOptions() => new()
    {
        TimeoutSeconds = 5,
        ThrowErrorOnFailure = true,
    };
}
