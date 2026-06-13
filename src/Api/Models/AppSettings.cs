namespace RssReader.Api.Models;

/// <summary>Single-row app-wide settings. Id is always 1, seeded via migration. [phase: 2]</summary>
public class AppSettings
{
    public int Id { get; set; }
    public DateTime? LastOpenedUtc { get; set; }
}
