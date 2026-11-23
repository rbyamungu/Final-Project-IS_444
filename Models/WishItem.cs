using SQLite;

namespace Prototype_Final_Project.Models;

/// <summary>
/// Core domain entity representing a crowd-sourced software wish or feature request.
/// Maps directly to the SQLite 'Wishes' relational table for offline caching and feed display.
/// Defined in Section 3 of the Prototype Architecture Specification.
/// </summary>
[Table("Wishes")]
public class WishItem
{
    /// <summary>
    /// Local auto-incrementing primary key for device-specific SQLite caching.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }

    /// <summary>
    /// Global unique identifier used across cloud RESTful endpoints.
    /// Indexed to optimize lookup performance.
    /// </summary>
    [Indexed]
    public string RemoteId { get; set; } = string.Empty;

    /// <summary>
    /// Concise summary title of the requested software application or enhancement.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Type of demand submission: 'NewAppConcept' (standalone app) or 'FeatureRequest' (enhancement to existing product).
    /// </summary>
    public string SubmissionType { get; set; } = "NewAppConcept";

    /// <summary>
    /// Target mobile operating system: 'Cross-Platform', 'iOS', 'Android', or 'Web Application'.
    /// </summary>
    public string TargetPlatform { get; set; } = "Cross-Platform";

    /// <summary>
    /// Industry category: Productivity, Developer Tools, Finance, Health, Social, etc.
    /// </summary>
    public string Category { get; set; } = "Productivity";

    /// <summary>
    /// Detailed description of the unmet consumer pain point and why existing solutions fail.
    /// </summary>
    public string ProblemStatement { get; set; } = string.Empty;

    /// <summary>
    /// Intended demographic or target consumer persona (e.g. 'College Students', 'Remote Engineers').
    /// </summary>
    public string TargetPersona { get; set; } = string.Empty;

    /// <summary>
    /// Name of the commercial application if this submission is a FeatureRequest (e.g. 'Spotify', 'Slack').
    /// </summary>
    public string ExistingAppName { get; set; } = string.Empty;

    /// <summary>
    /// Aggregate community vote tally measuring consumer demand velocity.
    /// </summary>
    public int UpvoteCount { get; set; }

    /// <summary>
    /// Tracks whether the local authenticated user has cast a vote for this wish.
    /// </summary>
    public bool HasUserUpvoted { get; set; }

    /// <summary>
    /// Current development maturity lifecycle stage:
    /// 'OpenIdea', 'UnderReview', 'InDevelopment', or 'Shipped'.
    /// </summary>
    public string LifecycleStatus { get; set; } = "OpenIdea";

    /// <summary>
    /// Developer or studio name that has claimed responsibility for building the solution.
    /// </summary>
    public string ClaimedByDeveloper { get; set; } = string.Empty;

    /// <summary>
    /// Estimated delivery quarter committed by the developer (e.g. 'Q3 2026').
    /// </summary>
    public string TargetReleaseQuarter { get; set; } = string.Empty;

    /// <summary>
    /// External hyperlink to source code repository, prototype landing page, or app store listing.
    /// </summary>
    public string ExternalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the wish was submitted (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the wish record was last reconciled with the remote REST server.
    /// </summary>
    public DateTime LastSynchronizedAt { get; set; } = DateTime.UtcNow;
}
