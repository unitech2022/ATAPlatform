using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ATA.Infrastructure.Persistence;

internal static class NamingConventions
{
    /// <summary>Converts <c>PascalCase</c> to <c>snake_case</c> (e.g. <c>NameAr</c> → <c>name_ar</c>, <c>Sha256</c> → <c>sha256</c>).</summary>
    public static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsUpper(name[i - 1]))))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>Applies snake_case column names and string-backed snake_case enums to every entity in the model.</summary>
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));

                var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (clrType.IsEnum)
                {
                    var converterType = typeof(SnakeCaseEnumConverter<>).MakeGenericType(clrType);
                    property.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
                    property.SetMaxLength(32);
                }
                else if (clrType == typeof(DateTime))
                {
                    property.SetValueConverter(property.ClrType == typeof(DateTime) ? UtcDateTimeConverter.Instance : UtcDateTimeConverter.NullableInstance);
                }
            }

            foreach (var key in entity.GetKeys())
            {
                if (key.IsPrimaryKey())
                {
                    // Ids are UUID v7 values generated in the application, never by the database.
                    foreach (var property in key.Properties.Where(p => p.ClrType == typeof(Guid)))
                    {
                        property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
                    }
                }

                key.SetName(key.IsPrimaryKey() ? $"pk_{entity.GetTableName()}" : $"ak_{entity.GetTableName()}_{string.Join('_', key.Properties.Select(p => p.GetColumnName()))}");
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName($"fk_{entity.GetTableName()}_{string.Join('_', fk.Properties.Select(p => p.GetColumnName()))}");
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName($"{(index.IsUnique ? "ux" : "ix")}_{entity.GetTableName()}_{string.Join('_', index.Properties.Select(p => p.GetColumnName()))}");
            }
        }
    }
}

/// <summary>Stores enum members as snake_case strings (<c>UnderReview</c> ↔ <c>under_review</c>).</summary>
internal sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(e => ToDb(e), s => FromDb(s))
    where TEnum : struct, Enum
{
    private static string ToDb(TEnum value) => NamingConventions.ToSnakeCase(value.ToString());

    private static TEnum FromDb(string value) => Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);
}

/// <summary>Round-trips DateTime values as UTC regardless of the provider (SQLite loses the Kind).</summary>
internal static class UtcDateTimeConverter
{
    public static readonly ValueConverter<DateTime, DateTime> Instance =
        new(v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public static readonly ValueConverter<DateTime?, DateTime?> NullableInstance =
        new(v => v == null ? null : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
            v => v == null ? null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));
}
