using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Api.Modules.Trips;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

/// <summary>Result of <c>POST /wallet/topups</c>: <c>201</c> when captured, <c>202</c> while a 3-D Secure action is pending.</summary>
public sealed record TopupOutcome(bool Accepted, object Body);

/// <summary>
/// Payment state machine and its effects (doc 08 §F11.4): applies gateway results, webhooks, return pages and the sandbox challenge; credits
/// card top-ups; starts matching for a trip whose 3-D Secure authorization completed and cancels it when the authorization fails. Gateway calls
/// always run outside database transactions.
/// </summary>
public sealed class PaymentService(
    AtaDbContext db,
    IPaymentGatewayResolver gateways,
    SandboxGateway sandbox,
    LedgerService ledger,
    INotificationDispatcher notifications,
    ITripNotifier realtime,
    TripEventRecorder events,
    TripReadService tripReads,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<PaymentsOptions> options,
    ILogger<PaymentService> logger)
{
    public const string TopupReference = "payment";
    private readonly PaymentsOptions _options = options.Value;
    private readonly List<(Guid UserId, PaymentUpdatedEvent Event)> _pendingEvents = [];
    private readonly List<Trip> _pendingTrips = [];

    public PaymentsOptions Options => _options;

    public PaymentConfigDto GetConfig()
    {
        var provider = _options.Provider.ToLowerInvariant();
        var sandboxMode = provider == PaymentProviders.Sandbox;
        return new PaymentConfigDto(
            provider,
            sandboxMode ? _options.Sandbox.PublishableKey : _options.Moyasar.PublishableKey,
            Payment.DefaultCurrency,
            ["mada", "visa", "mastercard"],
            new ApplePayConfigDto(sandboxMode || !string.IsNullOrWhiteSpace(_options.Moyasar.ApplePayMerchantId), sandboxMode ? null : _options.Moyasar.ApplePayMerchantId),
            sandboxMode ? [SandboxGateway.Mada, SandboxGateway.Visa, SandboxGateway.ThreeDs, SandboxGateway.Declined] : null);
    }

    public async Task<PaymentDto> GetOwnAsync(Guid paymentId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        return await ToDtoAsync(payment, ct);
    }

    public async Task<PaymentDto> ToDtoAsync(Payment payment, CancellationToken ct)
    {
        PaymentCardRefDto? card = null;
        if (payment.PaymentMethodId is { } methodId)
        {
            card = await db.PaymentMethods.AsNoTracking().Where(m => m.Id == methodId).Select(m => new PaymentCardRefDto(m.Brand, m.Last4)).FirstOrDefaultAsync(ct);
        }

        return new PaymentDto(payment.Id, payment.Purpose, payment.Status, payment.Method, payment.Amount, payment.AuthorizedAmount, payment.CapturedAmount,
            payment.RefundedAmount, payment.Currency, card, ActionOf(payment), payment.FailureCode, payment.FailureMessage, payment.TripId, payment.CreatedAt, payment.CapturedAt);
    }

    public static PaymentActionDto? ActionOf(Payment payment) =>
        payment.Status == PaymentStatus.Initiated && payment.ActionUrl is { } url ? new PaymentActionDto("redirect", url, payment.ActionExpiresAt) : null;

    /// <summary>Runs a gateway call with a timeout; timeouts, network errors and an unavailable provider become transient results.</summary>
    public async Task<GatewayResult> CallAsync(Func<CancellationToken, Task<GatewayResult>> call, int timeoutSeconds, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));
        try
        {
            return await call(timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return GatewayResult.Transient("timeout", "The payment gateway did not answer in time");
        }
        catch (HttpRequestException ex)
        {
            return GatewayResult.Transient("network_error", ex.Message);
        }
        catch (DomainException ex) when (ex.Code == ErrorCodes.PaymentProviderUnavailable)
        {
            return GatewayResult.Transient(ErrorCodes.PaymentProviderUnavailable, "The payment provider is not configured");
        }
    }

    /// <summary>
    /// Applies a direct gateway result to <paramref name="payment"/>: records the gateway id/status, keeps a 3-D Secure action (with its
    /// <c>Payments:ActionTimeoutSeconds</c> expiry) and performs the state transition. Must run inside the caller's transaction.
    /// </summary>
    public async Task<bool> ApplyResultAsync(Payment payment, GatewayResult result, CancellationToken ct)
    {
        if (result.GatewayPaymentId is not null && payment.GatewayPaymentId is null)
        {
            payment.GatewayPaymentId = result.GatewayPaymentId;
        }

        if (result.RawStatus is not null) payment.GatewayStatus = result.RawStatus;
        if (result.IsTransient)
        {
            payment.FailureCode = result.FailureCode;
            payment.FailureMessage = result.FailureMessage;
            return false;
        }

        if (result.Status == GatewayStatus.RequiresAction)
        {
            payment.ActionUrl = result.ActionUrl;
            payment.ActionExpiresAt = clock.UtcNow.AddSeconds(_options.ActionTimeoutSeconds);
            return false;
        }

        return await TransitionAsync(payment, result.Status, result.CapturedAmount ?? result.AuthorizedAmount, result.FailureCode, result.FailureMessage, ct);
    }

    /// <summary>
    /// Moves the payment forward (older/repeated states return <c>false</c> and change nothing) and runs the effects: top-up credit,
    /// trip start after 3-D Secure, trip cancellation on failure, and the <c>trip_card_capture</c> journal of a pending capture.
    /// </summary>
    public async Task<bool> TransitionAsync(Payment payment, GatewayStatus status, decimal? amount, string? failureCode, string? failureMessage, CancellationToken ct)
    {
        var target = status switch
        {
            GatewayStatus.Authorized => PaymentStatus.Authorized,
            GatewayStatus.Captured => PaymentStatus.Captured,
            GatewayStatus.Failed => PaymentStatus.Failed,
            GatewayStatus.Voided => PaymentStatus.Voided,
            _ => (PaymentStatus?)null,
        };
        if (target is not { } next || !payment.CanMoveTo(next))
        {
            return false;
        }

        var previous = payment.Status;
        var now = clock.UtcNow;
        switch (next)
        {
            case PaymentStatus.Authorized:
                payment.MarkAuthorized(amount ?? payment.Amount, now);
                break;
            case PaymentStatus.Captured:
                payment.MarkCaptured(amount ?? payment.AuthorizedAmount ?? payment.Amount, now);
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed(failureCode ?? "payment_failed", failureMessage, now);
                break;
            case PaymentStatus.Voided:
                payment.MarkVoided(now);
                break;
        }

        await ApplyEffectsAsync(payment, previous, ct);
        _pendingEvents.Add((payment.UserId, new PaymentUpdatedEvent(payment.Id, payment.Purpose, payment.Status, payment.TripId, payment.FailureCode)));
        return true;
    }

    private async Task ApplyEffectsAsync(Payment payment, PaymentStatus previous, CancellationToken ct)
    {
        if (payment.Purpose == PaymentPurpose.Topup)
        {
            if (payment.Status == PaymentStatus.Captured && payment.WalletId is { } walletId)
            {
                var wallet = await db.Wallets.FirstAsync(w => w.Id == walletId, ct);
                var amount = payment.CapturedAmount ?? payment.Amount;
                var posted = await ledger.PostAsync(wallet, TransactionType.Topup, TransactionDirection.Credit, amount, LedgerAccounts.GatewayClearing,
                    payment.Method == PaymentInstrument.ApplePay ? "Apple Pay top-up" : "Card top-up", payment.IdempotencyKey, TopupReference, payment.Id, ct);
                if (posted is not null)
                {
                    await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.WalletTopup, payment.UserId,
                        NotificationPlaceholders.Of().Money("amount", amount).Money("balance", wallet.Balance), "payment", payment.Id), ct);
                }
            }
            else if (payment.Status == PaymentStatus.Failed && previous == PaymentStatus.Initiated && payment.ActionExpiresAt is not null)
            {
                await NotifyFailedAsync(payment, payment.Amount, ct);
            }

            return;
        }

        if (payment.Purpose != PaymentPurpose.Trip || payment.TripId is not { } tripId)
        {
            return;
        }

        var trip = db.Trips.Local.FirstOrDefault(t => t.Id == tripId) ?? await db.Trips.Include(t => t.Stops).FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null)
        {
            return;
        }

        if (payment.Status == PaymentStatus.Authorized && previous == PaymentStatus.Initiated && trip.Status == TripStatus.Requested)
        {
            trip.StartSearching();
            events.Add(trip.Id, TripEventTypes.PaymentAuthorized, TripActor.System, data: new { paymentId = payment.Id, amount = payment.AuthorizedAmount });
            events.Add(trip.Id, TripEventTypes.SearchStarted, TripActor.System);
            _pendingTrips.Add(trip);
        }
        else if (payment.Status == PaymentStatus.Failed && trip.Status == TripStatus.Requested)
        {
            trip.Cancel(CancelledBy.System, "payment_failed", clock.UtcNow);
            events.Add(trip.Id, TripEventTypes.PaymentFailed, TripActor.System, data: new { paymentId = payment.Id, payment.FailureCode });
            events.Add(trip.Id, TripEventTypes.Cancelled, TripActor.System, data: new { reasonCode = "payment_failed" });
            await NotifyFailedAsync(payment, payment.Amount, ct);
            _pendingTrips.Add(trip);
        }
        else if (payment.Status == PaymentStatus.Captured && PaymentMetadata.Get(payment).CapturePending)
        {
            var amount = payment.CapturedAmount ?? payment.Amount;
            await ledger.JournalAsync(JournalType.TripCardCapture, LedgerAccounts.GatewayClearing, LedgerAccounts.TripRevenue, amount, "trip", trip.Id,
                $"trip:{trip.Id}:capture", $"Trip {trip.TripNumber} card capture", ct);
            PaymentMetadata.Update(payment, m => m.Remove("capturePending"));
        }
    }

    public async Task NotifyFailedAsync(Payment payment, decimal amount, CancellationToken ct) =>
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.PaymentFailed, payment.UserId,
            NotificationPlaceholders.Of(("reason", payment.FailureMessage ?? payment.FailureCode)).Money("amount", amount),
            payment.TripId is null ? "payment" : "trip", payment.TripId ?? payment.Id, new Dictionary<string, object?> { ["paymentId"] = payment.Id }), ct);

    /// <summary>Publishes <c>PaymentUpdated</c> (and <c>TripUpdated</c> for affected trips) collected since the last call; call after commit.</summary>
    public async Task PublishPendingAsync(CancellationToken ct)
    {
        var pending = _pendingEvents.ToList();
        var trips = _pendingTrips.Distinct().ToList();
        _pendingEvents.Clear();
        _pendingTrips.Clear();
        foreach (var (userId, evt) in pending)
        {
            try
            {
                await realtime.PaymentUpdatedAsync(userId, evt, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "PaymentUpdated push failed");
            }
        }

        foreach (var trip in trips)
        {
            await tripReads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
        }
    }

    // ----- top-ups by card / Apple Pay -----

    public async Task<TopupOutcome> TopupAsync(Domain.Wallet.Wallet wallet, Wallet.TopupRequest request, string idempotencyKey, CancellationToken ct)
    {
        var method = request.Method == "apple_pay" ? PaymentInstrument.ApplePay : PaymentInstrument.Card;
        var key = $"topup:{wallet.Id:N}:{idempotencyKey}";
        var existing = await db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);
        if (existing is not null)
        {
            return await TopupOutcomeAsync(existing, wallet, ct);
        }

        var now = clock.UtcNow;
        var v = new Validator()
            .Rule(nameof(request.PaymentMethodId), method != PaymentInstrument.Card || request.PaymentMethodId is not null, "required")
            .Rule(nameof(request.ApplePayToken), method != PaymentInstrument.ApplePay || !string.IsNullOrWhiteSpace(request.ApplePayToken), "required")
            .Rule(nameof(request.ReturnUrl), request.ReturnUrl is null || request.ReturnUrl.Length <= 500, "max_length:500");
        v.ThrowIfInvalid();

        var dayStart = Formats.RiyadhMidnightUtc(Formats.RiyadhDate(now));
        var today = await db.WalletTransactions.Where(t => t.WalletId == wallet.Id && t.Type == TransactionType.Topup && t.CreatedAt >= dayStart).SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        v.Rule(nameof(request.Amount), today + request.Amount!.Value <= _options.MaxTopupPerDay, "daily_limit").ThrowIfInvalid();

        PaymentMethod? card = null;
        GatewaySource source;
        if (method == PaymentInstrument.Card)
        {
            card = await db.PaymentMethods.FirstOrDefaultAsync(m => m.Id == request.PaymentMethodId && m.UserId == wallet.UserId && m.Status != SavedCardStatus.Removed, ct)
                   ?? throw new DomainException(ErrorCodes.NotFound, new { paymentMethodId = request.PaymentMethodId });
            card.EnsureUsable(now);
            source = new GatewaySource("token", card.GatewayToken);
        }
        else
        {
            source = new GatewaySource("apple_pay", request.ApplePayToken!.Trim());
        }

        var payment = new Payment
        {
            UserId = wallet.UserId,
            Purpose = PaymentPurpose.Topup,
            WalletId = wallet.Id,
            PaymentMethodId = card?.Id,
            Method = method,
            Provider = card?.Provider ?? gateways.Current.Provider,
            Amount = request.Amount.Value,
            CaptureMode = CaptureMode.Auto,
            ReturnUrl = request.ReturnUrl ?? _options.DefaultReturnUrl,
            IdempotencyKey = key,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var gateway = gateways.Get(payment.Provider);
        var result = await CallAsync(t => gateway.PurchaseAsync(new GatewayChargeRequest(payment.Id, payment.Amount, payment.Currency, source,
            "ATA wallet top-up", CallbackUrl(payment.Provider), payment.IdempotencyKey, new Dictionary<string, string> { ["purpose"] = "topup" }), t), _options.GatewayTimeoutSeconds, ct);
        await db.InTransactionAsync(async () =>
        {
            await ApplyResultAsync(payment, result, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
        await PublishPendingAsync(ct);
        return await TopupOutcomeAsync(payment, wallet, ct);
    }

    private async Task<TopupOutcome> TopupOutcomeAsync(Payment payment, Domain.Wallet.Wallet wallet, CancellationToken ct)
    {
        switch (payment.Status)
        {
            case PaymentStatus.Captured or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded:
                var transaction = await db.WalletTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.IdempotencyKey == payment.IdempotencyKey, ct);
                var balance = await db.Wallets.AsNoTracking().Where(w => w.Id == wallet.Id).Select(w => w.Balance).FirstAsync(ct);
                return new TopupOutcome(false, new Wallet.TopupResponse(transaction?.Id, balance, payment.Id, payment.Status, null));
            case PaymentStatus.Failed:
                throw new DomainException(ErrorCodes.PaymentFailed, new { paymentId = payment.Id, failureCode = payment.FailureCode, failureMessage = payment.FailureMessage });
            default:
                return new TopupOutcome(true, new Wallet.TopupResponse(null, null, payment.Id, payment.Status, ActionOf(payment)));
        }
    }

    public string CallbackUrl(string provider) => $"{_options.PublicBaseUrl.TrimEnd('/')}/api/v1/payments/return/{provider}";

    // ----- webhooks, return pages, sandbox challenge -----

    public async Task HandleWebhookAsync(string provider, string rawBody, IHeaderDictionary headers, CancellationToken ct)
    {
        var gateway = gateways.Find(provider) ?? throw new DomainException(ErrorCodes.NotFound, new { provider });
        var parsed = gateway.ParseWebhook(rawBody, headers);
        if (!parsed.SignatureValid)
        {
            // Stored under a synthetic id so a forged event can never block the genuine one (UNIQUE(provider, event_id)).
            db.PaymentWebhookEvents.Add(new PaymentWebhookEvent
            {
                Provider = gateway.Provider,
                EventId = $"unverified:{Guid.CreateVersion7():N}",
                EventType = Truncate(parsed.EventType ?? "unknown", 60),
                GatewayPaymentId = parsed.GatewayPaymentId,
                SignatureValid = false,
                Payload = JsonPayload(rawBody),
                ProcessingStatus = WebhookProcessingStatus.Ignored,
                Error = "signature_invalid",
            });
            await db.SaveChangesAsync(ct);
            throw new DomainException(ErrorCodes.WebhookSignatureInvalid);
        }

        var eventId = Truncate(parsed.EventId ?? Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawBody))), 100);
        if (await db.PaymentWebhookEvents.AnyAsync(e => e.Provider == gateway.Provider && e.EventId == eventId, ct))
        {
            return;
        }

        var evt = new PaymentWebhookEvent
        {
            Provider = gateway.Provider,
            EventId = eventId,
            EventType = Truncate(parsed.EventType ?? "unknown", 60),
            GatewayPaymentId = parsed.GatewayPaymentId,
            SignatureValid = true,
            Payload = JsonPayload(rawBody),
        };
        db.PaymentWebhookEvents.Add(evt);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A concurrent delivery of the same event won the insert: it is processed there.
            db.Entry(evt).State = EntityState.Detached;
            return;
        }

        await ProcessWebhookEventAsync(evt, parsed, ct);
    }

    /// <summary>Matches the event to its payment and applies the transition in one transaction (also used by the retry job).</summary>
    public async Task ProcessWebhookEventAsync(PaymentWebhookEvent evt, WebhookParseResult parsed, CancellationToken ct)
    {
        evt.Attempts++;
        var payment = parsed.GatewayPaymentId is null
            ? null
            : await db.Payments.FirstOrDefaultAsync(p => p.Provider == evt.Provider && p.GatewayPaymentId == parsed.GatewayPaymentId, ct);
        if (payment is null || parsed.Status is null)
        {
            evt.ProcessingStatus = payment is null ? WebhookProcessingStatus.Failed : WebhookProcessingStatus.Ignored;
            evt.Error = payment is null ? "payment_not_found" : "unknown_status";
            evt.ProcessedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
            return;
        }

        await db.InTransactionAsync(async () =>
        {
            var changed = await TransitionAsync(payment, parsed.Status.Value, parsed.Amount, parsed.FailureCode, null, ct);
            evt.ProcessingStatus = changed ? WebhookProcessingStatus.Processed : WebhookProcessingStatus.Ignored;
            evt.Error = null;
            evt.ProcessedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }, ct);
        await PublishPendingAsync(ct);
    }

    /// <summary><c>GET /payments/return/{provider}?id=</c>: fetches the payment from the gateway, applies it and returns the app redirect URL.</summary>
    public async Task<string> HandleReturnAsync(string provider, string gatewayPaymentId, CancellationToken ct)
    {
        var gateway = gateways.Find(provider) ?? throw new DomainException(ErrorCodes.NotFound, new { provider });
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Provider == gateway.Provider && p.GatewayPaymentId == gatewayPaymentId, ct)
                      ?? throw new DomainException(ErrorCodes.NotFound);
        var result = await CallAsync(t => gateway.FetchAsync(gatewayPaymentId, t), _options.GatewayTimeoutSeconds, ct);
        await db.InTransactionAsync(async () =>
        {
            await ApplyResultAsync(payment, result, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
        await PublishPendingAsync(ct);
        return ReturnRedirect(payment);
    }

    /// <summary>Completes the simulated 3-D Secure page for a payment; <c>null</c> when <paramref name="paymentId"/> is not a payment.</summary>
    public async Task<string?> CompleteSandboxChallengeAsync(Guid paymentId, bool approve, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId && p.Provider == PaymentProviders.Sandbox, ct);
        if (payment is null)
        {
            return null;
        }

        if (payment.GatewayPaymentId is { } gatewayId && payment.Status == PaymentStatus.Initiated)
        {
            var result = sandbox.CompleteChallenge(gatewayId, approve);
            await db.InTransactionAsync(async () =>
            {
                await ApplyResultAsync(payment, result, ct);
                await db.SaveChangesAsync(ct);
            }, ct);
            await PublishPendingAsync(ct);
        }

        return ReturnRedirect(payment);
    }

    public string ReturnRedirect(Payment payment) =>
        QueryHelpers.AddQueryString(payment.ReturnUrl ?? _options.DefaultReturnUrl, new Dictionary<string, string?>
        {
            ["paymentId"] = payment.Id.ToString(),
            ["status"] = JsonNamingPolicy.SnakeCaseLower.ConvertName(payment.Status.ToString()),
        });

    public static string JsonPayload(string raw)
    {
        try
        {
            using var _ = JsonDocument.Parse(raw);
            return raw;
        }
        catch (JsonException)
        {
            return new JsonObject { ["raw"] = raw.Length > 4000 ? raw[..4000] : raw }.ToJsonString();
        }
    }

    private static string Truncate(string value, int max) => value.Length > max ? value[..max] : value;
}

/// <summary>Typed access to <c>payments.metadata</c>.</summary>
public static class PaymentMetadata
{
    public sealed record Values(bool CapturePending, int CaptureAttempts, decimal? AmountDue);

    public static Values Get(Payment payment)
    {
        var node = Parse(payment.Metadata);
        return new Values(
            node["capturePending"]?.GetValue<bool>() == true,
            node["captureAttempts"]?.GetValue<int>() ?? 0,
            node["amountDue"] is { } due ? due.GetValue<decimal>() : null);
    }

    public static void Update(Payment payment, Action<JsonObject> change)
    {
        var node = Parse(payment.Metadata);
        change(node);
        payment.Metadata = node.Count == 0 ? null : node.ToJsonString();
    }

    private static JsonObject Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonNode.Parse(json) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
