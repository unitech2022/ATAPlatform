using ATA.Domain.Common;

namespace ATA.Api.Common;

public sealed record ErrorBody(string Code, string Message, object? Details);

public sealed record ErrorEnvelope(ErrorBody Error);

/// <summary>Maps error codes to HTTP status codes and localized (ar/en) messages.</summary>
public static class ErrorCatalog
{
    private sealed record Entry(int Status, string Ar, string En);

    private static readonly Dictionary<string, Entry> Entries = new(StringComparer.Ordinal)
    {
        [ErrorCodes.ValidationFailed] = new(StatusCodes.Status422UnprocessableEntity, "البيانات المدخلة غير صحيحة", "The submitted data is invalid"),
        [ErrorCodes.Unauthorized] = new(StatusCodes.Status401Unauthorized, "يجب تسجيل الدخول", "Authentication is required"),
        [ErrorCodes.InvalidCredentials] = new(StatusCodes.Status401Unauthorized, "اسم المستخدم أو كلمة المرور غير صحيحة", "Invalid username or password"),
        [ErrorCodes.Forbidden] = new(StatusCodes.Status403Forbidden, "ليس لديك صلاحية لهذا الإجراء", "You are not allowed to perform this action"),
        [ErrorCodes.NotFound] = new(StatusCodes.Status404NotFound, "العنصر غير موجود", "The requested resource was not found"),
        [ErrorCodes.Conflict] = new(StatusCodes.Status409Conflict, "لا يمكن تنفيذ العملية في الحالة الحالية", "The operation conflicts with the current state"),
        [ErrorCodes.RateLimited] = new(StatusCodes.Status429TooManyRequests, "طلبات كثيرة، حاول لاحقاً", "Too many requests, please try again later"),
        [ErrorCodes.OtpInvalid] = new(StatusCodes.Status400BadRequest, "رمز التحقق غير صحيح", "The verification code is incorrect"),
        [ErrorCodes.OtpExpired] = new(StatusCodes.Status400BadRequest, "انتهت صلاحية رمز التحقق، اطلب رمزاً جديداً", "The verification code has expired, request a new one"),
        [ErrorCodes.OtpLocked] = new(StatusCodes.Status429TooManyRequests, "تم تجاوز عدد المحاولات، اطلب رمزاً جديداً", "Too many attempts, request a new code"),
        [ErrorCodes.PhoneInvalid] = new(StatusCodes.Status400BadRequest, "رقم الجوال غير صحيح", "The phone number is invalid"),
        [ErrorCodes.AccountSuspended] = new(StatusCodes.Status403Forbidden, "الحساب موقوف، تواصل مع الدعم", "This account is suspended, please contact support"),
        [ErrorCodes.DriverNotApproved] = new(StatusCodes.Status403Forbidden, "لم يتم اعتماد حساب السائق بعد", "The driver account is not approved yet"),
        [ErrorCodes.FileTooLarge] = new(StatusCodes.Status422UnprocessableEntity, "حجم الملف يتجاوز الحد المسموح (10MB)", "The file exceeds the maximum size (10MB)"),
        [ErrorCodes.UnsupportedFileType] = new(StatusCodes.Status422UnprocessableEntity, "نوع الملف غير مدعوم (PDF/JPG/PNG)", "Unsupported file type (PDF/JPG/PNG)"),
        [ErrorCodes.InsufficientBalance] = new(StatusCodes.Status422UnprocessableEntity, "الرصيد غير كافٍ", "Insufficient balance"),
        [ErrorCodes.TripActiveExists] = new(StatusCodes.Status409Conflict, "لديك رحلة نشطة بالفعل", "You already have an active trip"),
        [ErrorCodes.OfferExpired] = new(StatusCodes.Status409Conflict, "انتهت صلاحية العرض", "The offer has expired"),
        [ErrorCodes.PinInvalid] = new(StatusCodes.Status400BadRequest, "رمز الرحلة غير صحيح", "The trip PIN is incorrect"),
        [ErrorCodes.PinLocked] = new(StatusCodes.Status429TooManyRequests, "تم تجاوز عدد محاولات إدخال رمز الرحلة", "Too many PIN attempts for this trip"),
        [ErrorCodes.OfferOutOfRange] = new(StatusCodes.Status422UnprocessableEntity, "السعر المقترح خارج النطاق المسموح", "The offered price is outside the allowed range"),
        [ErrorCodes.QuoteExpired] = new(StatusCodes.Status422UnprocessableEntity, "انتهت صلاحية عرض السعر، اطلب تسعيرة جديدة", "The fare quote has expired, request a new one"),
        [ErrorCodes.PaymentFailed] = new(StatusCodes.Status422UnprocessableEntity, "تعذّر إتمام الدفع", "The payment could not be completed"),
        [ErrorCodes.PaymentMethodExpired] = new(StatusCodes.Status422UnprocessableEntity, "البطاقة منتهية الصلاحية", "The card has expired"),
        [ErrorCodes.PaymentMethodInUse] = new(StatusCodes.Status409Conflict, "البطاقة مرتبطة برحلة جارية", "The card is linked to an active trip"),
        [ErrorCodes.PaymentProviderUnavailable] = new(StatusCodes.Status503ServiceUnavailable, "خدمة الدفع غير متاحة حالياً", "The payment service is currently unavailable"),
        [ErrorCodes.WebhookSignatureInvalid] = new(StatusCodes.Status401Unauthorized, "توقيع غير صالح", "Invalid signature"),
        [ErrorCodes.RefundExceedsAmount] = new(StatusCodes.Status422UnprocessableEntity, "مبلغ الاسترداد يتجاوز المبلغ المدفوع", "The refund exceeds the paid amount"),
        [ErrorCodes.FourEyesRequired] = new(StatusCodes.Status409Conflict, "يجب أن يعتمد العملية مستخدم آخر", "Another user must approve this operation"),
        [ErrorCodes.PayoutBelowMinimum] = new(StatusCodes.Status422UnprocessableEntity, "المبلغ أقل من الحد الأدنى للسحب", "The amount is below the minimum payout"),
        [ErrorCodes.PayoutPendingExists] = new(StatusCodes.Status409Conflict, "لديك طلب سحب قيد المعالجة", "You already have a pending payout request"),
        [ErrorCodes.IbanMissing] = new(StatusCodes.Status422UnprocessableEntity, "أضف رقم الآيبان أولاً", "Add your IBAN first"),
        [ErrorCodes.CashDebtLimitExceeded] = new(StatusCodes.Status403Forbidden, "تجاوزت مستحقات النقد الحد المسموح، سدّدها للاتصال", "Your cash dues exceed the limit; settle them to go online"),
        [ErrorCodes.OutstandingBalance] = new(StatusCodes.Status422UnprocessableEntity, "يوجد مبلغ مستحق على حسابك، اشحن المحفظة للمتابعة", "Your account has an outstanding balance; top up your wallet to continue"),
        [ErrorCodes.SettlementPeriodOverlap] = new(StatusCodes.Status409Conflict, "فترة التسوية متداخلة مع دفعة سابقة", "The settlement period overlaps an existing batch"),
        [ErrorCodes.UnknownEventCode] = new(StatusCodes.Status422UnprocessableEntity, "كود الحدث غير معروف", "Unknown notification event code"),
        [ErrorCodes.CampaignNotEditable] = new(StatusCodes.Status409Conflict, "لا يمكن تعديل الحملة في حالتها الحالية", "The campaign can no longer be edited"),
        [ErrorCodes.TemplatePlaceholderInvalid] = new(StatusCodes.Status422UnprocessableEntity, "القالب يحتوي عناصر نائبة غير معرّفة لهذا الحدث", "The template uses placeholders that are not defined for this event"),
        [ErrorCodes.ShareNotFound] = new(StatusCodes.Status404NotFound, "رابط التتبع غير موجود", "The tracking link was not found"),
        [ErrorCodes.ShareExpired] = new(StatusCodes.Status410Gone, "انتهت صلاحية رابط التتبع", "The tracking link has expired"),
        [ErrorCodes.TrustedContactsLimit] = new(StatusCodes.Status422UnprocessableEntity, "الحد الأقصى 5 جهات موثوقة", "You can add at most 5 trusted contacts"),
        [ErrorCodes.TrustedContactExists] = new(StatusCodes.Status409Conflict, "الجهة مضافة مسبقاً", "This contact is already added"),
        [ErrorCodes.ChatClosed] = new(StatusCodes.Status409Conflict, "المحادثة مغلقة لهذه الرحلة", "The chat is closed for this trip"),
        [ErrorCodes.LostItemWindowClosed] = new(StatusCodes.Status422UnprocessableEntity, "انتهت مدة الإبلاغ عن المفقودات", "The lost item reporting window has closed"),
        [ErrorCodes.CancellationReasonInvalid] = new(StatusCodes.Status422UnprocessableEntity, "سبب الإلغاء غير صالح", "The cancellation reason is not valid"),
        [ErrorCodes.CancellationFeeChanged] = new(StatusCodes.Status409Conflict, "تغيّرت رسوم الإلغاء، راجعها وأعد المحاولة", "The cancellation fee changed, review it and try again"),
        [ErrorCodes.NoShowTooEarly] = new(StatusCodes.Status422UnprocessableEntity, "لم تنتهِ مدة الانتظار المطلوبة بعد", "The required waiting time has not elapsed yet"),
        [ErrorCodes.AccountRestricted] = new(StatusCodes.Status403Forbidden, "حسابك مقيّد مؤقتاً بسبب تكرار الإلغاء", "Your account is temporarily restricted because of repeated cancellations"),
        [ErrorCodes.RatingWindowClosed] = new(StatusCodes.Status422UnprocessableEntity, "انتهت مدة التقييم", "The rating window has closed"),
        [ErrorCodes.RatingExists] = new(StatusCodes.Status409Conflict, "تم تقييم هذه الرحلة مسبقاً", "This trip has already been rated"),
        [ErrorCodes.PromoNotFound] = new(StatusCodes.Status404NotFound, "كود الخصم غير صحيح", "The promo code is not valid"),
        [ErrorCodes.PromoExpired] = new(StatusCodes.Status422UnprocessableEntity, "انتهت صلاحية كود الخصم", "The promo code has expired"),
        [ErrorCodes.PromoNotEligible] = new(StatusCodes.Status422UnprocessableEntity, "كود الخصم لا ينطبق على هذه الرحلة", "The promo code does not apply to this trip"),
        [ErrorCodes.PromoUsageLimitReached] = new(StatusCodes.Status422UnprocessableEntity, "تم استنفاد كود الخصم", "The promo code has been used up"),
        [ErrorCodes.IncentiveOptInClosed] = new(StatusCodes.Status409Conflict, "الاشتراك في هذا الحافز غير متاح", "Joining this incentive is not available"),
        [ErrorCodes.FavoriteNotEligible] = new(StatusCodes.Status422UnprocessableEntity, "يمكنك إضافة الكابتن بعد إكمال رحلة معه", "You can add a driver after completing a trip with them"),
        [ErrorCodes.FavoriteExists] = new(StatusCodes.Status409Conflict, "الكابتن موجود في مفضلتك بالفعل", "This driver is already in your favourites"),
        [ErrorCodes.FavoritesLimit] = new(StatusCodes.Status422UnprocessableEntity, "وصلت إلى الحد الأقصى للكباتن المفضلين", "You have reached the maximum number of favourite drivers"),
        [ErrorCodes.ScheduleWindowExceeded] = new(StatusCodes.Status422UnprocessableEntity, "لا يمكن الجدولة لأكثر من 7 أيام من الآن", "You cannot schedule a trip more than 7 days ahead"),
        [ErrorCodes.ScheduleLeadTooShort] = new(StatusCodes.Status422UnprocessableEntity, "يجب أن يكون الموعد بعد 30 دقيقة على الأقل", "The pickup time must be at least 30 minutes from now"),
        [ErrorCodes.ScheduledLimitReached] = new(StatusCodes.Status422UnprocessableEntity, "وصلت للحد الأقصى من الرحلات المجدولة", "You have reached the maximum number of scheduled trips"),
        [ErrorCodes.ReservationTaken] = new(StatusCodes.Status409Conflict, "تم حجز هذه الرحلة من كابتن آخر", "This trip was reserved by another driver"),
        [ErrorCodes.ReservationConflict] = new(StatusCodes.Status409Conflict, "يتعارض الموعد مع رحلة محجوزة لديك", "The time conflicts with a trip you have reserved"),
        [ErrorCodes.ReservationLimitReached] = new(StatusCodes.Status422UnprocessableEntity, "وصلت للحد الأقصى من الحجوزات", "You have reached the maximum number of reservations"),
        [ErrorCodes.ReservationNotConfirmable] = new(StatusCodes.Status409Conflict, "لا يمكن التأكيد الآن", "The reservation cannot be confirmed right now"),
        [ErrorCodes.AirportPickupZoneRequired] = new(StatusCodes.Status422UnprocessableEntity, "اختر منطقة الالتقاط في المطار", "Choose a pickup zone at the airport"),
        [ErrorCodes.AirportCategoryNotApplicable] = new(StatusCodes.Status422UnprocessableEntity, "فئة المطار متاحة لرحلات المطار فقط", "The airport category is only available for airport trips"),
        [ErrorCodes.NotInAirportWaitingArea] = new(StatusCodes.Status422UnprocessableEntity, "يجب أن تكون داخل منطقة انتظار المطار", "You must be inside the airport waiting area"),
        [ErrorCodes.TicketClosed] = new(StatusCodes.Status409Conflict, "التذكرة مغلقة، أنشئ تذكرة جديدة", "The ticket is closed, please open a new one"),
        [ErrorCodes.DisputeExists] = new(StatusCodes.Status409Conflict, "يوجد اعتراض سابق على هذه الرحلة", "A dispute already exists for this trip"),
        [ErrorCodes.DisputeWindowClosed] = new(StatusCodes.Status422UnprocessableEntity, "انتهت مدة الاعتراض على الأجرة", "The fare dispute window has closed"),
        [ErrorCodes.AttachmentLimit] = new(StatusCodes.Status422UnprocessableEntity, "الحد الأقصى 5 مرفقات", "You can attach at most 5 files"),
        [ErrorCodes.InternalError] = new(StatusCodes.Status500InternalServerError, "حدث خطأ غير متوقع", "An unexpected error occurred"),
    };

    public static int StatusOf(string code) => Entries.TryGetValue(code, out var e) ? e.Status : StatusCodes.Status400BadRequest;

    public static string MessageOf(string code, Language language)
    {
        if (!Entries.TryGetValue(code, out var e))
        {
            return code;
        }

        return language == Language.En ? e.En : e.Ar;
    }

    public static ErrorEnvelope Envelope(string code, Language language, object? details = null) =>
        new(new ErrorBody(code, MessageOf(code, language), details));
}
