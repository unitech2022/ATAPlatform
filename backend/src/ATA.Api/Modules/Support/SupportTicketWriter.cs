using System.Runtime.CompilerServices;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Files;
using ATA.Domain.Support;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Support;

/// <summary>
/// The building blocks every ticket write shares (doc 11 §F18.2): ticket numbers, SLA policies, messages with their attachments and system lines.
/// Everything is added to the caller's unit of work; the caller saves.
/// </summary>
public sealed class SupportTicketWriter(AtaDbContext db, IClock clock, IOptions<SupportOptions> options)
{
    public const string StoragePrefix = "support/";
    private const int NumberRetries = 3;
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IClock, StrongBox<long>> LastMessageTicks = new();

    /// <summary>The in-code policies used only if <c>support_sla_policies</c> misses a priority (the seeder always adds all four).</summary>
    public static SupportSlaPolicy DefaultPolicy(SupportPriority priority) => priority switch
    {
        SupportPriority.Urgent => new SupportSlaPolicy { Priority = priority, FirstResponseMinutes = 15, ResolutionMinutes = 240 },
        SupportPriority.High => new SupportSlaPolicy { Priority = priority, FirstResponseMinutes = 60, ResolutionMinutes = 1440 },
        SupportPriority.Normal => new SupportSlaPolicy { Priority = priority, FirstResponseMinutes = 240, ResolutionMinutes = 2880 },
        _ => new SupportSlaPolicy { Priority = priority, FirstResponseMinutes = 1440, ResolutionMinutes = 4320 },
    };

    public sealed record Draft(
        Guid RequesterUserId, SupportRequesterRole RequesterRole, SupportTicketType Type, Guid? TripId, string Subject, string Message, SupportPriority? Priority,
        SupportChannel Channel, Guid MessageAuthorUserId, IReadOnlyList<Guid> FileIds);

    public async Task<SupportSlaPolicy> PolicyAsync(SupportPriority priority, CancellationToken ct) =>
        await db.SupportSlaPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Priority == priority, ct) ?? DefaultPolicy(priority);

    public async Task<string> NextNumberAsync(int offset, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return await SequenceNumbers.NextAsync(db.SupportTickets.Select(t => t.TicketNumber), $"ST-{now:yyyyMMdd}-", 5, offset, ct);
    }

    /// <summary>Adds a new <c>open</c> ticket with its SLA due dates and its first (public) message; the ticket number is assigned here and retried by <see cref="SaveNewAsync"/>.</summary>
    public async Task<SupportTicket> AddAsync(Draft draft, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var priority = draft.Priority ?? SupportTicket.DefaultPriority(draft.Type);
        var ticket = new SupportTicket
        {
            TicketNumber = await NextNumberAsync(0, ct),
            RequesterUserId = draft.RequesterUserId,
            RequesterRole = draft.RequesterRole,
            Type = draft.Type,
            TripId = draft.TripId,
            Subject = draft.Subject,
            Priority = priority,
            Channel = draft.Channel,
            CreatedAt = now,
            LastMessageAt = now,
            LastMessageBy = SupportAuthorRole.User,
        };
        ticket.ApplySla(await PolicyAsync(priority, ct));
        db.SupportTickets.Add(ticket);
        AddMessage(ticket.Id, draft.MessageAuthorUserId, SupportAuthorRole.User, draft.Message, isInternal: false, draft.FileIds);
        return ticket;
    }

    /// <summary>Saves a unit of work that contains new tickets, retrying with the next ticket number on a unique-number conflict; <paramref name="renumber"/> re-numbers the other numbered rows (attempt offset).</summary>
    public async Task SaveNewAsync(SupportTicket ticket, Func<int, CancellationToken, Task>? renumber, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException) when (attempt < NumberRetries)
            {
                ticket.TicketNumber = await NextNumberAsync(attempt + 1, ct);
                if (renumber is not null)
                {
                    await renumber(attempt + 1, ct);
                }
            }
        }
    }

    public SupportMessage AddMessage(Guid ticketId, Guid? authorUserId, SupportAuthorRole role, string body, bool isInternal, IEnumerable<Guid>? fileIds = null)
    {
        var message = new SupportMessage
        {
            TicketId = ticketId, AuthorUserId = authorUserId, AuthorRole = role, Body = body.Length > 4000 ? body[..4000] : body, IsInternal = isInternal,
            CreatedAt = MessageTimestamp(),
        };
        db.SupportMessages.Add(message);
        foreach (var fileId in fileIds ?? [])
        {
            db.SupportMessageAttachments.Add(new SupportMessageAttachment { MessageId = message.Id, FileId = fileId, CreatedAt = message.CreatedAt });
        }

        return message;
    }

    /// <summary>
    /// A line written by the platform (lost-item updates, dispute outcomes): moves <c>last_message_at</c> / <c>last_message_by</c> but sends no
    /// <c>support.reply</c>, does not raise the unread counter and never changes the ticket's status.
    /// </summary>
    public SupportMessage AddSystemMessage(SupportTicket ticket, string body)
    {
        var message = AddMessage(ticket.Id, null, SupportAuthorRole.System, body, isInternal: false);
        ticket.LastMessageAt = message.CreatedAt;
        ticket.LastMessageBy = SupportAuthorRole.System;
        return message;
    }

    /// <summary>Checks the attachments of one message: at most <c>Support:MaxAttachmentsPerMessage</c> (<c>422 attachment_limit</c>), every file uploaded by the sender through <c>POST /support/attachments</c> and not attached yet.</summary>
    public async Task<IReadOnlyList<Guid>> ValidateFilesAsync(Guid uploaderUserId, IReadOnlyCollection<Guid>? fileIds, CancellationToken ct)
    {
        var ids = fileIds?.Distinct().ToList() ?? [];
        var max = options.Value.MaxAttachmentsPerMessage;
        if (ids.Count > max)
        {
            throw new DomainException(ErrorCodes.AttachmentLimit, new { max });
        }

        if (ids.Count == 0)
        {
            return ids;
        }

        var owned = await db.StoredFiles.AsNoTracking().CountAsync(f => ids.Contains(f.Id) && f.OwnerUserId == uploaderUserId && f.StorageKey.StartsWith(StoragePrefix), ct);
        new Validator().Rule(nameof(CreateTicketRequest.FileIds), owned == ids.Count, "unknown file").ThrowIfInvalid();
        var used = await db.SupportMessageAttachments.AsNoTracking().AnyAsync(a => ids.Contains(a.FileId), ct);
        new Validator().Rule(nameof(CreateTicketRequest.FileIds), !used, "already attached").ThrowIfInvalid();
        return ids;
    }

    /// <summary>Attachments of the given messages grouped by message.</summary>
    public async Task<Dictionary<Guid, List<TicketAttachmentDto>>> AttachmentsAsync(IReadOnlyCollection<Guid> messageIds, CancellationToken ct)
    {
        var rows = await (from a in db.SupportMessageAttachments.AsNoTracking()
                          join f in db.StoredFiles.AsNoTracking() on a.FileId equals f.Id
                          where messageIds.Contains(a.MessageId)
                          orderby a.CreatedAt
                          select new { a.MessageId, a.FileId, f.OriginalName, f.ContentType }).ToListAsync(ct);
        return rows.GroupBy(r => r.MessageId).ToDictionary(g => g.Key, g => g.Select(r => new TicketAttachmentDto(r.FileId, r.OriginalName, r.ContentType)).ToList());
    }

    /// <summary>
    /// Monotonic message timestamps (per clock) so the conversation keeps its insertion order within one clock tick; a clock that jumped back by
    /// more than a second (tests) starts a new sequence.
    /// </summary>
    private DateTime MessageTimestamp()
    {
        var state = LastMessageTicks.GetValue(clock, _ => new StrongBox<long>(0));
        var now = clock.UtcNow.Ticks;
        lock (state)
        {
            var next = now > state.Value || state.Value - now > TimeSpan.TicksPerSecond ? now : state.Value + TimeSpan.TicksPerMicrosecond;
            state.Value = next;
            return new DateTime(next, DateTimeKind.Utc);
        }
    }
}
