using ATA.Domain.Files;
using ATA.Domain.Identity;
using ATA.Domain.Support;
using ATA.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATA.Infrastructure.Persistence.Configurations;

// ----- F18 help center (doc 11 §F18.1) -----

public sealed class HelpCategoryConfiguration : IEntityTypeConfiguration<HelpCategory>
{
    public void Configure(EntityTypeBuilder<HelpCategory> b)
    {
        b.ToTable("help_categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameAr).HasMaxLength(120).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(120).IsRequired();
        b.Property(x => x.Icon).HasMaxLength(40).IsRequired();
        b.HasIndex(x => new { x.IsActive, x.SortOrder });
    }
}

public sealed class HelpArticleConfiguration : IEntityTypeConfiguration<HelpArticle>
{
    public void Configure(EntityTypeBuilder<HelpArticle> b)
    {
        b.ToTable("help_articles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.TitleAr).HasMaxLength(200).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(200).IsRequired();
        b.Property(x => x.BodyAr).HasColumnType("mediumtext").IsRequired();
        b.Property(x => x.BodyEn).HasColumnType("mediumtext").IsRequired();
        b.Property(x => x.Tags).HasColumnType("json");
        b.HasIndex(x => new { x.CategoryId, x.IsPublished, x.SortOrder });
        b.HasIndex(x => new { x.IsPublished, x.ViewCount });
        // MySQL only: the public search uses MATCH … AGAINST; the SQLite test database ignores the annotation and the search falls back to LIKE.
        b.HasIndex(x => new { x.TitleAr, x.TitleEn, x.BodyAr, x.BodyEn }).HasAnnotation("MySql:FullTextIndex", true);
        b.HasOne<HelpCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

// ----- F18 tickets (doc 11 §F18.1) -----

public sealed class SupportSlaPolicyConfiguration : IEntityTypeConfiguration<SupportSlaPolicy>
{
    public void Configure(EntityTypeBuilder<SupportSlaPolicy> b)
    {
        b.ToTable("support_sla_policies");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Priority).IsUnique();
    }
}

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> b)
    {
        b.ToTable("support_tickets");
        b.HasKey(x => x.Id);
        b.Property(x => x.TicketNumber).HasMaxLength(24).IsRequired();
        b.HasIndex(x => x.TicketNumber).IsUnique();
        b.Property(x => x.Subject).HasMaxLength(160).IsRequired();
        b.Property(x => x.CsatComment).HasMaxLength(500);
        b.HasIndex(x => new { x.Status, x.Priority, x.ResolutionDueAt });
        b.HasIndex(x => new { x.RequesterUserId, x.CreatedAt });
        b.HasIndex(x => new { x.AssignedToUserId, x.Status });
        b.HasIndex(x => x.TripId);
        b.Ignore(x => x.IsActive);
        b.Ignore(x => x.IsClosed);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> b)
    {
        b.ToTable("support_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.HasIndex(x => new { x.TicketId, x.CreatedAt });
        b.HasOne<SupportTicket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class SupportMessageAttachmentConfiguration : IEntityTypeConfiguration<SupportMessageAttachment>
{
    public void Configure(EntityTypeBuilder<SupportMessageAttachment> b)
    {
        b.ToTable("support_message_attachments");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.MessageId);
        b.HasIndex(x => x.FileId);
        b.HasOne<SupportMessage>().WithMany().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CannedResponseConfiguration : IEntityTypeConfiguration<CannedResponse>
{
    public void Configure(EntityTypeBuilder<CannedResponse> b)
    {
        b.ToTable("canned_responses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Title).HasMaxLength(120).IsRequired();
        b.Property(x => x.BodyAr).HasMaxLength(4000).IsRequired();
        b.Property(x => x.BodyEn).HasMaxLength(4000).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FareDisputeConfiguration : IEntityTypeConfiguration<FareDispute>
{
    public void Configure(EntityTypeBuilder<FareDispute> b)
    {
        b.ToTable("fare_disputes");
        b.HasKey(x => x.Id);
        b.Property(x => x.ResolutionNote).HasMaxLength(1000);
        b.HasIndex(x => x.TicketId).IsUnique();
        // One dispute per trip.
        b.HasIndex(x => x.TripId).IsUnique();
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.Ignore(x => x.IsResolved);
        b.HasOne<SupportTicket>().WithMany().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ResolvedBy).OnDelete(DeleteBehavior.SetNull);
    }
}
