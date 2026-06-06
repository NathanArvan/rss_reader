namespace RssReader.Api.Models;

/// <summary>
/// The kind of feed a <see cref="Source"/> represents. Stored as an int.
/// Only <see cref="Rss"/>/<see cref="Atom"/> matter for Phase 1; YouTube and
/// Reddit get dedicated handling in later steps (11/20) but are modelled now so
/// the schema doesn't need to change for them.
/// </summary>
public enum SourceType
{
    Rss = 0,
    Atom = 1,
    YouTube = 2,
    Reddit = 3
}
