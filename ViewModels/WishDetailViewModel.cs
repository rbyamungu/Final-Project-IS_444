using System.Collections.ObjectModel;
using System.Windows.Input;
using Prototype_Final_Project.Models;
using Prototype_Final_Project.Services;

namespace Prototype_Final_Project.ViewModels;

/// <summary>
/// ViewModel managing detailed wish inspection, upvoting, developer claiming,
/// and community discussion threads (Section 7 and 4 of Architecture Specification).
/// Consumes navigation arguments via [QueryProperty].
/// </summary>
[QueryProperty(nameof(WishId), "WishId")]
public class WishDetailViewModel : BaseViewModel
{
    private readonly RestDataService _restService;
    private readonly LocalDatabaseService _localDb;
    private string _wishId = string.Empty;
    private WishItem? _wish;
    private string _newCommentText = string.Empty;
    private string _newCommentAuthor = string.Empty;
    private string _newCommentRole = "Community";

    /// <summary>
    /// Community discussion comments attached to this wish.
    /// </summary>
    public ObservableCollection<CommentItem> Comments { get; } = new();

    /// <summary>
    /// Query property populated by Shell navigation route (e.g. WishDetailPage?WishId=WISH-1001).
    /// </summary>
    public string WishId
    {
        get => _wishId;
        set
        {
            if (SetProperty(ref _wishId, value))
            {
                _ = LoadWishDetailsAsync(value);
            }
        }
    }

    /// <summary>
    /// The active wish entity bound to the presentation view.
    /// </summary>
    public WishItem? Wish
    {
        get => _wish;
        set
        {
            if (SetProperty(ref _wish, value))
            {
                OnPropertyChanged(nameof(IsClaimed));
                OnPropertyChanged(nameof(IsFeatureRequest));
                OnPropertyChanged(nameof(HasExternalUrl));
            }
        }
    }

    /// <summary>
    /// Text input for the new comment being authored.
    /// </summary>
    public string NewCommentText
    {
        get => _newCommentText;
        set => SetProperty(ref _newCommentText, value);
    }

    /// <summary>
    /// Author name input for the new comment.
    /// </summary>
    public string NewCommentAuthor
    {
        get => _newCommentAuthor;
        set => SetProperty(ref _newCommentAuthor, value);
    }

    /// <summary>
    /// Role of the comment author ("Wisher", "Developer", "Community").
    /// </summary>
    public string NewCommentRole
    {
        get => _newCommentRole;
        set => SetProperty(ref _newCommentRole, value);
    }

    public bool IsClaimed => !string.IsNullOrEmpty(Wish?.ClaimedByDeveloper);
    public bool IsFeatureRequest => Wish?.SubmissionType == "FeatureRequest";
    public bool HasExternalUrl => !string.IsNullOrEmpty(Wish?.ExternalUrl);

    public ICommand UpvoteCommand { get; }
    public ICommand ClaimProjectCommand { get; }
    public ICommand OpenExternalLinkCommand { get; }
    public ICommand AddCommentCommand { get; }

    public WishDetailViewModel(RestDataService restService, LocalDatabaseService localDb)
    {
        _restService = restService;
        _localDb = localDb;

        Title = "Wish Specifications";
        UpvoteCommand = new Command(async () => await ExecuteUpvoteAsync());
        ClaimProjectCommand = new Command(async () => await ExecuteClaimAsync());
        AddCommentCommand = new Command(async () => await ExecuteAddCommentAsync());
        OpenExternalLinkCommand = new Command(async () =>
        {
            if (!string.IsNullOrEmpty(Wish?.ExternalUrl))
            {
                try
                {
                    await Launcher.Default.OpenAsync(new Uri(Wish.ExternalUrl));
                }
                catch { }
            }
        });
    }

    /// <summary>
    /// Loads the wish data from SQLite storage and hydrates the comments discussion list.
    /// </summary>
    public async Task LoadWishDetailsAsync(string remoteId)
    {
        if (string.IsNullOrEmpty(remoteId))
            return;

        IsBusy = true;
        try
        {
            Wish = await _localDb.GetWishByRemoteIdAsync(remoteId);
            await LoadCommentsAsync(remoteId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Fetches community comments for the wish from SQLite and remote REST service.
    /// </summary>
    private async Task LoadCommentsAsync(string remoteId)
    {
        var localComments = await _localDb.GetCommentsForWishAsync(remoteId);
        Comments.Clear();
        foreach (var c in localComments)
        {
            Comments.Add(c);
        }

        // Sync with remote comments if connected
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            var remoteComments = await _restService.GetCommentsAsync(remoteId);
            var list = remoteComments.ToList();
            if (list.Count > 0)
            {
                foreach (var rc in list)
                {
                    await _localDb.SaveCommentAsync(rc);
                }
                localComments = await _localDb.GetCommentsForWishAsync(remoteId);
                Comments.Clear();
                foreach (var c in localComments)
                {
                    Comments.Add(c);
                }
            }
        }
    }

    /// <summary>
    /// Posts a new user or developer discussion comment.
    /// </summary>
    private async Task ExecuteAddCommentAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCommentText))
        {
            await Shell.Current.DisplayAlertAsync("Empty Comment", "Please type your comment or feedback before posting.", "OK");
            return;
        }

        string author = string.IsNullOrWhiteSpace(NewCommentAuthor) ? "Community Member" : NewCommentAuthor.Trim();

        var comment = new CommentItem
        {
            RemoteId = $"COMM-{Random.Shared.Next(1000, 9999)}",
            WishRemoteId = Wish?.RemoteId ?? WishId,
            AuthorName = author,
            AuthorRole = NewCommentRole,
            Content = NewCommentText.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // Persist to local SQLite
        await _localDb.SaveCommentAsync(comment);
        Comments.Add(comment);

        // Reset input fields
        NewCommentText = string.Empty;

        // Sync with remote REST API if online
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await _restService.PostCommentAsync(comment);
        }

        await Shell.Current.DisplayAlertAsync("Comment Posted", "Your feedback has been added to the discussion thread.", "OK");
    }

    /// <summary>
    /// Toggles the user's upvote optimistically in the UI and synchronizes with SQLite and cloud API.
    /// </summary>
    private async Task ExecuteUpvoteAsync()
    {
        if (Wish is null)
            return;

        if (Wish.HasUserUpvoted)
        {
            Wish.UpvoteCount = Math.Max(0, Wish.UpvoteCount - 1);
            Wish.HasUserUpvoted = false;
        }
        else
        {
            Wish.UpvoteCount++;
            Wish.HasUserUpvoted = true;
        }

        OnPropertyChanged(nameof(Wish));
        await _localDb.SaveWishAsync(Wish);

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await _restService.PostUpvoteAsync(Wish.RemoteId);
        }
    }

    /// <summary>
    /// Handles the developer claim flow with modal prompts.
    /// </summary>
    private async Task ExecuteClaimAsync()
    {
        if (Wish is null)
            return;

        string devName = await Shell.Current.DisplayPromptAsync("Claim Wish as Developer", 
            "Enter your developer or team name:", 
            placeholder: "e.g. BlueSky Apps", 
            initialValue: "Mobile Engineer");

        if (string.IsNullOrWhiteSpace(devName))
            return;

        string quarter = await Shell.Current.DisplayActionSheetAsync("Target Release Quarter", "Cancel", null,
            "Q1 2027", "Q2 2027", "Q3 2027", "Q4 2027");

        if (quarter == "Cancel" || string.IsNullOrWhiteSpace(quarter))
            return;

        Wish.ClaimedByDeveloper = devName.Trim();
        Wish.TargetReleaseQuarter = quarter;
        Wish.LifecycleStatus = "InDevelopment";

        OnPropertyChanged(nameof(Wish));
        OnPropertyChanged(nameof(IsClaimed));

        await _localDb.SaveWishAsync(Wish);

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await _restService.UpdateDeveloperStatusAsync(Wish.RemoteId, Wish.LifecycleStatus, Wish.TargetReleaseQuarter, Wish.ClaimedByDeveloper);
        }

        await Shell.Current.DisplayAlertAsync("Project Claimed!", 
            $"Congratulations! You are officially registered as the developer for '{Wish.Title}'.", "OK");
    }

    private void OnPropertyChanged(string propertyName)
    {
        base.SetProperty(ref _wish, _wish, propertyName);
    }
}
