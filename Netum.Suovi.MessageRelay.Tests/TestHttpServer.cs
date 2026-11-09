using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Netum.Suovi.MessageRelay.Tests;

internal sealed class TestHttpServer : IAsyncDisposable
{
    private readonly TcpListener listener;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Task<RequestData> requestTask;
    private readonly string responseBody;
    private readonly int statusCode;

    internal TestHttpServer(string responseBody = "{}", int statusCode = 200, string contentType = "application/json")
    {
        this.responseBody = responseBody;
        this.statusCode = statusCode;
        ContentType = contentType;
        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        BaseUrl = $"http://127.0.0.1:{port}";
        requestTask = ReceiveRequestAsync();
    }

    internal string BaseUrl { get; }

    private string ContentType { get; }

    internal Task<RequestData> Request => requestTask;

    public async ValueTask DisposeAsync()
    {
        cancellationTokenSource.Cancel();
        listener.Stop();
        try
        {
            await requestTask.ConfigureAwait(false);
        }
        catch (Exception) when (!requestTask.IsCompletedSuccessfully)
        {
            // The listener may be stopped before a request is received when a test fails.
        }

        cancellationTokenSource.Dispose();
    }

    private async Task<RequestData> ReceiveRequestAsync()
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);

        var requestLine = await reader.ReadLineAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("Missing HTTP request line.");
        var requestParts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
        {
            var separator = line.IndexOf(':');
            if (separator > 0)
                headers[line[..separator]] = line[(separator + 1)..].Trim();
        }

        var contentLength = headers.TryGetValue("Content-Length", out var value) ? int.Parse(value) : 0;
        var buffer = new char[contentLength];
        var charactersRead = 0;
        while (charactersRead < contentLength)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(charactersRead, contentLength - charactersRead)).ConfigureAwait(false);
            if (count == 0)
                break;
            charactersRead += count;
        }

        var responseBytes = Encoding.UTF8.GetBytes(responseBody);
        var reasonPhrase = statusCode is >= 200 and < 300 ? "OK" : "Error";
        var responseHeaders = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {statusCode} {reasonPhrase}\r\n" +
            $"Content-Type: {ContentType}\r\n" +
            $"Content-Length: {responseBytes.Length}\r\n" +
            "Connection: close\r\n\r\n");
        await stream.WriteAsync(responseHeaders).ConfigureAwait(false);
        await stream.WriteAsync(responseBytes).ConfigureAwait(false);

        return new RequestData(
            requestParts[0],
            requestParts[1],
            headers,
            new string(buffer, 0, charactersRead));
    }
}

internal sealed record RequestData(
    string Method,
    string Path,
    IReadOnlyDictionary<string, string> Headers,
    string Body);
