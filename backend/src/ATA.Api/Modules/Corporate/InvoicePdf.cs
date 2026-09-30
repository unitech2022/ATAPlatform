using System.Globalization;
using System.Reflection;
using System.Text;
using ATA.Domain.Corporate;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ATA.Api.Modules.Corporate;

/// <summary>Everything the invoice PDF prints (a snapshot: the renderer never reads the database).</summary>
public sealed record InvoicePdfModel(
    string InvoiceNumber, bool IsDraft, DateOnly IssueDate, DateOnly DueDate, DateOnly PeriodStart, DateOnly PeriodEnd, string Currency, decimal VatRate, decimal SubtotalExclVat,
    decimal VatAmount, decimal TotalInclVat, InvoiceParty Seller, InvoiceParty Buyer, string SellerAddress, string BuyerAddress, DateTime Timestamp, IReadOnlyList<InvoiceLineDto> Lines);

/// <summary>Turns an <see cref="InvoicePdfModel"/> into PDF bytes (swap the implementation to change the PDF library, see README "Corporate invoices").</summary>
public interface IInvoicePdfRenderer
{
    byte[] Render(InvoicePdfModel model);
}

/// <summary>
/// ZATCA-style QR payload: TLV (tag, length, UTF-8 value) of seller name (1), VAT number (2), ISO-8601 timestamp (3), total with VAT (4) and VAT amount (5), base64-encoded.
/// </summary>
public static class ZatcaQr
{
    public static string Payload(string sellerName, string vatNumber, DateTime timestampUtc, decimal totalInclVat, decimal vatAmount)
    {
        using var stream = new MemoryStream();
        Write(stream, 1, sellerName);
        Write(stream, 2, vatNumber);
        Write(stream, 3, timestampUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        Write(stream, 4, totalInclVat.ToString("0.00", CultureInfo.InvariantCulture));
        Write(stream, 5, vatAmount.ToString("0.00", CultureInfo.InvariantCulture));
        return Convert.ToBase64String(stream.ToArray());
    }

    /// <summary>Decodes a payload back to <c>tag → value</c> (tests and verification tools).</summary>
    public static IReadOnlyDictionary<int, string> Decode(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var result = new Dictionary<int, string>();
        for (var i = 0; i < bytes.Length;)
        {
            var tag = bytes[i];
            var length = bytes[i + 1];
            result[tag] = Encoding.UTF8.GetString(bytes, i + 2, length);
            i += 2 + length;
        }

        return result;
    }

    private static void Write(Stream stream, byte tag, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > 255)
        {
            bytes = bytes[..255];
        }

        stream.WriteByte(tag);
        stream.WriteByte((byte)bytes.Length);
        stream.Write(bytes);
    }

    /// <summary>The QR code as an SVG (black modules on white, 4-module quiet zone) built from the QRCoder module matrix.</summary>
    public static string Svg(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var matrix = data.ModuleMatrix;
        const int quiet = 4;
        var size = matrix.Count + quiet * 2;
        var path = new StringBuilder();
        for (var y = 0; y < matrix.Count; y++)
        {
            for (var x = 0; x < matrix.Count; x++)
            {
                if (matrix[y][x])
                {
                    path.Append(CultureInfo.InvariantCulture, $"M{x + quiet},{y + quiet}h1v1h-1z");
                }
            }
        }

        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {size} {size}\" shape-rendering=\"crispEdges\"><rect width=\"{size}\" height=\"{size}\" fill=\"#fff\"/><path d=\"{path}\" fill=\"#000\"/></svg>";
    }
}

/// <summary>
/// Bilingual (Arabic / English) A4 tax invoice rendered with QuestPDF (Community license — see README), Arabic shaped right-to-left with IBM Plex Sans Arabic
/// (SIL OFL) embedded in the assembly so no system font is needed. Contains seller and buyer (name, VAT number, national address), invoice number, dates, period, the
/// line table, the totals with VAT and the TLV QR code.
/// </summary>
public sealed class QuestPdfInvoiceRenderer : IInvoicePdfRenderer
{
    private const string Family = "IBM Plex Sans Arabic";
    private static readonly object Gate = new();
    private static bool _initialized;

    private static void Initialize()
    {
        lock (Gate)
        {
            if (_initialized)
            {
                return;
            }

            QuestPDF.Settings.License = LicenseType.Community;
            var assembly = typeof(QuestPdfInvoiceRenderer).Assembly;
            foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("ATA.Fonts.", StringComparison.Ordinal)))
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                QuestPDF.Drawing.FontManager.RegisterFontFromStream(stream);
            }

            _initialized = true;
        }
    }

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

    public byte[] Render(InvoicePdfModel m)
    {
        Initialize();
        var qr = ZatcaQr.Svg(ZatcaQr.Payload(m.Seller.NameAr, m.Seller.VatNumber ?? string.Empty, m.Timestamp, m.TotalInclVat, m.VatAmount));
        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(t => t.FontFamily(Family).FontSize(9).FontColor("#123650"));

            page.Header().Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(m.IsDraft ? "مسودة فاتورة ضريبية · Draft Tax Invoice" : "فاتورة ضريبية · Tax Invoice").FontSize(18).Bold();
                        c.Item().PaddingTop(2).Text($"{m.InvoiceNumber}").FontSize(12).SemiBold();
                    });
                    row.ConstantItem(150).Column(c =>
                    {
                        c.Item().Text($"تاريخ الإصدار · Issue date: {m.IssueDate:yyyy-MM-dd}");
                        c.Item().Text($"تاريخ الاستحقاق · Due date: {m.DueDate:yyyy-MM-dd}");
                        c.Item().Text($"الفترة · Period: {m.PeriodStart:yyyy-MM-dd} → {m.PeriodEnd:yyyy-MM-dd}");
                    });
                });
                column.Item().PaddingTop(8).LineHorizontal(1).LineColor("#19B7A5");
            });

            page.Content().PaddingVertical(8).Column(column =>
            {
                column.Spacing(10);
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(e => Party(e, "البائع · Seller", m.Seller, m.SellerAddress, showCr: false));
                    row.ConstantItem(12);
                    row.RelativeItem().Element(e => Party(e, "المشتري · Buyer", m.Buyer, m.BuyerAddress, showCr: true));
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(20);
                        c.ConstantColumn(58);
                        c.ConstantColumn(72);
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.ConstantColumn(50);
                        c.ConstantColumn(42);
                        c.ConstantColumn(54);
                    });
                    table.Header(h =>
                    {
                        foreach (var title in new[] { "#", "التاريخ · Date", "المرجع · Ref", "الراكب · Rider", "البيان · Description", "قبل الضريبة · Excl.", "الضريبة · VAT", "الإجمالي · Total" })
                        {
                            h.Cell().Background("#123650").Padding(3).Text(title).FontColor(Colors.White).FontSize(7.5f).SemiBold();
                        }
                    });
                    var index = 0;
                    foreach (var line in m.Lines)
                    {
                        index++;
                        var background = index % 2 == 0 ? "#F4F7F9" : "#FFFFFF";
                        var rider = line.GuestName is { Length: > 0 } guest ? $"{guest} · guest" : line.EmployeeName ?? string.Empty;
                        var description = line.LineType == InvoiceLineType.Trip
                            ? $"{line.PickupName} → {line.DropoffName}" + (string.IsNullOrWhiteSpace(line.Purpose) ? string.Empty : $" ({line.Purpose})")
                            : line.Description;
                        table.Cell().Background(background).Padding(3).Text(index.ToString(CultureInfo.InvariantCulture)).FontSize(7.5f);
                        table.Cell().Background(background).Padding(3).Text(line.TripDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty).FontSize(7.5f);
                        table.Cell().Background(background).Padding(3).Text(line.TripNumber ?? line.LineType.ToString()).FontSize(7.5f);
                        table.Cell().Background(background).Padding(3).Text(rider).FontSize(7.5f);
                        table.Cell().Background(background).Padding(3).Text(description).FontSize(7.5f);
                        table.Cell().Background(background).Padding(3).AlignRight().Text(Money(line.AmountExclVat)).FontSize(7.5f);
                        table.Cell().Background(background).Padding(3).AlignRight().Text(Money(line.VatAmount)).FontSize(7.5f);
                        table.Cell().Background(background).Padding(3).AlignRight().Text(Money(line.AmountInclVat)).FontSize(7.5f);
                    }
                });

                column.Item().Row(row =>
                {
                    row.ConstantItem(96).Height(96).Svg(qr);
                    row.RelativeItem();
                    row.ConstantItem(220).Border(1).BorderColor("#D5DEE5").Padding(8).Column(c =>
                    {
                        Total(c, "المجموع قبل الضريبة · Subtotal (excl. VAT)", $"{m.Currency} {Money(m.SubtotalExclVat)}");
                        Total(c, $"ضريبة القيمة المضافة {m.VatRate:0.##}% · VAT", $"{m.Currency} {Money(m.VatAmount)}");
                        c.Item().PaddingVertical(3).LineHorizontal(0.5f).LineColor("#D5DEE5");
                        Total(c, "الإجمالي شامل الضريبة · Total (incl. VAT)", $"{m.Currency} {Money(m.TotalInclVat)}", bold: true);
                    });
                });
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text("فاتورة مبسطة بانتظار الربط مع منصة فاتورة · Simplified invoice pending e-invoicing integration").FontSize(7).FontColor("#6B7C8A");
                row.ConstantItem(80).AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7).FontColor("#6B7C8A"));
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    private static void Party(IContainer container, string title, InvoiceParty party, string address, bool showCr) =>
        container.Border(1).BorderColor("#D5DEE5").Padding(8).Column(c =>
        {
            c.Item().Text(title).FontSize(8).FontColor("#6B7C8A").SemiBold();
            c.Item().PaddingTop(2).Text(party.NameAr).Bold();
            c.Item().Text(party.NameEn);
            c.Item().PaddingTop(3).Text($"الرقم الضريبي · VAT No: {party.VatNumber ?? "—"}");
            if (showCr && !string.IsNullOrWhiteSpace(party.CrNumber))
            {
                c.Item().Text($"السجل التجاري · CR: {party.CrNumber}");
            }

            c.Item().PaddingTop(3).Text($"العنوان · Address: {(string.IsNullOrWhiteSpace(address) ? "—" : address)}");
        });

    private static void Total(ColumnDescriptor column, string label, string value, bool bold = false) =>
        column.Item().PaddingVertical(1).Row(row =>
        {
            var left = row.RelativeItem().Text(label).FontSize(8.5f);
            var right = row.ConstantItem(84).AlignRight().Text(value).FontSize(9);
            if (bold)
            {
                left.Bold();
                right.Bold();
            }
        });
}
