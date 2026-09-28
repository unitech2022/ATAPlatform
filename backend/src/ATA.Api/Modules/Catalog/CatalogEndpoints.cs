using ATA.Api.Common;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Catalog;

public sealed record EstimateDto(int EtaMinutes, decimal Price);

public sealed record RideCategoryDto(Guid Id, string Code, string Name, string? Description, string? Icon, byte Seats, byte MaxStops, int SortOrder, EstimateDto? Estimate);

public sealed record DocumentTypeDto(Guid Id, string Code, string Name, DocumentAppliesTo AppliesTo, bool IsRequired, bool RequiresExpiry);

public sealed record CityDto(Guid Id, string Code, string Name);

public static class CatalogEndpoints
{
    /// <summary>Static sample estimates for step 1; real pricing/ETA arrive with trips in step 2.</summary>
    private static readonly Dictionary<string, EstimateDto> SampleEstimates = new(StringComparer.Ordinal)
    {
        ["saver"] = new(4, 28m),
        ["economy"] = new(2, 38m),
        ["comfort"] = new(3, 52m),
        ["family"] = new(5, 68m),
        ["premium"] = new(6, 95m),
        ["airport"] = new(8, 120m),
    };

    public static void Map(IEndpointRouteBuilder api)
    {
        var catalog = api.MapGroup("/catalog").WithTags("Catalog");

        catalog.MapGet("/ride-categories", async (AtaDbContext db, HttpContext http, CancellationToken ct) =>
            {
                var lang = http.GetLanguage();
                var items = await db.RideCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(ct);
                return Results.Ok(items.Select(c => ToDto(c, lang)).ToList());
            })
            .Produces<List<RideCategoryDto>>();

        catalog.MapGet("/document-types", async (AtaDbContext db, HttpContext http, CancellationToken ct) =>
            {
                var lang = http.GetLanguage();
                var items = await db.DocumentTypes.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.SortOrder).ToListAsync(ct);
                return Results.Ok(items.Select(d => ToDto(d, lang)).ToList());
            })
            .Produces<List<DocumentTypeDto>>();

        catalog.MapGet("/cities", async (AtaDbContext db, HttpContext http, CancellationToken ct) =>
            {
                var lang = http.GetLanguage();
                var items = await db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.NameEn).ToListAsync(ct);
                return Results.Ok(items.Select(c => new CityDto(c.Id, c.Code, lang.Pick(c.NameAr, c.NameEn))).ToList());
            })
            .Produces<List<CityDto>>();
    }

    public static RideCategoryDto ToDto(RideCategory c, Language lang) => new(
        c.Id, c.Code, lang.Pick(c.NameAr, c.NameEn), lang.PickOptional(c.DescriptionAr, c.DescriptionEn), c.Icon, c.Seats, c.MaxStops, c.SortOrder,
        SampleEstimates.GetValueOrDefault(c.Code));

    public static DocumentTypeDto ToDto(DocumentType d, Language lang) =>
        new(d.Id, d.Code, lang.Pick(d.NameAr, d.NameEn), d.AppliesTo, d.IsRequired, d.RequiresExpiry);
}
