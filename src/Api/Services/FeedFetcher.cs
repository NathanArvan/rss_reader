using System.Net;
using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using RssReader.Api.Data;
using RssReader.Api.Models;

namespace RssReader.Api.Services;

public interface IFeedFetcher
{
    Task FetchAsync(Source source, CancellationToken ct);
}

public class FeedFetcher : IFeedFetcher
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<FeedFetcher> _logger;

    public FeedFetcher(AppDbContext db, HttpClient httpClient, ILogger<FeedFetcher> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task FetchAsync(Source source, CancellationToken ct) => Task.CompletedTask; // stub
}
