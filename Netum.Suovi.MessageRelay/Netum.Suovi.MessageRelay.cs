using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Netum.Suovi.MessageRelay.Definitions;
using Netum.Suovi.MessageRelay.Helpers;

namespace Netum.Suovi.MessageRelay;

/// <summary>Frends task for Suovi Message Relay operations.</summary>
public static class Suovi
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Calls the Suovi Message Relay API.
    /// [Documentation](https://tasks.frends.com/tasks/netum/Netum-Suovi-MessageRelay)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="connection">Connection and authentication parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">Cancellation token supplied by Frends.</param>
    /// <returns>HTTP response data and possible error details.</returns>
    public static async Task<Result> MessageRelay(
        [PropertyTab] Input input,
        [PropertyTab] Connection connection,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            Validate(input, connection, options);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));
            using var client = new HttpClient();
            using var request = CreateRequest(input, connection);
            using var response = await client.SendAsync(request, timeout.Token).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Suovi API returned status code {(int)response.StatusCode}: {body}");

            return new Result
            {
                Success = true,
                Body = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty,
                Headers = GetHeaders(response),
                StatusCode = (int)response.StatusCode,
            };
        }
        catch (Exception exception)
        {
            return exception.Handle(options);
        }
    }

    private static void Validate(Input input, Connection connection, Options options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);
        Validator.ValidateObject(connection, new ValidationContext(connection), true);
        Validator.ValidateObject(input, new ValidationContext(input), true);
        Validator.ValidateObject(options, new ValidationContext(options), true);

        if (input.Operation == Operation.GetAccessToken)
        {
            Require(connection.ClientId, nameof(connection.ClientId));
            Require(connection.ClientSecret, nameof(connection.ClientSecret));
            return;
        }

        Require(connection.AccessToken, nameof(connection.AccessToken));

        if (input.Operation is Operation.CheckMailbox or Operation.SendMessage)
        {
            Require(input.ReceiverIdentity, nameof(input.ReceiverIdentity));
            if (!System.Text.RegularExpressions.Regex.IsMatch(input.ReceiverIdentity, "^[0-9A-Za-z]{9}$|^[0-9]{6}[+-A][0-9]{3}[0-9A-Za-z]$"))
                throw new ArgumentException("ReceiverIdentity must be a valid personal or business identity code.");
        }

        if (input.Operation == Operation.SendMessage)
        {
            Require(input.ReceiverName, nameof(input.ReceiverName));
            Require(input.Subject, nameof(input.Subject));
            Require(input.Message, nameof(input.Message));
            foreach (var attachment in input.Attachments ?? [])
                Validator.ValidateObject(attachment, new ValidationContext(attachment), true);
        }

        if (input.Operation == Operation.GetMessageStatus &&
            !input.MessageId.HasValue && string.IsNullOrWhiteSpace(input.ExternalIdentifier))
            throw new ArgumentException("MessageId or ExternalIdentifier must be provided.");
    }

    private static void Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required.");
    }

    private static HttpRequestMessage CreateRequest(Input input, Connection connection)
    {
        var request = new HttpRequestMessage(GetMethod(input.Operation), BuildUri(connection.BaseUrl, GetPath(input.Operation)));
        if (input.Operation == Operation.GetAccessToken)
        {
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = connection.ClientId,
                ["client_secret"] = connection.ClientSecret,
            });
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var body = CreateBody(input);
            if (body != null)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string CreateBody(Input input) => input.Operation switch
    {
        Operation.CheckMailbox => JsonSerializer.Serialize(new { input.ReceiverIdentity }, JsonOptions),
        Operation.SendMessage => JsonSerializer.Serialize(new
        {
            input.ReceiverName,
            input.ReceiverIdentity,
            input.Subject,
            input.Message,
            input.ExternalIdentifier,
            input.AllowResponse,
            Attachments = input.Attachments?.Length > 0 ? input.Attachments : null,
        }, JsonOptions),
        Operation.GetMessageStatus => JsonSerializer.Serialize(new { input.MessageId, input.ExternalIdentifier }, JsonOptions),
        _ => null,
    };

    private static HttpMethod GetMethod(Operation operation) => operation == Operation.Ping ? HttpMethod.Get : HttpMethod.Post;

    private static string GetPath(Operation operation) => operation switch
    {
        Operation.GetAccessToken => "/message-relay/oauth/token",
        Operation.Ping => "/message-relay/v1/ping",
        Operation.CheckMailbox => "/message-relay/v1/check-mailbox",
        Operation.SendMessage => "/message-relay/v1/send-message",
        Operation.GetMessageStatus => "/message-relay/v1/message-status",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
    };

    private static Uri BuildUri(string baseUrl, string path)
    {
        var root = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";
        return new Uri(new Uri(root), path.TrimStart('/'));
    }

    private static Dictionary<string, string> GetHeaders(HttpResponseMessage response) =>
        response.Headers.Concat(response.Content.Headers)
            .ToDictionary(header => header.Key, header => string.Join(";", header.Value), StringComparer.OrdinalIgnoreCase);
}
