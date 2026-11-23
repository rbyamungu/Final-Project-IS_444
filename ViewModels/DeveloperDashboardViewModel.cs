using System.Collections.ObjectModel;
using System.Windows.Input;
using Prototype_Final_Project.Models;
using Prototype_Final_Project.Services;

namespace Prototype_Final_Project.ViewModels;

/// <summary>
/// ViewModel driving the Developer Hub where software builders discover pre-validated ideas,
/// claim projects, and advance development lifecycle stages (Section 4 and 7 of Architecture Specification).
/// </summary>
public class DeveloperDashboardViewModel : BaseViewModel
{
    private readonly RestDataService _restService;
    private readonly LocalDatabaseService _localDb;

    /// <summary>
    /// Collection of unclaimed high-demand ideas awaiting software builders.
    /// </summary>
    public ObservableCollection<WishItem> OpenIdeas { get; } = new();

    /// <summary>
    /// Collection of active projects claimed by developers with delivery pipelines.
    /// </summary>
    public ObservableCollection<WishItem> ClaimedProjects { get; } = new();

    private int _openCount;
    /// <summary>
    /// Total count of open unbuilt ideas.
    /// </summary>
    public int OpenCount
    {
        get => _openCount;
        set => SetProperty(ref _openCount, value);
    }

    private int _claimedCount;
    /// <summary>
    /// Count of ideas currently In Development.
    /// </summary>
    public int ClaimedCount
    {
        get => _claimedCount;
        set => SetProperty(ref _claimedCount, value);
    }

    private int _shippedCount;
    /// <summary>
    /// Count of ideas that have been Shipped and published live.
    /// </summary>
    public int ShippedCount
    {
        get => _shippedCount;
        set => SetProperty(ref _shippedCount, value);
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ClaimIdeaCommand { get; }
    public ICommand UpdateStatusCommand { get; }
    public ICommand SelectWishCommand { get; }

    public DeveloperDashboardViewModel(RestDataService restService, LocalDatabaseService localDb)
    {
        _restService = restService;
        _localDb = localDb;

        Title = "Developer Hub";

        RefreshCommand = new Command(async () => await LoadDataAsync());
        ClaimIdeaCommand = new Command<WishItem>(async (item) => await PromptClaimIdeaAsync(item));
        UpdateStatusCommand = new Command<WishItem>(async (item) => await PromptUpdateStatusAsync(item));
        SelectWishCommand = new Command<WishItem>(async (item) =>
        {
            if (item != null)
            {
                await Shell.Current.GoToAsync($"WishDetailPage?WishId={item.RemoteId}");
            }
        });
    }

    /// <summary>
    /// Retrieves all wish records from SQLite and partitions them into open ideas vs claimed projects.
    /// </summary>
    public async Task LoadDataAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        IsRefreshing = true;

        try
        {
            var all = await _localDb.GetCachedWishesAsync();

            OpenIdeas.Clear();
            ClaimedProjects.Clear();

            int open = 0;
            int claimed = 0;
            int shipped = 0;

            foreach (var w in all)
            {
                if (w.LifecycleStatus == "OpenIdea" || string.IsNullOrEmpty(w.ClaimedByDeveloper))
                {
                    OpenIdeas.Add(w);
                    open++;
                }
                else
                {
                    ClaimedProjects.Add(w);
                    if (w.LifecycleStatus == "Shipped")
                        shipped++;
                    else
                        claimed++;
                }
            }

            OpenCount = open;
            ClaimedCount = claimed;
            ShippedCount = shipped;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to load developer dashboard: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Interactively prompts developer for organization credentials and target delivery quarter to claim a project.
    /// </summary>
    private async Task PromptClaimIdeaAsync(WishItem? item)
    {
        if (item is null)
            return;

        string devName = await Shell.Current.DisplayPromptAsync("Claim Project Opportunity", 
            $"Enter your Developer or Studio Name to claim '{item.Title}':", 
            placeholder: "e.g. Acme Mobile Labs", 
            initialValue: "Solo Indie Dev");

        if (string.IsNullOrWhiteSpace(devName))
            return;

        string targetQuarter = await Shell.Current.DisplayActionSheetAsync("Select Target Delivery Window", "Cancel", null,
            "Q1 2027", "Q2 2027", "Q3 2027", "Q4 2027");

        if (targetQuarter == "Cancel" || string.IsNullOrWhiteSpace(targetQuarter))
            return;

        item.ClaimedByDeveloper = devName.Trim();
        item.TargetReleaseQuarter = targetQuarter;
        item.LifecycleStatus = "InDevelopment";

        await _localDb.SaveWishAsync(item);
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await _restService.UpdateDeveloperStatusAsync(item.RemoteId, item.LifecycleStatus, item.TargetReleaseQuarter, item.ClaimedByDeveloper);
        }

        await Shell.Current.DisplayAlertAsync("Project Claimed!", 
            $"You have successfully registered to build '{item.Title}'. It is now marked as 'In Development'.", "Awesome");

        await LoadDataAsync();
    }

    /// <summary>
    /// Updates the commercial lifecycle badge of a claimed project.
    /// </summary>
    private async Task PromptUpdateStatusAsync(WishItem? item)
    {
        if (item is null)
            return;

        string action = await Shell.Current.DisplayActionSheetAsync("Update Development Lifecycle", "Cancel", null,
            "Mark as Under Review", "Mark as In Development", "Mark as Shipped & Live");

        if (action == "Cancel")
            return;

        string newStatus = action switch
        {
            "Mark as Under Review" => "UnderReview",
            "Mark as In Development" => "InDevelopment",
            "Mark as Shipped & Live" => "Shipped",
            _ => item.LifecycleStatus
        };

        item.LifecycleStatus = newStatus;
        await _localDb.SaveWishAsync(item);

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await _restService.UpdateDeveloperStatusAsync(item.RemoteId, item.LifecycleStatus, item.TargetReleaseQuarter, item.ClaimedByDeveloper);
        }

        await LoadDataAsync();
    }
}
