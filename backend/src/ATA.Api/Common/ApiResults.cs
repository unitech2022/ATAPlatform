using ATA.Domain.Common;

namespace ATA.Api.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

/// <summary>Parses and clamps <c>?page=&amp;pageSize=</c>.</summary>
public readonly record struct Paging(int Page, int PageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;

    public static Paging From(int? page, int? pageSize) =>
        new(Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));

    public PagedResult<T> Result<T>(IReadOnlyList<T> items, int total) => new(items, Page, PageSize, total);
}

/// <summary>Collects field-level validation errors and raises a single <c>validation_failed</c> error.</summary>
public sealed class Validator
{
    private readonly Dictionary<string, string> _errors = new(StringComparer.Ordinal);

    /// <summary>Field names are reported in camelCase to match the JSON payloads.</summary>
    private static string Key(string field) => field.Length == 0 ? field : char.ToLowerInvariant(field[0]) + field[1..];

    public Validator Require(string field, string? value, int maxLength = 255)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _errors[Key(field)] = "required";
        }
        else if (value.Length > maxLength)
        {
            _errors[Key(field)] = $"max_length:{maxLength}";
        }

        return this;
    }

    public Validator Require<T>(string field, T? value) where T : struct
    {
        if (value is null)
        {
            _errors[Key(field)] = "required";
        }

        return this;
    }

    public Validator Rule(string field, bool valid, string message)
    {
        if (!valid)
        {
            _errors[Key(field)] = message;
        }

        return this;
    }

    public Validator Fail(string field, string message)
    {
        _errors[Key(field)] = message;
        return this;
    }

    public void ThrowIfInvalid()
    {
        if (_errors.Count > 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string>(_errors));
        }
    }
}

public static class Guard
{
    public static T NotFound<T>(T? entity) where T : class => entity ?? throw new DomainException(ErrorCodes.NotFound);
}
