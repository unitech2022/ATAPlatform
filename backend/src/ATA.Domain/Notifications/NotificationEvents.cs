namespace ATA.Domain.Notifications;

/// <summary>Default (code-level) text of an event, used when no <c>notification_templates</c> row exists and by the seeder.</summary>
public sealed record NotificationText(string TitleAr, string TitleEn, string BodyAr, string BodyEn);

public sealed record NotificationButton(string Id, string TextAr, string TextEn);

/// <summary>
/// One row of the event catalogue (doc 08 §F13.2). <see cref="Recipients"/>: P passenger, D driver, O operations, C trusted contact,
/// A corporate admin, G corporate guest. Critical events bypass the user's preferences.
/// </summary>
public sealed record NotificationEventDefinition(
    string Code,
    NotificationCategory Category,
    bool IsCritical,
    string Recipients,
    IReadOnlyList<NotificationChannel> DefaultChannels,
    IReadOnlyList<NotificationChannel> AllowedChannels,
    IReadOnlyList<string> Placeholders,
    string? DeepLink,
    NotificationText Text,
    string Priority = "normal",
    int? TtlSeconds = null,
    IReadOnlyList<NotificationButton>? Buttons = null)
{
    /// <summary><c>offer.received</c> never creates an inbox row.</summary>
    public bool CreatesInbox => Code != NotificationTypes.OfferReceived;
}

/// <summary>The catalogue of notification event codes shared by every feature (single source of truth for codes, channels and defaults).</summary>
public static class NotificationEvents
{
    private const NotificationChannel I = NotificationChannel.Inapp;
    private const NotificationChannel P = NotificationChannel.Push;
    private const NotificationChannel S = NotificationChannel.Sms;

    private static readonly Dictionary<string, string> Legacy = new(StringComparer.Ordinal)
    {
        ["driver_application_approved"] = NotificationTypes.DriverApplicationApproved,
        ["driver_application_rejected"] = NotificationTypes.DriverApplicationRejected,
        ["driver_application_under_review"] = NotificationTypes.DriverUnderReview,
        ["driver_suspended"] = NotificationTypes.DriverSuspended,
        ["driver_reinstated"] = NotificationTypes.DriverReinstated,
        ["trip_driver_assigned"] = NotificationTypes.TripDriverAssigned,
        ["trip_driver_arrived"] = NotificationTypes.TripDriverArrived,
        ["trip_completed"] = NotificationTypes.TripCompleted,
        ["trip_cancelled"] = NotificationTypes.TripCancelled,
        ["trip_no_drivers"] = NotificationTypes.TripNoDrivers,
    };

    public static readonly IReadOnlyList<NotificationEventDefinition> All =
    [
        E("trip.driver_assigned", NotificationCategory.Trips, true, "P", [I, P], ["driverName", "vehicle", "plateNumber", "etaMinutes"], "ata://trip/{tripId}",
            T("تم تعيين كابتن", "Driver assigned", "{driverName} في طريقه إليك · {vehicle} {plateNumber} · يصل خلال {etaMinutes} د", "{driverName} is on the way · {vehicle} {plateNumber} · arriving in {etaMinutes} min"), priority: "high"),
        E("trip.driver_arrived", NotificationCategory.Trips, true, "P", [I, P], ["driverName", "plateNumber", "freeWaitingMinutes"], "ata://trip/{tripId}",
            T("وصل الكابتن", "Your driver has arrived", "{driverName} بانتظارك · {plateNumber}. الانتظار مجاني لمدة {freeWaitingMinutes} دقائق.", "{driverName} is waiting · {plateNumber}. Waiting is free for {freeWaitingMinutes} minutes."), priority: "high"),
        E("trip.started", NotificationCategory.Trips, false, "P", [P], ["dropoffName"], "ata://trip/{tripId}",
            T("بدأت الرحلة", "Trip started", "رحلتك إلى {dropoffName} بدأت. رحلة سعيدة!", "Your trip to {dropoffName} has started. Enjoy the ride!")),
        E("trip.completed", NotificationCategory.Trips, false, "P", [I, P], ["fare", "tripNumber"], "ata://rides/{tripId}/receipt",
            T("انتهت الرحلة", "Trip completed", "وصلت بسلامة. أجرة الرحلة {tripNumber}: {fare}", "You have arrived. Trip {tripNumber} fare: {fare}")),
        E("trip.cancelled", NotificationCategory.Trips, true, "P/D", [I, P], ["tripNumber", "cancelledBy", "fee"], "ata://rides/{tripId}",
            T("تم إلغاء الرحلة", "Trip cancelled", "تم إلغاء الرحلة {tripNumber} ({cancelledBy}).", "Trip {tripNumber} was cancelled ({cancelledBy})."), priority: "high"),
        E("trip.no_drivers", NotificationCategory.Trips, true, "P", [I, P], ["categoryName"], "ata://home",
            T("لم نجد كابتن", "No drivers available", "عذراً، لا يوجد كابتن {categoryName} متاح الآن. حاول مرة أخرى.", "Sorry, no {categoryName} driver is available right now. Please try again.")),
        E("trip.favorite_fallback", NotificationCategory.Trips, false, "P", [P], ["driverName"], "ata://trip/{tripId}",
            T("كابتن بديل", "Another driver", "كابتنك المفضل غير متاح، {driverName} في طريقه إليك.", "Your favourite driver is unavailable; {driverName} is on the way.")),
        E("trip.message", NotificationCategory.Trips, false, "P/D", [P], ["senderName", "preview"], "ata://trip/{tripId}/chat",
            T("رسالة جديدة", "New message", "{senderName}: {preview}", "{senderName}: {preview}"), priority: "high"),
        E("trip.payment_action_required", NotificationCategory.Trips, true, "P", [P], ["amount"], "ata://trip/{tripId}",
            T("أكمل التحقق من الدفع", "Complete payment verification", "يلزم تأكيد الدفع بمبلغ {amount} لبدء البحث عن كابتن.", "Confirm the {amount} payment to start searching for a driver."), priority: "high"),
        E("offer.received", NotificationCategory.Offers, true, "D", [P], ["pickupName", "driverNet", "etaMinutes"], "ata://driver/offer",
            T("طلب رحلة جديد", "New trip request", "{pickupName} · صافي {driverNet} · {etaMinutes} د", "{pickupName} · net {driverNet} · {etaMinutes} min"), priority: "high", ttl: 20),
        E("rating.reminder", NotificationCategory.Trips, false, "P/D", [I, P], ["driverName", "passengerName"], "ata://rate/{tripId}",
            T("قيّم رحلتك", "Rate your trip", "كيف كانت رحلتك؟ شاركنا تقييمك.", "How was your trip? Share your rating.")),
        E("scheduled.booked", NotificationCategory.Trips, false, "P", [I, P], ["scheduledAt", "pickupName"], "ata://scheduled/{tripId}",
            T("تم حجز رحلتك", "Trip booked", "رحلتك المجدولة {scheduledAt} من {pickupName} مؤكدة.", "Your scheduled trip at {scheduledAt} from {pickupName} is booked.")),
        E("scheduled.reminder", NotificationCategory.Trips, false, "P/D", [I, P], ["scheduledAt", "minutesBefore", "pickupName"], "ata://scheduled/{tripId}",
            T("تذكير برحلة مجدولة", "Scheduled trip reminder", "رحلتك من {pickupName} بعد {minutesBefore} دقيقة ({scheduledAt}).", "Your trip from {pickupName} starts in {minutesBefore} minutes ({scheduledAt}).")),
        E("scheduled.driver_reserved", NotificationCategory.Trips, false, "P", [I, P], ["driverName", "scheduledAt"], "ata://scheduled/{tripId}",
            T("تم حجز كابتن", "Driver reserved", "{driverName} سيقلك في {scheduledAt}.", "{driverName} will pick you up at {scheduledAt}.")),
        E("scheduled.confirm_request", NotificationCategory.Trips, true, "D", [I, P], ["scheduledAt", "pickupName", "deadlineMinutes"], "ata://driver/scheduled/{tripId}",
            T("أكّد رحلتك المجدولة", "Confirm your scheduled trip", "أكّد رحلة {scheduledAt} من {pickupName} خلال {deadlineMinutes} دقيقة.", "Confirm the {scheduledAt} trip from {pickupName} within {deadlineMinutes} minutes.")),
        E("scheduled.reservation_released", NotificationCategory.Trips, false, "P/D", [I, P], ["scheduledAt", "reason"], "ata://scheduled/{tripId}",
            T("تم إلغاء الحجز", "Reservation released", "تم إلغاء حجز رحلة {scheduledAt}: {reason}", "The {scheduledAt} reservation was released: {reason}")),
        E("scheduled.favorite_request", NotificationCategory.Trips, false, "D", [I, P], ["passengerName", "scheduledAt"], "ata://driver/scheduled/{tripId}",
            T("طلب من راكب مفضل", "Request from a regular passenger", "{passengerName} يطلبك لرحلة {scheduledAt}.", "{passengerName} requested you for a trip at {scheduledAt}.")),
        E("scheduled.rematched", NotificationCategory.Trips, false, "P", [I, P], ["scheduledAt"], "ata://scheduled/{tripId}",
            T("كابتن جديد لرحلتك", "New driver for your trip", "عيّنا كابتن جديداً لرحلة {scheduledAt}.", "We assigned a new driver for your {scheduledAt} trip.")),
        E("payment.succeeded", NotificationCategory.Wallet, false, "P", [I], ["amount", "purpose"], "ata://wallet",
            T("تم الدفع", "Payment successful", "تم دفع {amount} بنجاح ({purpose}).", "{amount} paid successfully ({purpose}).")),
        E("payment.failed", NotificationCategory.Wallet, true, "P", [I, P], ["amount", "reason"], "ata://wallet",
            T("تعذّر الدفع", "Payment failed", "تعذّر تحصيل {amount}: {reason}", "We could not collect {amount}: {reason}")),
        E("payment.refunded", NotificationCategory.Wallet, false, "P", [I, P], ["amount", "tripNumber"], "ata://wallet/transactions",
            T("تم الاسترداد", "Refund issued", "أعدنا لك {amount} عن الرحلة {tripNumber}.", "We refunded {amount} for trip {tripNumber}.")),
        E("wallet.topup", NotificationCategory.Wallet, false, "P/D", [I, P], ["amount", "balance"], "ata://wallet",
            T("تم شحن المحفظة", "Wallet topped up", "أضيف {amount} إلى محفظتك. الرصيد: {balance}", "{amount} was added to your wallet. Balance: {balance}")),
        E("cancellation.fee_charged", NotificationCategory.Wallet, false, "P", [I, P], ["fee", "tripNumber"], "ata://rides/{tripId}",
            T("رسوم إلغاء", "Cancellation fee", "تم احتساب رسوم إلغاء {fee} للرحلة {tripNumber}.", "A {fee} cancellation fee was charged for trip {tripNumber}.")),
        E("cancellation.compensation", NotificationCategory.Wallet, false, "D", [I], ["amount", "tripNumber"], "ata://driver/earnings",
            T("تعويض إلغاء", "Cancellation compensation", "أضيف {amount} تعويضاً عن إلغاء الرحلة {tripNumber}.", "{amount} was added as compensation for cancelled trip {tripNumber}.")),
        E("payout.status", NotificationCategory.Wallet, false, "D", [I, P], ["amount", "status", "payoutNumber"], "ata://driver/payouts",
            T("تحديث طلب التحويل", "Payout update", "طلب التحويل {payoutNumber} بمبلغ {amount}: {status}", "Payout {payoutNumber} of {amount}: {status}")),
        E("settlement.ready", NotificationCategory.Wallet, false, "D", [I], ["periodLabel", "netAmount"], "ata://driver/settlements/{settlementId}",
            T("كشف التسوية جاهز", "Settlement statement ready", "كشف {periodLabel} جاهز. الصافي: {netAmount}", "Your {periodLabel} statement is ready. Net: {netAmount}")),
        E("incentive.new", NotificationCategory.Promotions, false, "D", [I, P], ["incentiveName", "reward"], "ata://driver/incentives/{incentiveId}",
            T("حافز جديد", "New incentive", "{incentiveName}: اربح {reward}", "{incentiveName}: earn {reward}")),
        E("incentive.achieved", NotificationCategory.Wallet, false, "D", [I, P], ["incentiveName", "reward"], "ata://driver/incentives/{incentiveId}",
            T("حققت الحافز", "Incentive achieved", "مبروك! أضيف {reward} لحافز {incentiveName}.", "Congratulations! {reward} added for {incentiveName}.")),
        E("driver.application.under_review", NotificationCategory.System, true, "D", [I, P], ["applicationNumber"], "ata://driver/pending",
            T("طلبك قيد المراجعة", "Your application is under review", "بدأت الإدارة مراجعة طلبك رقم {applicationNumber}.", "Our team started reviewing application {applicationNumber}.")),
        E("driver.application.approved", NotificationCategory.System, true, "D", [I, P, S], ["applicationNumber"], "ata://driver",
            T("تم تفعيل حسابك", "Your account is activated", "تم اعتماد طلبك رقم {applicationNumber}. يمكنك الآن استقبال الرحلات.", "Application {applicationNumber} was approved. You can now go online.")),
        E("driver.application.rejected", NotificationCategory.System, true, "D", [I, P], ["reason"], "ata://driver/pending",
            T("تم رفض طلبك", "Your application was rejected", "تم رفض طلبك: {reason}", "Your application was rejected: {reason}")),
        E("driver.suspended", NotificationCategory.System, true, "D", [I, P], ["reason"], "ata://driver",
            T("تم تعليق حسابك", "Your account was suspended", "تم تعليق حساب السائق: {reason}", "Your driver account was suspended: {reason}")),
        E("driver.reinstated", NotificationCategory.System, true, "D", [I, P], ["reason"], "ata://driver",
            T("تمت إعادة تفعيل حسابك", "Your account was reinstated", "يمكنك الآن استقبال الرحلات مجدداً.", "You can go online and receive trips again.")),
        E("driver.tier_changed", NotificationCategory.System, false, "D", [I, P], ["tierName"], "ata://driver/tier",
            T("مستواك الجديد", "Your new tier", "أصبح مستواك {tierName}.", "Your tier is now {tierName}.")),
        E("document.expiring", NotificationCategory.System, true, "D", [I, P], ["documentName", "daysLeft", "expiresAt"], "ata://driver/documents",
            T("مستند قارب على الانتهاء", "Document expiring soon", "ينتهي {documentName} خلال {daysLeft} يوم. حدّثه لتستمر في استقبال الرحلات.", "{documentName} expires in {daysLeft} days. Update it to keep receiving trips."), extraAllowed: [S]),
        E("document.expired", NotificationCategory.System, true, "D", [I, P, S], ["documentName"], "ata://driver/documents",
            T("انتهى مستند", "Document expired", "انتهت صلاحية {documentName}. حدّثه لتتمكن من الاتصال.", "{documentName} has expired. Update it to go online again.")),
        E("reliability.warning", NotificationCategory.System, true, "P/D", [I, P], ["level", "cancellationRate"], "ata://reliability",
            T("تنبيه الموثوقية", "Reliability warning", "نسبة إلغائك {cancellationRate}. المستوى: {level}", "Your cancellation rate is {cancellationRate}. Level: {level}")),
        E("reliability.restricted", NotificationCategory.System, true, "P/D", [I, P], ["level", "restrictedUntil"], "ata://reliability",
            T("تقييد مؤقت", "Temporary restriction", "تم تقييد حسابك حتى {restrictedUntil} ({level}).", "Your account is restricted until {restrictedUntil} ({level}).")),
        E("support.reply", NotificationCategory.System, false, "P/D", [I, P], ["ticketNumber", "preview"], "ata://support/tickets/{ticketId}",
            T("رد من الدعم", "Support replied", "التذكرة {ticketNumber}: {preview}", "Ticket {ticketNumber}: {preview}")),
        E("support.status", NotificationCategory.System, false, "P/D", [I], ["ticketNumber", "status"], "ata://support/tickets/{ticketId}",
            T("تحديث التذكرة", "Ticket update", "حالة التذكرة {ticketNumber}: {status}", "Ticket {ticketNumber} status: {status}")),
        E("safety.check", NotificationCategory.Safety, true, "P", [I, P], ["alertType"], "ata://safety/check/{alertId}",
            T("هل أنت بخير؟", "Are you OK?", "لاحظنا {alertType} أثناء رحلتك. هل أنت بخير؟", "We noticed {alertType} during your trip. Are you OK?"), priority: "high",
            buttons: [new NotificationButton("ok", "أنا بخير", "I'm OK"), new NotificationButton("help", "أحتاج مساعدة", "I need help")]),
        E("safety.alert", NotificationCategory.Safety, true, "O", [P], ["caseNumber", "type", "priority", "userName"], "/safety/cases/{caseId}",
            T("بلاغ سلامة", "Safety alert", "{caseNumber} · {type} · {priority} · {userName}", "{caseNumber} · {type} · {priority} · {userName}"), priority: "high", extraAllowed: [S]),
        E("safety.sos_contact", NotificationCategory.Safety, true, "C", [S], ["userName", "shareUrl", "mapsUrl"], null,
            T("", "", "تنبيه طوارئ من {userName} عبر ATA. الموقع: {mapsUrl} تتبع الرحلة: {shareUrl}", "Emergency alert from {userName} via ATA. Location: {mapsUrl} Track the trip: {shareUrl}")),
        E("safety.trip_shared", NotificationCategory.Safety, true, "C", [S], ["userName", "shareUrl"], null,
            T("", "", "{userName} شارك رحلته معك عبر ATA: {shareUrl}", "{userName} shared their trip with you via ATA: {shareUrl}")),
        E("safety.case_update", NotificationCategory.Safety, false, "P/D", [I, P], ["caseNumber", "status"], "ata://safety/cases/{caseId}",
            T("تحديث البلاغ", "Case update", "البلاغ {caseNumber}: {status}", "Case {caseNumber}: {status}")),
        E("lost_item.reported", NotificationCategory.Trips, false, "D", [I, P], ["tripNumber", "itemCategory"], "ata://driver/lost-items/{reportId}",
            T("بلاغ مفقودات", "Lost item report", "أبلغ راكب الرحلة {tripNumber} عن فقدان {itemCategory}.", "The passenger of trip {tripNumber} reported a lost {itemCategory}.")),
        E("lost_item.update", NotificationCategory.Trips, false, "P", [I, P], ["status"], "ata://support/tickets/{ticketId}",
            T("تحديث المفقودات", "Lost item update", "حالة بلاغك: {status}", "Your report status: {status}")),
        E("promo.new", NotificationCategory.Promotions, false, "P", [I, P], ["code", "title"], "ata://promotions",
            T("عرض جديد", "New offer", "{title} · استخدم الكود {code}", "{title} · use code {code}")),
        E("corporate.invitation", NotificationCategory.System, false, "P", [I, P, S], ["companyName"], "ata://corporate/invitations",
            T("دعوة حساب شركة", "Corporate invitation", "دعتك {companyName} للانضمام إلى حساب الشركة.", "{companyName} invited you to its corporate account.")),
        E("corporate.guest_trip", NotificationCategory.Trips, true, "G", [S], ["companyName", "pickupName", "pickupTime", "shareUrl", "pin"], null,
            T("", "", "حجزت لك {companyName} رحلة من {pickupName} الساعة {pickupTime}. الرمز {pin}. التتبع: {shareUrl}", "{companyName} booked you a ride from {pickupName} at {pickupTime}. PIN {pin}. Track: {shareUrl}")),
        E("corporate.invoice_issued", NotificationCategory.System, false, "A", [I, S], ["invoiceNumber", "total"], "/business/invoices/{invoiceId}",
            T("فاتورة جديدة", "New invoice", "صدرت الفاتورة {invoiceNumber} بمبلغ {total}.", "Invoice {invoiceNumber} of {total} was issued.")),
        E("privacy.export_ready", NotificationCategory.System, false, "P/D", [I, P], ["expiresAt"], "ata://account/privacy",
            T("بياناتك جاهزة", "Your data export is ready", "ملف بياناتك جاهز للتحميل حتى {expiresAt}.", "Your data export is ready to download until {expiresAt}.")),
        E("account.deletion_scheduled", NotificationCategory.System, true, "P/D", [I, S], ["scheduledFor"], "ata://account",
            T("جدولة حذف الحساب", "Account deletion scheduled", "سيُحذف حسابك في {scheduledFor}. سجّل الدخول للإلغاء.", "Your account will be deleted on {scheduledFor}. Sign in to cancel.")),
        E("campaign.broadcast", NotificationCategory.Promotions, false, "*", [I, P], ["title", "body"], null,
            T("{title}", "{title}", "{body}", "{body}"), extraAllowed: [S]),
    ];

    private static readonly Dictionary<string, NotificationEventDefinition> ByCode = All.ToDictionary(e => e.Code, StringComparer.Ordinal);

    public static NotificationEventDefinition? Find(string code) => ByCode.GetValueOrDefault(code);

    /// <summary>Maps a legacy snake_case type (pre-F13 rows) to its dotted code; other values are returned unchanged.</summary>
    public static string NormalizeLegacy(string type) => Legacy.GetValueOrDefault(type, type);

    /// <summary>
    /// The preference flag that can switch an event category off (push and SMS only): trips → trips, wallet → wallet, safety → safety,
    /// promotions → offers. <c>offers</c> (driver trip offers) and <c>system</c> are mandatory and return <c>null</c>.
    /// </summary>
    public static Func<NotificationPreference, bool>? PreferenceOf(NotificationCategory category) => category switch
    {
        NotificationCategory.Trips => p => p.Trips,
        NotificationCategory.Wallet => p => p.Wallet,
        NotificationCategory.Safety => p => p.Safety,
        NotificationCategory.Promotions => p => p.Offers,
        _ => null,
    };

    private static NotificationText T(string titleAr, string titleEn, string bodyAr, string bodyEn) => new(titleAr, titleEn, bodyAr, bodyEn);

    private static NotificationEventDefinition E(
        string code, NotificationCategory category, bool critical, string recipients, NotificationChannel[] channels, string[] placeholders,
        string? deepLink, NotificationText text, string priority = "normal", int? ttl = null, NotificationChannel[]? extraAllowed = null,
        NotificationButton[]? buttons = null)
    {
        // Users with an account can always receive inbox + push; SMS is only allowed for critical or explicitly listed events;
        // SMS-only recipients (trusted contacts, corporate guests) have no account.
        var smsOnly = recipients is "C" or "G";
        var allowed = new List<NotificationChannel>(channels);
        if (!smsOnly)
        {
            if (!allowed.Contains(NotificationChannel.Inapp) && code != NotificationTypes.OfferReceived) allowed.Add(NotificationChannel.Inapp);
            if (!allowed.Contains(NotificationChannel.Push)) allowed.Add(NotificationChannel.Push);
        }

        if ((critical || extraAllowed?.Contains(NotificationChannel.Sms) == true) && !allowed.Contains(NotificationChannel.Sms)) allowed.Add(NotificationChannel.Sms);
        return new NotificationEventDefinition(code, category, critical, recipients, channels, allowed.OrderBy(c => c).ToList(), placeholders, deepLink, text, priority, ttl, buttons);
    }
}
