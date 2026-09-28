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
