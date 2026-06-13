namespace RssReader.Api.Models;

/// <summary>
/// Tracks a user's explicit triage decision for a feed item. Stored as int.
/// New=0 is the safe default so existing rows are automatically untriaged after migration.
/// </summary>
public enum TriageState
{
    New = 0,
    Kept = 1,
    Dismissed = 2
}
