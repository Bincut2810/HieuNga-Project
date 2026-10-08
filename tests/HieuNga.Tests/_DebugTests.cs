using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HieuNga.Domain.Entities;
using HieuNga.Domain.Enums;
using HieuNga.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace HieuNga.Tests;

public class _DebugTests : IClassFixture<HieuNgaTestAppFactory>
{
    private readonly HieuNgaTestAppFactory _factory;
    private readonly ITestOutputHelper _out;

    public _DebugTests(HieuNgaTestAppFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _out = output;
    }

    [Fact]
    public async Task PostAndInspectResponse()
    {
        using var seedDb = _factory.CreateDbContext();
        var bike = new Motorcycle
        {
            Id = Guid.NewGuid(),
            Name = "Airblade Marvel",
            Slug = "airblade-marvel",
            Category = MotorcycleCategory.Scooter,
            BasePrice = 41_290_000m,
            ShortDescription = "Xe ga thể thao",
            Description = "Mô tả dài ban đầu",
            IsFeatured = false,
            IsPublished = true,
            SortOrder = 5,
            ThumbnailUrl = "https://res.cloudinary.com/demo/seed.jpg",
        };
        seedDb.Motorcycles.Add(bike);
        await seedDb.SaveChangesAsync();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        // GET the editor
        var getUrl = $"/admin/xe/editor/{bike.Id}?tab=general";
        var getResp = await client.GetAsync(getUrl);
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var html = await getResp.Content.ReadAsStringAsync();

        // Extract antiforgery token
        var tokenMatch = Regex.Match(html,
            @"name=""__RequestVerificationToken""[^>]*value=""(?<tok>[^""]+)""");
        Assert.True(tokenMatch.Success, "AF token not found");
        var token = tokenMatch.Groups["tok"].Value;

        // POST form
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = bike.Id.ToString(),
            ["Tab"] = "general",
            ["PublishStatus"] = "",
            ["Input.Name"] = "Airblade Marvel",
            ["Input.Slug"] = "",
            ["Input.Category"] = ((int)MotorcycleCategory.ConTay).ToString(),
            ["Input.BasePrice"] = "50500000",
            ["Input.ShortDescription"] = "Xe ga thể thao",
            ["Input.Description"] = "Mô tả dài ban đầu",
            ["Input.SortOrder"] = "5",
            ["Input.IsFeatured"] = "false",
            ["Input.IsPublished"] = "true",
            ["Input.ThumbnailUrl"] = "https://res.cloudinary.com/demo/seed.jpg",
            ["Input.MetaTitle"] = "",
            ["Input.MetaDescription"] = "",
            ["Input.MetaKeywords"] = "",
            ["Input.OgImageUrl"] = "",
            ["Input.CanonicalUrl"] = "",
        };
        using var form = new FormUrlEncodedContent(fields);
        var postUrl = $"/admin/xe/editor/{bike.Id}?handler=SaveGeneral&tab=general";
        var postResp = await client.PostAsync(postUrl, form);
        var respBody = await postResp.Content.ReadAsStringAsync();

        _out.WriteLine($"POST status: {(int)postResp.StatusCode} {postResp.StatusCode}");
        _out.WriteLine($"Location header: {postResp.Headers.Location}");
        _out.WriteLine($"Body length: {respBody.Length}");

        // Search for any error markers
        if (respBody.Contains("admin-validation-summary"))
        {
            var sidx = respBody.IndexOf("admin-validation-summary");
            var excerpt = respBody.Substring(Math.Max(0, sidx - 50), Math.Min(800, respBody.Length - sidx + 50));
            _out.WriteLine($"Validation summary excerpt:\n{excerpt}");
        }
        if (respBody.Contains("Đã xảy ra lỗi"))
        {
            var sidx = respBody.IndexOf("Đã xảy ra lỗi");
            var excerpt = respBody.Substring(Math.Max(0, sidx - 50), Math.Min(400, respBody.Length - sidx + 50));
            _out.WriteLine($"Error excerpt:\n{excerpt}");
        }

        // Save full body to a file for inspection
        await File.WriteAllTextAsync(@"d:\HieuNga-Project\_postresp.html", respBody, Encoding.UTF8);
    }
}