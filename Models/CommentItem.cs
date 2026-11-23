using SQLite;

namespace Prototype_Final_Project.Models;

/// <summary>
/// Represents a community discussion entry or developer feedback attached to a specific wish.
/// Fulfills the community discussion requirements specified in Section 3 of the architecture.
/// </summary>
[Table("WishComments")]
public class CommentItem
{
    /// <summary>
    /// Local SQLite auto-incrementing primary key.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }

    /// <summary>
    /// Unique remote identifier for cross-client cloud synchronization.
    /// </summary>
    [Indexed]
    public string RemoteId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key reference linking this comment to the parent WishItem.
    /// Indexed for rapid relational lookups in SQLite.
    /// </summary>
    [Indexed]
    public string WishRemoteId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the comment author (e.g. "Sarah J.", "NovaLabs Lead Dev").
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Role badge of the author: "Wisher", "Developer", or "Community".
    /// </summary>
    public string AuthorRole { get; set; } = "Community";

    /// <summary>
    /// Text content of the comment or feedback.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the comment was authored (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

