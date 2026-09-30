using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Support;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class HelpCenterTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Api = "/api/v1";

    private static object Category(string code, string nameAr = "فئة اختبار", string nameEn = "Test category", string audience = "all") =>
        new { code, nameAr, nameEn, icon = "help", audience, sortOrder = 500 };

    private static object Article(string categoryId, string slug, string titleAr, string titleEn, string bodyAr, string bodyEn, string audience = "all", bool publish = true, string[]? tags = null) =>
        new { categoryId, slug, titleAr, titleEn, bodyAr, bodyEn, audience, tags, sortOrder = 0, isPublished = publish };

    private async Task<(string Id, string Code)> CreateCategoryAsync(HttpClient admin, string suffix, string audience = "all")
    {
        var code = $"t-{suffix}";
        var response = await admin.PostAsJsonAsync($"{Api}/admin/help/categories", Category(code, audience: audience));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return ((await response.ReadJsonAsync()).GetProperty("id").GetString()!, code);
    }

    private async Task<JsonElement> CreateArticleAsync(HttpClient admin, object body)
    {
        var response = await admin.PostAsJsonAsync($"{Api}/admin/help/articles", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    [Fact]
    public async Task Categories_are_public_filtered_by_audience_and_hidden_until_they_have_a_published_article()
    {
        using var anonymous = fixture.CreateClient();
        var driverCategories = await (await anonymous.GetAsync($"{Api}/help/categories?audience=driver")).ReadJsonAsync();
        var driverCodes = driverCategories.EnumerateArray().Select(c => c.GetProperty("code").GetString()).ToList();
        Assert.Contains("drivers", driverCodes);
        Assert.DoesNotContain("trips", driverCodes);
        Assert.DoesNotContain("payments", driverCodes);

        var passengerCategories = await (await anonymous.GetAsync($"{Api}/help/categories?audience=passenger")).ReadJsonAsync();
        var passengerCodes = passengerCategories.EnumerateArray().Select(c => c.GetProperty("code").GetString()).ToList();
        Assert.Contains("trips", passengerCodes);
        Assert.Contains("payments", passengerCodes);
        Assert.DoesNotContain("drivers", passengerCodes);
        var trips = passengerCategories.EnumerateArray().First(c => c.GetProperty("code").GetString() == "trips");
        Assert.Equal("الرحلات", trips.GetProperty("name").GetString());
        Assert.Equal("car", trips.GetProperty("icon").GetString());
        Assert.Equal(3, trips.GetProperty("articlesCount").GetInt32());

        using var english = fixture.CreateClient(language: "en");
        var englishTrips = (await (await english.GetAsync($"{Api}/help/categories?audience=passenger")).ReadJsonAsync()).EnumerateArray()
            .First(c => c.GetProperty("code").GetString() == "trips");
        Assert.Equal("Trips", englishTrips.GetProperty("name").GetString());

        var bad = await anonymous.GetAsync($"{Api}/help/categories?audience=admin");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

        // A category without a published article is not offered; publishing the first article makes it appear.
        var admin = await fixture.LoginAdminAsync();
        var (categoryId, code) = await CreateCategoryAsync(admin, "visible");
        var all = (await (await anonymous.GetAsync($"{Api}/help/categories")).ReadJsonAsync()).EnumerateArray().Select(c => c.GetProperty("code").GetString());
        Assert.DoesNotContain(code, all);
        var draft = await CreateArticleAsync(admin, Article(categoryId, "t-visible-one", "مقال", "Article", "نص", "Body", publish: false));
        Assert.DoesNotContain(code, (await (await anonymous.GetAsync($"{Api}/help/categories")).ReadJsonAsync()).EnumerateArray().Select(c => c.GetProperty("code").GetString()));
        (await admin.PostAsync($"{Api}/admin/help/articles/{draft.GetProperty("id").GetString()}/publish", null)).EnsureSuccessStatusCode();
        var listed = (await (await anonymous.GetAsync($"{Api}/help/categories")).ReadJsonAsync()).EnumerateArray().First(c => c.GetProperty("code").GetString() == code);
        Assert.Equal(1, listed.GetProperty("articlesCount").GetInt32());
    }

    [Fact]
    public async Task Articles_are_paged_most_read_first_and_filtered_by_category_and_audience()
    {
        using var anonymous = fixture.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            (await anonymous.GetAsync($"{Api}/help/articles/cancellation-fees")).EnsureSuccessStatusCode();
        }

        for (var i = 0; i < 4; i++)
        {
            (await anonymous.GetAsync($"{Api}/help/articles/how-to-schedule-a-ride")).EnsureSuccessStatusCode();
        }

        var popular = await (await anonymous.GetAsync($"{Api}/help/articles?audience=passenger&page=1")).ReadJsonAsync();
        Assert.Equal(1, popular.GetProperty("page").GetInt32());
        Assert.True(popular.GetProperty("total").GetInt32() >= 9);
        var items = popular.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("cancellation-fees", items[0].GetProperty("slug").GetString());
        Assert.Equal("how-to-schedule-a-ride", items[1].GetProperty("slug").GetString());
        Assert.All(items, i => Assert.True(i.GetProperty("excerpt").GetString()!.Length <= 160));
        var excerpt = items[0].GetProperty("excerpt").GetString()!;
        Assert.DoesNotContain("#", excerpt);
        Assert.DoesNotContain("*", excerpt);
        Assert.NotEmpty(items[0].GetProperty("title").GetString()!);

        var driverSlugs = (await (await anonymous.GetAsync($"{Api}/help/articles?audience=driver&pageSize=50")).ReadJsonAsync()).GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("slug").GetString()).ToList();
        Assert.Contains("withdraw-earnings", driverSlugs);
        Assert.Contains("sos-button", driverSlugs);
        Assert.DoesNotContain("cancellation-fees", driverSlugs);
        var passengerSlugs = (await (await anonymous.GetAsync($"{Api}/help/articles?audience=passenger&pageSize=50")).ReadJsonAsync()).GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("slug").GetString()).ToList();
        Assert.DoesNotContain("withdraw-earnings", passengerSlugs);

        var categories = await (await anonymous.GetAsync($"{Api}/help/categories?audience=passenger")).ReadJsonAsync();
        var trips = categories.EnumerateArray().First(c => c.GetProperty("code").GetString() == "trips");
        var inCategory = await (await anonymous.GetAsync($"{Api}/help/articles?categoryId={trips.GetProperty("id").GetString()}&audience=passenger")).ReadJsonAsync();
        Assert.Equal(3, inCategory.GetProperty("total").GetInt32());
        Assert.All(inCategory.GetProperty("items").EnumerateArray(), i => Assert.Equal(trips.GetProperty("id").GetString(), i.GetProperty("categoryId").GetString()));
        // Inside a category the editorial order applies (sort_order), not the read count.
        Assert.Equal("how-to-schedule-a-ride", inCategory.GetProperty("items")[0].GetProperty("slug").GetString());

        var newest = await anonymous.GetAsync($"{Api}/help/articles?sort=newest");
        Assert.Equal(HttpStatusCode.OK, newest.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await anonymous.GetAsync($"{Api}/help/articles?sort=bogus")).StatusCode);

        var paged = await (await anonymous.GetAsync($"{Api}/help/articles?pageSize=2&page=2")).ReadJsonAsync();
        Assert.Equal(2, paged.GetProperty("items").GetArrayLength());
        Assert.Equal(2, paged.GetProperty("page").GetInt32());
        Assert.Equal(2, paged.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task Search_matches_titles_and_bodies_in_both_languages_and_returns_the_requested_language()
    {
        using var arabic = fixture.CreateClient();
        var byTitle = await (await arabic.GetAsync($"{Api}/help/articles?q={Uri.EscapeDataString("جدولة")}")).ReadJsonAsync();
        var slugs = byTitle.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("slug").GetString()).ToList();
        Assert.Contains("how-to-schedule-a-ride", slugs);
        // Title hits rank first: "جدولة" is in the title of the drivers' article ("المجدولة") and only in the body of the rider's one.
        Assert.Equal("scheduled-rides-for-drivers", slugs[0]);
        Assert.True(slugs.IndexOf("how-to-schedule-a-ride") > 0);
        var single = await (await arabic.GetAsync($"{Api}/help/articles?q={Uri.EscapeDataString("أجدول")}")).ReadJsonAsync();
        Assert.Equal(["how-to-schedule-a-ride"], single.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("slug").GetString()!).ToArray());
        Assert.Equal("كيف أجدول رحلة؟", single.GetProperty("items")[0].GetProperty("title").GetString());

        using var english = fixture.CreateClient(language: "en");
        var inEnglish = await (await english.GetAsync($"{Api}/help/articles?q=schedule")).ReadJsonAsync();
        var article = inEnglish.GetProperty("items").EnumerateArray().First(i => i.GetProperty("slug").GetString() == "how-to-schedule-a-ride");
        Assert.Equal("How do I schedule a ride?", article.GetProperty("title").GetString());
        Assert.Contains("Scheduling", article.GetProperty("excerpt").GetString());

        // A word only in a body, several words (all must match), no match, and the audience filter together with the search.
        var body = await (await english.GetAsync($"{Api}/help/articles?q=IBAN")).ReadJsonAsync();
        Assert.Contains("withdraw-earnings", body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("slug").GetString()!));
        var both = await (await english.GetAsync($"{Api}/help/articles?q={Uri.EscapeDataString("pickup zone")}")).ReadJsonAsync();
        Assert.Equal(["airport-pickup"], both.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("slug").GetString()!).Take(1).ToArray());
        var none = await (await english.GetAsync($"{Api}/help/articles?q=zzzunmatchedzzz")).ReadJsonAsync();
        Assert.Equal(0, none.GetProperty("total").GetInt32());
        var driverOnly = await (await english.GetAsync($"{Api}/help/articles?q=IBAN&audience=passenger")).ReadJsonAsync();
        Assert.Equal(0, driverOnly.GetProperty("total").GetInt32());
        // Boolean-mode operators in the query are harmless.
        var operators = await english.GetAsync($"{Api}/help/articles?q={Uri.EscapeDataString("+schedule* -\"ride\" (")}");
        Assert.Equal(HttpStatusCode.OK, operators.StatusCode);
    }

    [Fact]
    public async Task Unpublished_articles_are_hidden_from_every_public_endpoint_until_published()
    {
        var admin = await fixture.LoginAdminAsync();
        var (categoryId, _) = await CreateCategoryAsync(admin, "hidden");
        var published = await CreateArticleAsync(admin, Article(categoryId, "t-hidden-public", "مقال ظاهر", "Visible note", "نص ظاهر فريد", "visible body uniqueword"));
        var draft = await CreateArticleAsync(admin, Article(categoryId, "t-hidden-draft", "مسودة سرية", "Secret draft", "نص مسودة", "draft body secretmarker", publish: false));
        var draftId = draft.GetProperty("id").GetString()!;

        using var anonymous = fixture.CreateClient(language: "en");
        var list = await (await anonymous.GetAsync($"{Api}/help/articles?categoryId={categoryId}")).ReadJsonAsync();
        Assert.Equal(["t-hidden-public"], list.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("slug").GetString()!).ToArray());
        Assert.Equal(0, (await (await anonymous.GetAsync($"{Api}/help/articles?q=secretmarker")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"{Api}/help/articles/t-hidden-draft")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsJsonAsync($"{Api}/help/articles/{draftId}/feedback", new { helpful = true })).StatusCode);
        Assert.Equal("not_found", await (await anonymous.GetAsync($"{Api}/help/articles/t-hidden-draft")).ErrorCodeAsync());

        // The draft is not among the related articles of the published one either.
        var detail = await (await anonymous.GetAsync($"{Api}/help/articles/t-hidden-public")).ReadJsonAsync();
        Assert.Empty(detail.GetProperty("related").EnumerateArray());

        (await admin.PostAsync($"{Api}/admin/help/articles/{draftId}/publish", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"{Api}/help/articles/t-hidden-draft")).StatusCode);
        Assert.Equal(1, (await (await anonymous.GetAsync($"{Api}/help/articles?q=secretmarker")).ReadJsonAsync()).GetProperty("total").GetInt32());
        var related = (await (await anonymous.GetAsync($"{Api}/help/articles/t-hidden-public")).ReadJsonAsync()).GetProperty("related");
        Assert.Equal(["t-hidden-draft"], related.EnumerateArray().Select(r => r.GetProperty("slug").GetString()!).ToArray());

        (await admin.PostAsync($"{Api}/admin/help/articles/{draftId}/unpublish", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"{Api}/help/articles/t-hidden-draft")).StatusCode);

        // An inactive category hides its published articles too.
        var inactive = await admin.PutAsJsonAsync($"{Api}/admin/help/categories/{categoryId}", new { code = "t-hidden", nameAr = "فئة", nameEn = "Category", icon = "help", audience = "all", sortOrder = 500, isActive = false });
        inactive.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"{Api}/help/articles/{published.GetProperty("slug").GetString()}")).StatusCode);
    }

    [Fact]
    public async Task Reading_an_article_returns_markdown_category_tags_related_and_counts_the_view()
    {
        var admin = await fixture.LoginAdminAsync();
        var (categoryId, code) = await CreateCategoryAsync(admin, "detail");
        await CreateArticleAsync(admin, Article(categoryId, "t-detail-main", "الرئيسي", "Main article", "## عنوان\n\nنص **غامق**", "## Heading\n\nSome **bold** text", tags: ["one", "two"]));
        await CreateArticleAsync(admin, Article(categoryId, "t-detail-other", "آخر", "Another article", "نص", "Body"));

        using var anonymous = fixture.CreateClient(language: "en");
        var detail = await (await anonymous.GetAsync($"{Api}/help/articles/t-detail-main")).ReadJsonAsync();
        Assert.Equal("Main article", detail.GetProperty("title").GetString());
        Assert.Equal("## Heading\n\nSome **bold** text", detail.GetProperty("body").GetString());
        Assert.Equal(code, detail.GetProperty("category").GetProperty("code").GetString());
        Assert.Equal("Test category", detail.GetProperty("category").GetProperty("name").GetString());
        Assert.Equal(["one", "two"], detail.GetProperty("tags").EnumerateArray().Select(t => t.GetString()!).ToArray());
        var related = detail.GetProperty("related").EnumerateArray().ToList();
        Assert.Single(related);
        Assert.Equal("t-detail-other", related[0].GetProperty("slug").GetString());
        Assert.Equal("Another article", related[0].GetProperty("title").GetString());

        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.HelpArticles.Where(a => a.Slug == "t-detail-main").Select(a => a.ViewCount).FirstAsync()));
        await anonymous.GetAsync($"{Api}/help/articles/t-detail-main");
        await anonymous.GetAsync($"{Api}/help/articles/t-detail-main");
        Assert.Equal(3, await fixture.Factory.WithDbAsync(db => db.HelpArticles.Where(a => a.Slug == "t-detail-main").Select(a => a.ViewCount).FirstAsync()));
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"{Api}/help/articles/no-such-article")).StatusCode);
    }

    [Fact]
    public async Task Helpful_votes_are_counted_and_limited_to_one_per_ip_article_and_day()
    {
        using var anonymous = fixture.CreateClient();
        var article = await (await anonymous.GetAsync($"{Api}/help/articles/wallet-topup")).ReadJsonAsync();
        var id = article.GetProperty("id").GetString()!;

        var yes = await anonymous.PostAsJsonAsync($"{Api}/help/articles/{id}/feedback", new { helpful = true });
        Assert.Equal(HttpStatusCode.NoContent, yes.StatusCode);
        var again = await anonymous.PostAsJsonAsync($"{Api}/help/articles/{id}/feedback", new { helpful = false });
        Assert.Equal(HttpStatusCode.TooManyRequests, again.StatusCode);
        Assert.Equal("rate_limited", await again.ErrorCodeAsync());
        Assert.True(again.Headers.Contains("Retry-After"));

        var other = (await (await anonymous.GetAsync($"{Api}/help/articles/payment-methods")).ReadJsonAsync()).GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.PostAsJsonAsync($"{Api}/help/articles/{other}/feedback", new { helpful = false })).StatusCode);

        var counts = await fixture.Factory.WithDbAsync(db => db.HelpArticles.Where(a => a.Id == Guid.Parse(id)).Select(a => new { a.HelpfulYes, a.HelpfulNo }).FirstAsync());
        Assert.Equal((1, 0), (counts.HelpfulYes, counts.HelpfulNo));
        var otherCounts = await fixture.Factory.WithDbAsync(db => db.HelpArticles.Where(a => a.Id == Guid.Parse(other!)).Select(a => new { a.HelpfulYes, a.HelpfulNo }).FirstAsync());
        Assert.Equal((0, 1), (otherCounts.HelpfulYes, otherCounts.HelpfulNo));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await anonymous.PostAsJsonAsync($"{Api}/help/articles/{id}/feedback", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.PostAsJsonAsync($"{Api}/help/articles/{Guid.NewGuid()}/feedback", new { helpful = true })).StatusCode);

        // The next (Riyadh) day the same IP may vote again.
        fixture.Factory.Clock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.PostAsJsonAsync($"{Api}/help/articles/{id}/feedback", new { helpful = true })).StatusCode);
        fixture.Factory.Clock.Advance(TimeSpan.FromDays(-1));
    }

    [Fact]
    public async Task Admin_console_manages_categories_and_articles_with_validation_publishing_and_audit()
    {
        var admin = await fixture.LoginAdminAsync();
        var (categoryId, code) = await CreateCategoryAsync(admin, "console");
        var duplicate = await admin.PostAsJsonAsync($"{Api}/admin/help/categories", Category(code));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"{Api}/admin/help/categories", Category("Bad Code!"))).StatusCode);

        var created = await CreateArticleAsync(admin, Article(categoryId, "t-console-article", "عنوان", "Title", "نص", "Body", publish: false, tags: ["a"]));
        var id = created.GetProperty("id").GetString()!;
        Assert.False(created.GetProperty("isPublished").GetBoolean());
        Assert.Equal(JsonValueKind.Null, created.GetProperty("publishedAt").ValueKind);
        Assert.Equal(["a"], created.GetProperty("tags").EnumerateArray().Select(t => t.GetString()!).ToArray());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Api}/admin/help/articles", Article(categoryId, "t-console-article", "x", "y", "z", "w"))).StatusCode);
        var invalid = await admin.PostAsJsonAsync($"{Api}/admin/help/articles", Article(categoryId, "Not A Slug", "x", "y", "z", "w"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"{Api}/admin/help/articles", Article(Guid.NewGuid().ToString(), "t-console-x", "x", "y", "z", "w"))).StatusCode);

        var updated = await admin.PutAsJsonAsync($"{Api}/admin/help/articles/{id}", Article(categoryId, "t-console-article", "عنوان جديد", "New title", "نص جديد", "New body", audience: "driver"));
        updated.EnsureSuccessStatusCode();
        var updatedBody = await updated.ReadJsonAsync();
        Assert.Equal("New title", updatedBody.GetProperty("titleEn").GetString());
        Assert.Equal("driver", updatedBody.GetProperty("audience").GetString());
        Assert.NotNull(updatedBody.GetProperty("updatedByName").GetString());

        var published = await (await admin.PostAsync($"{Api}/admin/help/articles/{id}/publish", null)).ReadJsonAsync();
        Assert.True(published.GetProperty("isPublished").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, published.GetProperty("publishedAt").ValueKind);
        var unpublished = await (await admin.PostAsync($"{Api}/admin/help/articles/{id}/unpublish", null)).ReadJsonAsync();
        Assert.False(unpublished.GetProperty("isPublished").GetBoolean());

        var listed = await (await admin.GetAsync($"{Api}/admin/help/articles?categoryId={categoryId}&published=false")).ReadJsonAsync();
        Assert.Equal(1, listed.GetProperty("total").GetInt32());
        Assert.Equal("t-console-article", listed.GetProperty("items")[0].GetProperty("slug").GetString());
        var categories = await (await admin.GetAsync($"{Api}/admin/help/categories")).ReadJsonAsync();
        Assert.Equal(1, categories.EnumerateArray().First(c => c.GetProperty("id").GetString() == categoryId).GetProperty("articlesCount").GetInt32());

        // A category with articles cannot be deleted; an empty one can.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"{Api}/admin/help/categories/{categoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Api}/admin/help/articles/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Api}/admin/help/articles/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Api}/admin/help/categories/{categoryId}")).StatusCode);

        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "help_article" || a.EntityType == "help_category").Select(a => a.Action).ToListAsync());
        foreach (var action in new[] { "help_category.create", "help_category.delete", "help_article.create", "help_article.update", "help_article.publish", "help_article.unpublish", "help_article.delete" })
        {
            Assert.Contains(action, actions);
        }
    }

    [Fact]
    public async Task Admin_help_endpoints_require_the_help_manage_permission()
    {
        var (passenger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync($"{Api}/admin/help/articles")).StatusCode);
        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Api}/admin/help/articles")).StatusCode);

        var supportOnly = await SupportFlow.AdminWithAsync(fixture, "help-no-perm", "support.manage", "support.view");
        var denied = await supportOnly.GetAsync($"{Api}/admin/help/categories");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("forbidden", await denied.ErrorCodeAsync());
        var editor = await SupportFlow.AdminWithAsync(fixture, "help-editor", "help.manage");
        Assert.Equal(HttpStatusCode.OK, (await editor.GetAsync($"{Api}/admin/help/categories")).StatusCode);
    }

    [Fact]
    public void Excerpts_strip_markdown_and_keep_the_first_160_characters_and_search_tokens_drop_operators()
    {
        var markdown = "## عنوان\n\n1. اختر **وجهتك** ثم [اضغط هنا](https://ata.sa) `code`\n\n```\nblock\n```\n" + new string('ب', 400);
        var excerpt = HelpService.Excerpt(markdown);
        Assert.True(excerpt.Length <= HelpService.ExcerptLength);
        Assert.StartsWith("عنوان 1. اختر وجهتك ثم اضغط هنا", excerpt);
        Assert.DoesNotContain("https", excerpt);
        Assert.DoesNotContain("block", excerpt);
        Assert.Equal("Short text", HelpService.Excerpt("**Short** text"));

        Assert.Equal(["how", "to", "schedule"], HelpService.Tokens("+how -to* \"schedule\" ("));
        Assert.Empty(HelpService.Tokens(" +-*() "));
    }
}
