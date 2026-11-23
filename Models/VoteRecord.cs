using SQLite;

namespace Prototype_Final_Project.Models;

/// <summary>
/// Domain model recording individual user voting transactions.
/// Stored locally in SQLite to prevent duplicate voting and support optimistic offline tallying.
/// </summary>
[Table("UserVotes")]
public class VoteRecord
{
    /// <summary>
    /// Auto-incrementing primary key for local vote audit logging.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Remote ID of the associated WishItem that received the vote.
    /// </summary>
    [Indexed]
    public string WishRemoteId { get; set; } = string.Empty;

    /// <summary>
    /// Unique user identifier or device installation GUID.
    /// </summary>
    public string UserIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the vote was registered.
    /// </summary>
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
}
