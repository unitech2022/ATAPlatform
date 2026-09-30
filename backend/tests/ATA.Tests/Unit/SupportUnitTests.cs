using ATA.Api.Modules.Support;
using ATA.Domain.Support;

namespace ATA.Tests.Unit;

public class SupportUnitTests
{
    private static readonly DateTime T0 = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    private static SupportTicket NewTicket(SupportPriority priority = SupportPriority.Normal)
    {
        var ticket = new SupportTicket { TicketNumber = "ST-20260928-00001", Subject = "x", CreatedAt = T0, LastMessageAt = T0, Priority = priority };
        ticket.ApplySla(SupportTicketWriter.DefaultPolicy(priority));
        return ticket;
    }

    [Theory]
    [InlineData(SupportTicketType.Safety, SupportPriority.Urgent)]
    [InlineData(SupportTicketType.PaymentIssue, SupportPriority.High)]
    [InlineData(SupportTicketType.TripIssue, SupportPriority.Normal)]
    [InlineData(SupportTicketType.LostItem, SupportPriority.Normal)]
    [InlineData(SupportTicketType.Account, SupportPriority.Normal)]
    [InlineData(SupportTicketType.Other, SupportPriority.Normal)]
    public void Default_priority_follows_the_ticket_type(SupportTicketType type, SupportPriority expected) =>
        Assert.Equal(expected, SupportTicket.DefaultPriority(type));

    [Fact]
    public void Only_trip_payment_and_lost_item_tickets_require_a_trip()
    {
        Assert.All(new[] { SupportTicketType.TripIssue, SupportTicketType.PaymentIssue, SupportTicketType.LostItem }, type => Assert.True(SupportTicket.RequiresTrip(type)));
        Assert.All(new[] { SupportTicketType.Safety, SupportTicketType.Account, SupportTicketType.Other }, type => Assert.False(SupportTicket.RequiresTrip(type)));
    }

    [Theory]
    [InlineData(SupportPriority.Urgent, 15, 240)]
    [InlineData(SupportPriority.High, 60, 1440)]
    [InlineData(SupportPriority.Normal, 240, 2880)]
    [InlineData(SupportPriority.Low, 1440, 4320)]
    public void The_sla_due_dates_are_created_at_plus_the_policy_minutes(SupportPriority priority, int first, int resolution)
    {
        var ticket = NewTicket(priority);
        Assert.Equal(T0.AddMinutes(first), ticket.FirstResponseDueAt);
        Assert.Equal(T0.AddMinutes(resolution), ticket.ResolutionDueAt);
    }

    [Fact]
    public void Pausing_and_resuming_moves_the_resolution_deadline_by_the_paused_time()
    {
        var ticket = NewTicket();
        ticket.PauseSla(T0.AddMinutes(10));
        ticket.PauseSla(T0.AddMinutes(20)); // pausing again keeps the first instant
        Assert.Equal(T0.AddMinutes(10), ticket.SlaPausedAt);
        ticket.ResumeSla(T0.AddMinutes(40));
        Assert.Null(ticket.SlaPausedAt);
        Assert.Equal(1800, ticket.SlaPausedSeconds);
        Assert.Equal(T0.AddMinutes(2880 + 30), ticket.ResolutionDueAt);
        ticket.ResumeSla(T0.AddMinutes(90)); // resuming a running clock changes nothing
        Assert.Equal(1800, ticket.SlaPausedSeconds);

        // A later policy change keeps the paused time.
        ticket.ApplySla(SupportTicketWriter.DefaultPolicy(SupportPriority.Urgent));
        Assert.Equal(T0.AddMinutes(240 + 30), ticket.ResolutionDueAt);
        Assert.Equal(T0.AddMinutes(15), ticket.FirstResponseDueAt);
    }

    [Fact]
    public void The_effective_deadline_moves_with_the_clock_while_the_ticket_is_paused()
    {
        var ticket = NewTicket(SupportPriority.Urgent);
        ticket.FirstResponseAt = T0.AddMinutes(1);
        ticket.PauseSla(T0.AddMinutes(220));
        Assert.Equal(T0.AddMinutes(240), ticket.EffectiveResolutionDueAt(T0.AddMinutes(220)));
        Assert.Equal(T0.AddMinutes(420), ticket.EffectiveResolutionDueAt(T0.AddMinutes(400)));
        // 20 minutes from the deadline when it was paused, and still 20 minutes away however long the wait lasts.
        Assert.Equal(SlaState.DueSoon, ticket.SlaStateAt(T0.AddMinutes(230)));
        Assert.Equal(SlaState.DueSoon, ticket.SlaStateAt(T0.AddMinutes(5000)));
    }

    [Fact]
    public void Sla_state_is_ok_due_soon_or_breached_and_ignores_resolved_and_closed_tickets()
    {
        var ticket = NewTicket();
        Assert.Equal(SlaState.Ok, ticket.SlaStateAt(T0.AddMinutes(10)));
        // First response due at +240: due soon within 30 minutes of it, breached after it.
        Assert.Equal(SlaState.Ok, ticket.SlaStateAt(T0.AddMinutes(209)));
        Assert.Equal(SlaState.DueSoon, ticket.SlaStateAt(T0.AddMinutes(210)));
        Assert.Equal(SlaState.DueSoon, ticket.SlaStateAt(T0.AddMinutes(240)));
        Assert.Equal(SlaState.Breached, ticket.SlaStateAt(T0.AddMinutes(241)));
        Assert.True(ticket.IsFirstResponseBreached(T0.AddMinutes(241)));
        Assert.False(ticket.IsResolutionBreached(T0.AddMinutes(241)));

        ticket.FirstResponseAt = T0.AddMinutes(30);
        Assert.Equal(SlaState.Ok, ticket.SlaStateAt(T0.AddMinutes(241)));
        Assert.Equal(SlaState.DueSoon, ticket.SlaStateAt(T0.AddMinutes(2850)));
        Assert.Equal(SlaState.Breached, ticket.SlaStateAt(T0.AddMinutes(2881)));
        Assert.True(ticket.IsResolutionBreached(T0.AddMinutes(2881)));

        ticket.Status = SupportTicketStatus.Resolved;
        Assert.Equal(SlaState.Ok, ticket.SlaStateAt(T0.AddMinutes(9999)));
        ticket.Status = SupportTicketStatus.Closed;
        Assert.Equal(SlaState.Ok, ticket.SlaStateAt(T0.AddMinutes(9999)));
    }

    [Fact]
    public void A_closed_ticket_rejects_further_activity_with_ticket_closed()
    {
        var ticket = NewTicket();
        ticket.EnsureNotClosed();
        Assert.True(ticket.IsActive);
        ticket.Status = SupportTicketStatus.Closed;
        Assert.False(ticket.IsActive);
        var error = Assert.Throws<ATA.Domain.Common.DomainException>(ticket.EnsureNotClosed);
        Assert.Equal("ticket_closed", error.Code);
    }

    [Fact]
    public void Medians_take_the_middle_value_or_the_mean_of_the_two_middle_ones()
    {
        Assert.Null(SupportMath.Median([]));
        Assert.Equal(7d, SupportMath.Median([7d]));
        Assert.Equal(3d, SupportMath.Median([9d, 1d, 3d]));
        Assert.Equal(2.5d, SupportMath.Median([4d, 1d, 3d, 2d]));
    }

    [Fact]
    public void Previews_are_single_line_and_truncated_with_an_ellipsis()
    {
        Assert.Equal("مرحباً بك في ATA", SupportLabels.Preview("  مرحباً\n  بك   في ATA "));
        var preview = SupportLabels.Preview(new string('a', 300), 50);
        Assert.Equal(51, preview.Length);
        Assert.EndsWith("…", preview);
    }

    [Fact]
    public void Help_votes_are_limited_to_one_per_ip_and_article_each_day()
    {
        var throttle = new HelpFeedbackThrottle();
        var article = Guid.NewGuid();
        var day = new DateOnly(2026, 9, 28);
        Assert.True(throttle.TryVote("1.1.1.1", article, day));
        Assert.False(throttle.TryVote("1.1.1.1", article, day));
        Assert.True(throttle.TryVote("2.2.2.2", article, day));
        Assert.True(throttle.TryVote("1.1.1.1", Guid.NewGuid(), day));
        Assert.True(throttle.TryVote("1.1.1.1", article, day.AddDays(1)));
        Assert.False(throttle.TryVote("1.1.1.1", article, day.AddDays(1)));
    }

    [Fact]
    public void The_sla_broadcast_state_reports_only_changes_and_forgets_tickets()
    {
        var state = new SupportSlaBroadcastState();
        var id = Guid.NewGuid();
        Assert.False(state.Changed(id, SlaState.Ok));
        Assert.True(state.Changed(id, SlaState.DueSoon));
        Assert.False(state.Changed(id, SlaState.DueSoon));
        Assert.True(state.Changed(id, SlaState.Breached));
        Assert.Contains(id, state.Known);
        state.Forget([id]);
        Assert.DoesNotContain(id, state.Known);
        Assert.False(state.Changed(id, SlaState.Ok));
    }
}
