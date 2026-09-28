using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Files;

public static class FileEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/files").WithTags("Files").RequireAuthorization(Policies.Authenticated);

        group.MapGet("/{id:guid}", async (Guid id, AtaDbContext db, IFileStorage storage, ICurrentUser user, CancellationToken ct) =>
            {
                var file = Guard.NotFound(await db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct));
                if (file.OwnerUserId != user.UserId && !user.IsAdmin)
                {
                    throw new DomainException(ErrorCodes.Forbidden);
                }

                var stream = Guard.NotFound(await storage.OpenReadAsync(file.StorageKey, ct));
                return Results.Stream(stream, file.ContentType, enableRangeProcessing: true, fileDownloadName: null)
                    .WithInlineDisposition(file.OriginalName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
    }

    private static IResult WithInlineDisposition(this IResult inner, string fileName) => new InlineFileResult(inner, fileName);

    private sealed class InlineFileResult(IResult inner, string fileName) : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext)
        {
            var disposition = new System.Net.Mime.ContentDisposition { Inline = true, FileName = fileName };
            httpContext.Response.Headers.ContentDisposition = disposition.ToString();
            return inner.ExecuteAsync(httpContext);
        }
    }
}
