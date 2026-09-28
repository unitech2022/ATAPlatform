using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATA.Infrastructure.Push;

/// <summary>
/// OneSignal REST API sender: <c>POST {ApiBaseUrl}/notifications</c> with <c>Authorization: Key {RestApiKey}</c>, addressing users by
/// External ID alias (<c>include_aliases.external_id</c>). 5xx/429/timeouts are transient; <c>errors.invalid_aliases</c> or "not subscribed"
/// report the affected users in <see cref="PushSendResult.InvalidExternalUserIds"/>.
/// </summary>
public sealed class OneSignalPushSender(HttpClient http, IOptions<OneSignalOptions> options, ILogger<OneSignalPushSender> logger) : IPushSender
{
    private readonly OneSignalOptions _options = options.Value;

    public string Provider => "onesignal";

    public async Task<PushSendResult> SendAsync(PushMessage message, CancellationToken ct)
    {
        var body = BuildBody(message);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/"), "notifications"))
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Key", _options.RestApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));
            response = await http.SendAsync(request, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return PushSendResult.Transient("timeout", "OneSignal request timed out");
        }
        catch (HttpRequestException ex)
        {
            return PushSendResult.Transient("network_error", ex.Message);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            var status = (int)response.StatusCode;
            if (status >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return PushSendResult.Transient($"http_{status}", Truncate(text));
            }

            JsonNode? json = null;
            try
            {
                json = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            }
            catch (JsonException)
            {
                // Non-JSON body: handled by the status code below.
            }

            var invalid = InvalidAliases(json, message.ExternalUserIds);
            if (!response.IsSuccessStatusCode && invalid.Count == 0)
            {
                logger.LogWarning("OneSignal rejected the notification ({Status}): {Body}", status, Truncate(text));
                return PushSendResult.Permanent($"http_{status}", Truncate(text));
            }

            var id = json?["id"]?.GetValue<string>();
            return new PushSendResult(true, string.IsNullOrEmpty(id) ? null : id, message.ExternalUserIds.Count - invalid.Count, invalid, null, null, false);
        }
    }

    /// <summary>The request body sent to OneSignal (exposed for contract tests).</summary>
    public JsonObject BuildBody(PushMessage message)
    {
        var headings = WithEnglish(message.Headings);
        var contents = WithEnglish(message.Contents);
        var body = new JsonObject
        {
            ["app_id"] = _options.AppId,
            ["target_channel"] = "push",
            ["include_aliases"] = new JsonObject { ["external_id"] = new JsonArray(message.ExternalUserIds.Select(id => (JsonNode)JsonValue.Create(id.ToString())!).ToArray()) },
            ["headings"] = headings,
            ["contents"] = contents,
            ["data"] = JsonSerializer.SerializeToNode(message.Data),
            ["priority"] = message.Priority == "high" ? 10 : 5,
            ["idempotency_key"] = message.IdempotencyKey,
        };
        if (_options.AndroidChannels.TryGetValue(message.Category, out var channel) && !string.IsNullOrWhiteSpace(channel))
        {
            body["android_channel_id"] = channel;
        }

        if (message.TtlSeconds is { } ttl) body["ttl"] = ttl;
        if (!string.IsNullOrWhiteSpace(message.CollapseId)) body["collapse_id"] = message.CollapseId;
        if (message.Buttons is { Count: > 0 } buttons)
        {
            body["buttons"] = new JsonArray(buttons.Select(b => (JsonNode)new JsonObject { ["id"] = b.Id, ["text"] = b.TextAr }).ToArray());
        }

        return body;
    }

    private static JsonObject WithEnglish(IReadOnlyDictionary<string, string> texts)
    {
        var node = new JsonObject();
        var en = texts.GetValueOrDefault("en");
        node["en"] = string.IsNullOrEmpty(en) ? texts.Values.FirstOrDefault() ?? string.Empty : en;
        foreach (var (lang, value) in texts.Where(t => t.Key != "en"))
        {
            node[lang] = value;
        }

        return node;
    }

    private static List<Guid> InvalidAliases(JsonNode? json, IReadOnlyList<Guid> recipients)
    {
        var errors = json?["errors"];
        if (errors is JsonObject obj && obj["invalid_aliases"]?["external_id"] is JsonArray ids)
        {
            return ids.Select(n => Guid.TryParse(n?.GetValue<string>(), out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
        }

        if (errors is JsonArray list && list.Any(e => e?.GetValue<string>()?.Contains("not subscribed", StringComparison.OrdinalIgnoreCase) == true))
        {
            return recipients.ToList();
        }

        return [];
    }

    private static string Truncate(string text) => text.Length > 480 ? text[..480] : text;
}
