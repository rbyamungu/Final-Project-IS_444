using System.Windows.Input;
using Prototype_Final_Project.Models;
using Prototype_Final_Project.Services;

namespace Prototype_Final_Project.ViewModels;

/// <summary>
/// ViewModel orchestrating dual-mode demand submission (New App Concept vs Feature for Existing App).
/// Provides reactive client-side validation and multi-tier persistence (Section 5 and 7 of Architecture Specification).
/// </summary>
public class SubmitWishViewModel : BaseViewModel
{
    private readonly RestDataService _restService;
    private readonly LocalDatabaseService _localDb;

    private string _selectedSubmissionType = "New App Concept";
    private string _title = string.Empty;
    private string _existingAppName = string.Empty;
    private string _targetPlatform = "Cross-Platform (iOS + Android)";
    private string _category = "Productivity";
    private string _targetPersona = string.Empty;
    private string _problemStatement = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _hasStatusMessage;

    /// <summary>
    /// Selected submission mode: 'New App Concept' or 'Feature for Existing App'.
    /// Updates IsFeatureRequest dynamically when altered.
    /// </summary>
    public string SelectedSubmissionType
    {
        get => _selectedSubmissionType;
        set
        {
            if (SetProperty(ref _selectedSubmissionType, value))
            {
                OnPropertyChanged(nameof(IsFeatureRequest));
            }
        }
    }

    /// <summary>
    /// Calculated boolean property driving the visibility of the ExistingAppName field in SubmitWishPage.xaml.
    /// </summary>
    public bool IsFeatureRequest => SelectedSubmissionType == "Feature for Existing App";

    /// <summary>
    /// Title of the requested application or enhancement.
    /// </summary>
    public string WishTitle
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Commercial application name if this submission is an enhancement request (e.g. Spotify, Notion).
    /// </summary>
    public string ExistingAppName
    {
        get => _existingAppName;
        set => SetProperty(ref _existingAppName, value);
    }

    /// <summary>
    /// Target mobile operating system (Cross-Platform, iOS, Android, Web).
    /// </summary>
    public string TargetPlatform
    {
        get => _targetPlatform;
        set => SetProperty(ref _targetPlatform, value);
    }

    /// <summary>
    /// Application category classification (Productivity, Utilities, Developer Tools, etc.).
    /// </summary>
    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    /// <summary>
    /// Target user demographic or persona experiencing the unmet need.
    /// </summary>
    public string TargetPersona
    {
        get => _targetPersona;
        set => SetProperty(ref _targetPersona, value);
    }

    /// <summary>
    /// In-depth problem statement and description of current functional limitations.
    /// </summary>
    public string ProblemStatement
    {
        get => _problemStatement;
        set => SetProperty(ref _problemStatement, value);
    }

    /// <summary>
    /// Informational message presented after submission attempts.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool HasStatusMessage
    {
        get => _hasStatusMessage;
        set => SetProperty(ref _hasStatusMessage, value);
    }

    /// <summary>
    /// Asynchronous command to validate and publish the wish to the community feed.
    /// </summary>
    public ICommand SubmitWishCommand { get; }

    /// <summary>
    /// Command to reset all form input fields to clean default states.
    /// </summary>
    public ICommand ResetFormCommand { get; }

    public SubmitWishViewModel(RestDataService restService, LocalDatabaseService localDb)
    {
        _restService = restService;
        _localDb = localDb;

        Title = "Submit Wish";
        SubmitWishCommand = new Command(async () => await ExecuteSubmitAsync());
        ResetFormCommand = new Command(ResetForm);
    }

    /// <summary>
    /// Resets all input bindings back to clean initial states.
    /// </summary>
    private void ResetForm()
    {
        WishTitle = string.Empty;
        ExistingAppName = string.Empty;
        TargetPersona = string.Empty;
        ProblemStatement = string.Empty;
        StatusMessage = string.Empty;
        HasStatusMessage = false;
    }

    /// <summary>
    /// Validates inputs, creates new WishItem, persists locally to SQLite, and synchronizes with REST endpoint.
    /// </summary>
    private async Task ExecuteSubmitAsync()
    {
        if (IsBusy)
            return;

        // Perform strict client-side validation
        if (string.IsNullOrWhiteSpace(WishTitle))
        {
            await Shell.Current.DisplayAlertAsync("Validation Error", "Please provide a descriptive title for your wish.", "OK");
            return;
        }

        if (IsFeatureRequest && string.IsNullOrWhiteSpace(ExistingAppName))
        {
            await Shell.Current.DisplayAlertAsync("Validation Error", "Please specify the existing app name for your feature request.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ProblemStatement) || ProblemStatement.Trim().Length < 10)
        {
            await Shell.Current.DisplayAlertAsync("Validation Error", "Please describe the problem or pain point in at least 10 characters.", "OK");
            return;
        }

        IsBusy = true;

        try
        {
            // Construct domain model entity
            var newWish = new WishItem
            {
                RemoteId = $"WISH-{Random.Shared.Next(1100, 9999)}",
                Title = WishTitle.Trim(),
                SubmissionType = IsFeatureRequest ? "FeatureRequest" : "NewAppConcept",
                ExistingAppName = IsFeatureRequest ? ExistingAppName.Trim() : string.Empty,
                TargetPlatform = TargetPlatform,
                Category = Category,
                TargetPersona = string.IsNullOrWhiteSpace(TargetPersona) ? "Mobile Users" : TargetPersona.Trim(),
                ProblemStatement = ProblemStatement.Trim(),
                UpvoteCount = 1,
                HasUserUpvoted = true,
                LifecycleStatus = "OpenIdea",
                CreatedAt = DateTime.UtcNow,
                LastSynchronizedAt = DateTime.UtcNow
            };

            // Tier 1: Persist to local SQLite storage
            await _localDb.SaveWishAsync(newWish);

            // Tier 2: Synchronize with remote REST endpoint if online
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await _restService.CreateWishAsync(newWish);
            }

            await Shell.Current.DisplayAlertAsync("Wish Published!", 
                $"Your wish '{newWish.Title}' has been added to the demand-validation feed.", "View in Feed");

            ResetForm();

            // Navigate back to the trending feed
            await Shell.Current.GoToAsync("//DiscoveryFeedPage");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Could not publish wish: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        base.SetProperty(ref _selectedSubmissionType, _selectedSubmissionType, propertyName);
    }
}
