using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace ATA.Infrastructure.Sms;

/// <summary>Shared HTTP handling for the SMS gateway adapters (placeholders validated by contract tests only).</summary>
public abstract class HttpSmsSender(HttpClient http, IOptions<SmsOptions> options) : ISmsSender
{
    protected SmsOptions Options { get; } = options.Value;

    public abstract string Provider { get; }

    public async Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(phoneNumber, message);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, Options.TimeoutSeconds)));
            using var response = await http.SendAsync(request, timeout.Token);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = (int)response.StatusCode;
            if (status >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return SmsSendResult.Transient($"http_{status}", Truncate(text));
            }

            if (!response.IsSuccessStatusCode)
            {
                return SmsSendResult.Permanent($"http_{status}", Truncate(text));
            }

            return new SmsSendResult(true, ReadMessageId(text), null, null, false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SmsSendResult.Transient("timeout", "SMS gateway timed out");
        }
        catch (HttpRequestException ex)
        {
            return SmsSendResult.Transient("network_error", ex.Message);
        }
    }

    protected abstract HttpRequestMessage BuildRequest(string phoneNumber, string message);

    protected abstract string? ReadMessageId(string responseBody);

    protected static string Digits(string phone) => new(phone.Where(char.IsAsciiDigit).ToArray());

    protected static JsonNode? TryParse(string text)
    {
        try
        {
            return JsonNode.Parse(text);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Truncate(string text) => text.Length > 480 ? text[..480] : text;
}

/// <summary>Unifonic-style adapter: <c>POST {BaseUrl}/rest/SMS/messages</c> with <c>AppSid</c>, <c>SenderID</c>, <c>Recipient</c>, <c>Body</c>.</summary>
public sealed class UnifonicSmsSender(HttpClient http, IOptions<SmsOptions> options) : HttpSmsSender(http, options)
{
    public override string Provider => "unifonic";

    protected override HttpRequestMessage BuildRequest(string phoneNumber, string message) =>
        new(HttpMethod.Post, $"{Options.Unifonic.BaseUrl.TrimEnd('/')}/rest/SMS/messages")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["AppSid"] = Options.Unifonic.AppSid ?? string.Empty,
                ["SenderID"] = Options.SenderName,
                ["Recipient"] = Digits(phoneNumber),
                ["Body"] = message,
            }),
        };

    protected override string? ReadMessageId(string responseBody) => TryParse(responseBody)?["data"]?["MessageID"]?.ToString();
}

/// <summary>Taqnyat-style adapter: <c>POST {BaseUrl}/v1/messages</c> with a Bearer token and <c>{ recipients[], body, sender }</c>.</summary>
public sealed class TaqnyatSmsSender(HttpClient http, IOptions<SmsOptions> options) : HttpSmsSender(http, options)
{
    public override string Provider => "taqnyat";

    protected override HttpRequestMessage BuildRequest(string phoneNumber, string message)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{Options.Taqnyat.BaseUrl.TrimEnd('/')}/v1/messages")
        {
            Content = new StringContent(
                new JsonObject { ["recipients"] = new JsonArray(Digits(phoneNumber)), ["body"] = message, ["sender"] = Options.SenderName }.ToJsonString(),
                Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.Taqnyat.BearerToken);
        return request;
    }

    protected override string? ReadMessageId(string responseBody) => TryParse(responseBody)?["messageId"]?.ToString();
}
