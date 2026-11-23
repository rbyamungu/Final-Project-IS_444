namespace Prototype_Final_Project.Models;

/// <summary>
/// Data transfer model representing a developer's formal registration to claim a validated wish concept.
/// Sent in PUT /api/v1/wishes/{id}/status requests.
/// </summary>
public class DeveloperClaim
{
    /// <summary>
    /// Remote ID of the wish being claimed.
    /// </summary>
    public string WishRemoteId { get; set; } = string.Empty;

    /// <summary>
    /// Registered developer or software organization name.
    /// </summary>
    public string DeveloperName { get; set; } = string.Empty;

    /// <summary>
    /// Link to developer GitHub profile, personal site, or studio portfolio.
    /// </summary>
    public string PortfolioUrl { get; set; } = string.Empty;

    /// <summary>
    /// Target release timeline committed by the builder (e.g. 'Q2 2027').
    /// </summary>
    public string TargetReleaseQuarter { get; set; } = string.Empty;

    /// <summary>
    /// New development lifecycle state to assign (typically 'InDevelopment').
    /// </summary>
    public string Status { get; set; } = "InDevelopment";

    /// <summary>
    /// Optional technical notes, planned tech stack, or roadmap overview.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
