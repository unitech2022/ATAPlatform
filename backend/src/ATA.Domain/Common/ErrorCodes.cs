namespace ATA.Domain.Common;

public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string Unauthorized = "unauthorized";
    public const string InvalidCredentials = "invalid_credentials";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
    public const string RateLimited = "rate_limited";
    public const string OtpInvalid = "otp_invalid";
    public const string OtpExpired = "otp_expired";
    public const string OtpLocked = "otp_locked";
    public const string PhoneInvalid = "phone_invalid";
    public const string AccountSuspended = "account_suspended";
    public const string DriverNotApproved = "driver_not_approved";
    public const string FileTooLarge = "file_too_large";
    public const string UnsupportedFileType = "unsupported_file_type";
    public const string InsufficientBalance = "insufficient_balance";
    public const string InternalError = "internal_error";
}
