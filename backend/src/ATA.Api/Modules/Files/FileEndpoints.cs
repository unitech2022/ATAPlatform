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
                if (file.OwnerUserId != user.UserId && !user.IsAdmin && !await IsTicketAttachmentOfRequesterAsync(db, id, user.UserId, ct))
                {
                    throw new DomainException(ErrorCodes.Forbidden);
                }

                var stream = Guard.NotFound(await storage.OpenReadAsync(file.StorageKey, ct));
                return Results.Stream(stream, file.ContentType, enableRangeProcessing: true, fileDownloadName: null)
                    .WithInlineDisposition(file.OriginalName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");
    }

    /// <summary>F18: the requester of a support ticket may read the files agents attached to a public message of their ticket (uploaded by the agent, so not "owned" by the requester).</summary>
    private static Task<bool> IsTicketAttachmentOfRequesterAsync(AtaDbContext db, Guid fileId, Guid userId, CancellationToken ct) =>
        (from a in db.SupportMessageAttachments.AsNoTracking()
         join m in db.SupportMessages.AsNoTracking() on a.MessageId equals m.Id
         join t in db.SupportTickets.AsNoTracking() on m.TicketId equals t.Id
         where a.FileId == fileId && !m.IsInternal && t.RequesterUserId == userId
         select a.Id).AnyAsync(ct);

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
