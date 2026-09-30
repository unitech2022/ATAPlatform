using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Support;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Support;

/// <summary>One helpfulness vote per IP / article / (Riyadh) day (doc 11 §F18.3); in memory, like the tracking-page view throttle.</summary>
public sealed class HelpFeedbackThrottle
{
    private readonly ConcurrentDictionary<string, byte> _votes = new();
    private DateOnly _day;

    /// <summary>Records the vote; <c>false</c> when this IP already voted on the article today.</summary>
    public bool TryVote(string ip, Guid articleId, DateOnly day)
    {
        lock (_votes)
        {
            if (day != _day)
            {
                _votes.Clear();
                _day = day;
            }
        }

        return _votes.TryAdd($"{ip}|{articleId}", 0);
    }
}

/// <summary>
/// The help center: the public read API (published articles only, in the caller's language, filtered by audience) and the <c>help.manage</c> admin console.
/// Search uses MySQL <c>FULLTEXT</c> (boolean mode, prefix terms) and falls back to <c>LIKE</c> when the provider has none (SQLite tests) or FULLTEXT finds nothing
/// (words shorter than the index's minimum token size).
/// </summary>
public sealed partial class HelpService(AtaDbContext db, IClock clock, ICurrentUser currentUser, HelpFeedbackThrottle throttle, AuditService audit)
{
    public const int ExcerptLength = 160;
    private const int MaxSearchCandidates = 500;
    private const int RelatedCount = 4;

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[a-z0-9_-]{1,40}$")]
    private static partial Regex CodePattern();

    [GeneratedRegex(@"```[\s\S]*?```")]
    private static partial Regex CodeFence();

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]*\)")]
    private static partial Regex MarkdownImage();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"[#>*_`~|]|(?<=^|\s)-(?=\s)")]
    private static partial Regex MarkdownMarks();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    // ----- public -----

    public async Task<IReadOnlyList<HelpCategoryDto>> CategoriesAsync(string? audience, Language language, CancellationToken ct)
    {
        var wanted = ParseAudience(audience);
        var counts = await PublishedQuery(wanted, null).GroupBy(x => x.Category.Id).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var categories = await db.HelpCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Code).ToListAsync(ct);
        // A category without a published article for the audience is not offered.
        return categories.Where(c => counts.ContainsKey(c.Id)).Select(c => new HelpCategoryDto(c.Id, c.Code, language.Pick(c.NameAr, c.NameEn), c.Icon, counts[c.Id])).ToList();
    }

    /// <summary>
    /// Published articles (page + total). Order: relevance with <c>q</c>; without it the most read (page 1 is the "popular" list) — or the category's own order when
    /// <c>categoryId</c> is given. <c>sort</c> = <c>popular</c> | <c>newest</c> | <c>order</c> overrides.
    /// </summary>
    public async Task<PagedResult<HelpArticleSummaryDto>> ArticlesAsync(Guid? categoryId, string? q, string? audience, string? sort, Paging paging, Language language, CancellationToken ct)
    {
        var wanted = ParseAudience(audience);
        new Validator().Rule("sort", string.IsNullOrEmpty(sort) || sort is "popular" or "newest" or "order" or "relevance", "must be popular|newest|order|relevance").ThrowIfInvalid();
        var query = PublishedQuery(wanted, categoryId);
        List<HelpArticle> rows;
        int total;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var candidates = await SearchAsync(query.Select(x => x.Article), q.Trim(), ct);
            candidates = sort switch
            {
                "popular" => candidates.OrderByDescending(a => a.ViewCount).ThenBy(a => a.SortOrder).ToList(),
                "newest" => candidates.OrderByDescending(a => a.PublishedAt).ToList(),
                "order" => candidates.OrderBy(a => a.SortOrder).ThenBy(a => language.Pick(a.TitleAr, a.TitleEn), StringComparer.Ordinal).ToList(),
                _ => candidates,
            };
            total = candidates.Count;
            rows = candidates.Skip(paging.Skip).Take(paging.PageSize).ToList();
        }
        else
        {
            var articles = query.Select(x => x.Article);
            var effective = string.IsNullOrEmpty(sort) || sort == "relevance" ? (categoryId is null ? "popular" : "order") : sort;
            var ordered = effective switch
            {
                "newest" => articles.OrderByDescending(a => a.PublishedAt).ThenBy(a => a.Slug),
                "order" => articles.OrderBy(a => a.SortOrder).ThenBy(a => a.Slug),
                _ => articles.OrderByDescending(a => a.ViewCount).ThenBy(a => a.SortOrder).ThenBy(a => a.Slug),
            };
            total = await articles.CountAsync(ct);
            rows = await ordered.Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        }

        return paging.Result(rows.Select(a => new HelpArticleSummaryDto(a.Id, a.Slug, Localized(a.TitleAr, a.TitleEn, language), Excerpt(Localized(a.BodyAr, a.BodyEn, language)), a.CategoryId, a.UpdatedAt)).ToList(), total);
    }

    /// <summary>The article by slug (published only) with its category and related articles; every read increases <c>view_count</c>.</summary>
    public async Task<HelpArticleDto> ArticleAsync(string slug, Language language, CancellationToken ct)
    {
        var row = await (from a in db.HelpArticles.AsNoTracking()
                         join c in db.HelpCategories.AsNoTracking() on a.CategoryId equals c.Id
                         where a.Slug == slug && a.IsPublished && c.IsActive
                         select new { Article = a, Category = c }).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.NotFound);
        await db.HelpArticles.Where(a => a.Id == row.Article.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), ct);
        var related = await (from a in db.HelpArticles.AsNoTracking()
                             where a.CategoryId == row.Article.CategoryId && a.IsPublished && a.Id != row.Article.Id
                             orderby a.ViewCount descending, a.SortOrder, a.Slug
                             select a).Take(RelatedCount).ToListAsync(ct);
        var article = row.Article;
        return new HelpArticleDto(article.Id, article.Slug, Localized(article.TitleAr, article.TitleEn, language), Localized(article.BodyAr, article.BodyEn, language),
            new HelpArticleCategoryDto(row.Category.Id, row.Category.Code, language.Pick(row.Category.NameAr, row.Category.NameEn)), ParseTags(article.Tags), article.UpdatedAt,
            related.Select(r => new HelpRelatedDto(r.Slug, Localized(r.TitleAr, r.TitleEn, language))).ToList());
    }

    /// <summary>One vote per IP, article and day: <c>429 rate_limited</c> (with <c>retryAfterSeconds</c> until the next Riyadh midnight) for the second one.</summary>
    public async Task FeedbackAsync(Guid articleId, HelpFeedbackRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Helpful), request.Helpful).ThrowIfInvalid();
        var exists = await (from a in db.HelpArticles.AsNoTracking()
                            join c in db.HelpCategories.AsNoTracking() on a.CategoryId equals c.Id
                            where a.Id == articleId && a.IsPublished && c.IsActive
                            select a.Id).AnyAsync(ct);
        if (!exists)
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        var now = clock.UtcNow;
        var today = Formats.RiyadhDate(now);
        if (!throttle.TryVote(currentUser.IpAddress ?? "unknown", articleId, today))
        {
            var retry = (int)Math.Max(1, (Formats.RiyadhMidnightUtc(today.AddDays(1)) - now).TotalSeconds);
            throw new DomainException(ErrorCodes.RateLimited, new { retryAfterSeconds = retry });
        }

        if (request.Helpful == true)
        {
            await db.HelpArticles.Where(a => a.Id == articleId).ExecuteUpdateAsync(s => s.SetProperty(a => a.HelpfulYes, a => a.HelpfulYes + 1), ct);
        }
        else
        {
            await db.HelpArticles.Where(a => a.Id == articleId).ExecuteUpdateAsync(s => s.SetProperty(a => a.HelpfulNo, a => a.HelpfulNo + 1), ct);
        }
    }

    private IQueryable<PublishedRow> PublishedQuery(HelpAudience? audience, Guid? categoryId)
    {
        var query = from a in db.HelpArticles.AsNoTracking()
                    join c in db.HelpCategories.AsNoTracking() on a.CategoryId equals c.Id
                    where a.IsPublished && c.IsActive
                    select new PublishedRow { Article = a, Category = c };
        if (audience is { } wanted)
        {
            query = query.Where(x => (x.Article.Audience == HelpAudience.All || x.Article.Audience == wanted) && (x.Category.Audience == HelpAudience.All || x.Category.Audience == wanted));
        }

        if (categoryId is { } category)
        {
            query = query.Where(x => x.Category.Id == category);
        }

        return query;
    }

    private sealed class PublishedRow
    {
        public required HelpArticle Article { get; init; }
        public required HelpCategory Category { get; init; }
    }

    private static HelpAudience? ParseAudience(string? audience) => string.IsNullOrWhiteSpace(audience) || audience.Equals("all", StringComparison.OrdinalIgnoreCase)
        ? null
        : audience.Equals("passenger", StringComparison.OrdinalIgnoreCase) ? HelpAudience.Passenger
        : audience.Equals("driver", StringComparison.OrdinalIgnoreCase) ? HelpAudience.Driver
        : throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["audience"] = "must be passenger|driver" });

    // ----- search -----

    private async Task<List<HelpArticle>> SearchAsync(IQueryable<HelpArticle> published, string term, CancellationToken ct)
    {
        term = term.Length > 100 ? term[..100] : term;
        var tokens = Tokens(term);
        if (tokens.Count == 0)
        {
            return [];
        }

        if (db.Database.IsMySql())
        {
            var boolean = string.Join(' ', tokens.Select(t => $"+{t}*"));
            var ids = (await db.Database.SqlQuery<Guid>($"""
                    SELECT id AS Value FROM help_articles
                    WHERE MATCH(title_ar, title_en, body_ar, body_en) AGAINST ({boolean} IN BOOLEAN MODE)
                    ORDER BY MATCH(title_ar, title_en, body_ar, body_en) AGAINST ({boolean} IN BOOLEAN MODE) DESC
                    LIMIT {MaxSearchCandidates}
                    """).ToListAsync(ct)).ToList();
            if (ids.Count > 0)
            {
                var found = await published.Where(a => ids.Contains(a.Id)).ToListAsync(ct);
                var position = ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
                return found.OrderBy(a => position[a.Id]).ToList();
            }
        }

        return await LikeSearchAsync(published, term, tokens, ct);
    }

    /// <summary>Every token must occur in the title or body of either language; whole-phrase and title hits rank first, then the most read.</summary>
    private static async Task<List<HelpArticle>> LikeSearchAsync(IQueryable<HelpArticle> published, string term, IReadOnlyList<string> tokens, CancellationToken ct)
    {
        var query = published;
        foreach (var token in tokens.Take(6))
        {
            var t = token;
            query = query.Where(a => a.TitleAr.Contains(t) || a.TitleEn.Contains(t) || a.BodyAr.Contains(t) || a.BodyEn.Contains(t));
        }

        var rows = await query.Take(MaxSearchCandidates).ToListAsync(ct);
        int Score(HelpArticle a) =>
            (Contains(a.TitleAr, term) || Contains(a.TitleEn, term) ? 8 : 0)
            + (tokens.Any(t => Contains(a.TitleAr, t) || Contains(a.TitleEn, t)) ? 4 : 0)
            + (Contains(a.BodyAr, term) || Contains(a.BodyEn, term) ? 2 : 0);
        return rows.OrderByDescending(Score).ThenByDescending(a => a.ViewCount).ThenBy(a => a.SortOrder).ToList();
    }

    private static bool Contains(string text, string value) => text.Contains(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>Words of the query without the boolean-mode operators and with a sane length (used by both search paths).</summary>
    public static List<string> Tokens(string term) => Regex.Split(term, @"[\s+\-<>()~*""@]+").Where(t => t.Length > 0).Select(t => t.Length > 40 ? t[..40] : t).Take(8).ToList();

    // ----- text helpers -----

    private static string Localized(string ar, string en, Language language) => language == Language.En ? (string.IsNullOrWhiteSpace(en) ? ar : en) : (string.IsNullOrWhiteSpace(ar) ? en : ar);

    /// <summary>The first <see cref="ExcerptLength"/> characters of the body without Markdown marks.</summary>
    public static string Excerpt(string markdown)
    {
        var text = CodeFence().Replace(markdown, " ");
        text = MarkdownImage().Replace(text, " ");
        text = MarkdownLink().Replace(text, "$1");
        text = MarkdownMarks().Replace(text, " ");
        text = Whitespace().Replace(text, " ").Trim();
        return text.Length <= ExcerptLength ? text : text[..ExcerptLength].TrimEnd();
    }

    private static IReadOnlyList<string>? ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ----- admin: categories -----

    public async Task<IReadOnlyList<AdminHelpCategoryDto>> AdminCategoriesAsync(CancellationToken ct)
    {
        var counts = await db.HelpArticles.AsNoTracking().GroupBy(a => a.CategoryId).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var rows = await db.HelpCategories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Code).ToListAsync(ct);
        return rows.Select(c => ToDto(c, counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<AdminHelpCategoryDto> CreateCategoryAsync(HelpCategoryUpsertRequest request, CancellationToken ct)
    {
        ValidateCategory(request);
        var code = request.Code!.Trim();
        if (await db.HelpCategories.AnyAsync(c => c.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new Dictionary<string, string> { ["code"] = "exists" });
        }

        var category = new HelpCategory { Code = code, NameAr = string.Empty, NameEn = string.Empty, Icon = string.Empty };
        ApplyCategory(category, request);
        db.HelpCategories.Add(category);
        audit.Log("help_category.create", "help_category", category.Id, null, CategorySnapshot(category));
        await db.SaveChangesAsync(ct);
        return ToDto(category, 0);
    }

    public async Task<AdminHelpCategoryDto> UpdateCategoryAsync(Guid id, HelpCategoryUpsertRequest request, CancellationToken ct)
    {
        ValidateCategory(request);
        var category = Guard.NotFound(await db.HelpCategories.FirstOrDefaultAsync(c => c.Id == id, ct));
        var code = request.Code!.Trim();
        if (code != category.Code && await db.HelpCategories.AnyAsync(c => c.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new Dictionary<string, string> { ["code"] = "exists" });
        }

        var before = CategorySnapshot(category);
        category.Code = code;
        ApplyCategory(category, request);
        audit.Log("help_category.update", "help_category", category.Id, before, CategorySnapshot(category));
        await db.SaveChangesAsync(ct);
        return ToDto(category, await db.HelpArticles.CountAsync(a => a.CategoryId == id, ct));
    }

    /// <summary>A category that still has articles cannot be deleted (<c>409 conflict</c>): move or delete them first, or deactivate the category.</summary>
    public async Task DeleteCategoryAsync(Guid id, CancellationToken ct)
    {
        var category = Guard.NotFound(await db.HelpCategories.FirstOrDefaultAsync(c => c.Id == id, ct));
        if (await db.HelpArticles.AnyAsync(a => a.CategoryId == id, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "has_articles" });
        }

        audit.Log("help_category.delete", "help_category", category.Id, CategorySnapshot(category), null);
        db.HelpCategories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private static void ValidateCategory(HelpCategoryUpsertRequest request) => new Validator()
        .Require(nameof(request.Code), request.Code, 40)
        .Rule(nameof(request.Code), string.IsNullOrWhiteSpace(request.Code) || CodePattern().IsMatch(request.Code.Trim()), "must be lowercase letters, digits, hyphens or underscores")
        .Require(nameof(request.NameAr), request.NameAr, 120)
        .Require(nameof(request.NameEn), request.NameEn, 120)
        .Rule(nameof(request.Icon), request.Icon is null || request.Icon.Length <= 40, "max_length:40")
        .ThrowIfInvalid();

    private static void ApplyCategory(HelpCategory category, HelpCategoryUpsertRequest request)
    {
        category.NameAr = request.NameAr!.Trim();
        category.NameEn = request.NameEn!.Trim();
        category.Icon = string.IsNullOrWhiteSpace(request.Icon) ? (string.IsNullOrEmpty(category.Icon) ? "help" : category.Icon) : request.Icon.Trim();
        category.Audience = request.Audience ?? category.Audience;
        category.SortOrder = request.SortOrder ?? category.SortOrder;
        category.IsActive = request.IsActive ?? category.IsActive;
    }

    private static object CategorySnapshot(HelpCategory c) => new { c.Code, c.NameAr, c.NameEn, c.Icon, c.Audience, c.SortOrder, c.IsActive };

    private static AdminHelpCategoryDto ToDto(HelpCategory c, int articles) => new(c.Id, c.Code, c.NameAr, c.NameEn, c.Icon, c.Audience, c.SortOrder, c.IsActive, articles, c.CreatedAt, c.UpdatedAt);

    // ----- admin: articles -----

    public async Task<PagedResult<AdminHelpArticleDto>> AdminArticlesAsync(Guid? categoryId, HelpAudience? audience, bool? published, string? q, Paging paging, CancellationToken ct)
    {
        var query = db.HelpArticles.AsNoTracking().AsQueryable();
        if (categoryId is { } category) query = query.Where(a => a.CategoryId == category);
        if (audience is { } wanted) query = query.Where(a => a.Audience == wanted);
        if (published is { } isPublished) query = query.Where(a => a.IsPublished == isPublished);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(a => a.Slug.Contains(term) || a.TitleAr.Contains(term) || a.TitleEn.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var rows = await (from a in query
                          join c in db.HelpCategories.AsNoTracking() on a.CategoryId equals c.Id
                          orderby c.SortOrder, a.SortOrder, a.Slug
                          select a).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await BuildAdminAsync(rows, ct), total);
    }

    public async Task<AdminHelpArticleDto> AdminArticleAsync(Guid id, CancellationToken ct) =>
        await BuildAdminAsync(Guard.NotFound(await db.HelpArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct)), ct);

    public async Task<AdminHelpArticleDto> CreateArticleAsync(HelpArticleUpsertRequest request, CancellationToken ct)
    {
        await ValidateArticleAsync(request, null, ct);
        var article = new HelpArticle { Slug = string.Empty, TitleAr = string.Empty, TitleEn = string.Empty, BodyAr = string.Empty, BodyEn = string.Empty };
        ApplyArticle(article, request);
        if (request.IsPublished == true)
        {
            article.IsPublished = true;
            article.PublishedAt = clock.UtcNow;
        }

        db.HelpArticles.Add(article);
        audit.Log("help_article.create", "help_article", article.Id, null, ArticleSnapshot(article));
        await db.SaveChangesAsync(ct);
        return await BuildAdminAsync(article, ct);
    }

    public async Task<AdminHelpArticleDto> UpdateArticleAsync(Guid id, HelpArticleUpsertRequest request, CancellationToken ct)
    {
        var article = Guard.NotFound(await db.HelpArticles.FirstOrDefaultAsync(a => a.Id == id, ct));
        await ValidateArticleAsync(request, id, ct);
        var before = ArticleSnapshot(article);
        ApplyArticle(article, request);
        audit.Log("help_article.update", "help_article", article.Id, before, ArticleSnapshot(article));
        await db.SaveChangesAsync(ct);
        return await BuildAdminAsync(article, ct);
    }

    public async Task DeleteArticleAsync(Guid id, CancellationToken ct)
    {
        var article = Guard.NotFound(await db.HelpArticles.FirstOrDefaultAsync(a => a.Id == id, ct));
        audit.Log("help_article.delete", "help_article", article.Id, ArticleSnapshot(article), null);
        db.HelpArticles.Remove(article);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AdminHelpArticleDto> SetPublishedAsync(Guid id, bool publish, CancellationToken ct)
    {
        var article = Guard.NotFound(await db.HelpArticles.FirstOrDefaultAsync(a => a.Id == id, ct));
        if (article.IsPublished != publish)
        {
            var before = ArticleSnapshot(article);
            article.IsPublished = publish;
            if (publish)
            {
                article.PublishedAt ??= clock.UtcNow;
            }

            article.UpdatedBy = currentUser.UserId;
            audit.Log(publish ? "help_article.publish" : "help_article.unpublish", "help_article", article.Id, before, ArticleSnapshot(article));
            await db.SaveChangesAsync(ct);
        }

        return await BuildAdminAsync(article, ct);
    }

    private async Task ValidateArticleAsync(HelpArticleUpsertRequest request, Guid? existingId, CancellationToken ct)
    {
        var tags = request.Tags ?? [];
        new Validator()
            .Require(nameof(request.CategoryId), request.CategoryId)
            .Require(nameof(request.Slug), request.Slug, 120)
            .Rule(nameof(request.Slug), string.IsNullOrWhiteSpace(request.Slug) || SlugPattern().IsMatch(request.Slug.Trim()), "must be lowercase letters, digits and hyphens")
            .Require(nameof(request.TitleAr), request.TitleAr, 200)
            .Require(nameof(request.TitleEn), request.TitleEn, 200)
            .Require(nameof(request.BodyAr), request.BodyAr, 100_000)
            .Require(nameof(request.BodyEn), request.BodyEn, 100_000)
            .Rule(nameof(request.Tags), tags.Count <= 20 && tags.All(t => !string.IsNullOrWhiteSpace(t) && t.Length <= 40), "at most 20 tags of up to 40 characters")
            .ThrowIfInvalid();
        if (!await db.HelpCategories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["categoryId"] = "unknown category" });
        }

        var slug = request.Slug!.Trim();
        if (await db.HelpArticles.AnyAsync(a => a.Slug == slug && a.Id != existingId, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new Dictionary<string, string> { ["slug"] = "exists" });
        }
    }

    private void ApplyArticle(HelpArticle article, HelpArticleUpsertRequest request)
    {
        article.CategoryId = request.CategoryId!.Value;
        article.Slug = request.Slug!.Trim();
        article.TitleAr = request.TitleAr!.Trim();
        article.TitleEn = request.TitleEn!.Trim();
        article.BodyAr = request.BodyAr!;
        article.BodyEn = request.BodyEn!;
        article.Audience = request.Audience ?? article.Audience;
        article.Tags = request.Tags is { Count: > 0 } tags ? JsonSerializer.Serialize(tags.Select(t => t.Trim()).Distinct().ToList()) : null;
        article.SortOrder = request.SortOrder ?? article.SortOrder;
        article.UpdatedBy = currentUser.UserId;
    }

    private static object ArticleSnapshot(HelpArticle a) => new { a.Slug, a.CategoryId, a.TitleAr, a.TitleEn, a.Audience, a.SortOrder, a.IsPublished, a.PublishedAt };

    private async Task<AdminHelpArticleDto> BuildAdminAsync(HelpArticle article, CancellationToken ct) => (await BuildAdminAsync([article], ct))[0];

    private async Task<IReadOnlyList<AdminHelpArticleDto>> BuildAdminAsync(IReadOnlyList<HelpArticle> articles, CancellationToken ct)
    {
        var categoryIds = articles.Select(a => a.CategoryId).Distinct().ToList();
        var categoryNames = await db.HelpCategories.AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.NameAr, ct);
        var userIds = articles.Where(a => a.UpdatedBy != null).Select(a => a.UpdatedBy!.Value).Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.PhoneNumber, ct);
        return articles.Select(a => new AdminHelpArticleDto(a.Id, a.Slug, a.CategoryId, categoryNames.GetValueOrDefault(a.CategoryId), a.TitleAr, a.TitleEn, a.BodyAr, a.BodyEn, a.Audience,
            ParseTags(a.Tags) ?? [], a.SortOrder, a.IsPublished, a.PublishedAt, a.ViewCount, a.HelpfulYes, a.HelpfulNo, a.UpdatedBy, a.UpdatedBy is { } id ? users.GetValueOrDefault(id) : null,
            a.CreatedAt, a.UpdatedAt)).ToList();
    }
}
