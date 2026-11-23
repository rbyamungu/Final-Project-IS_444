using System.Collections.ObjectModel;
using System.Windows.Input;
using Prototype_Final_Project.Models;
using Prototype_Final_Project.Services;

namespace Prototype_Final_Project.ViewModels;

/// <summary>
/// ViewModel coordinating the community demand-validation discovery feed.
/// Handles instant local SQLite hydration, optimistic upvoting, sorting, and text searching.
/// Architectural Tier: Application State Tier (Section 7 of Architecture Specification).
/// </summary>
public class DiscoveryFeedViewModel : BaseViewModel
{
    private readonly RestDataService _restService;
    private readonly LocalDatabaseService _localDb;
    private List<WishItem> _allLoadedWishes = new();
    private bool _isOffline;
    private bool _isRefreshing;
    private string _activeFilter = "Trending";
    private string _searchText = string.Empty;

    /// <summary>
    /// Active data-bound collection displayed in the CollectionView.
    /// Insertion/deletion automatically refreshes the UI via INotifyCollectionChanged.
    /// </summary>
    public ObservableCollection<WishItem> Wishes { get; } = new();

    /// <summary>
    /// Flag indicating whether the application is running in disconnected offline mode.
    /// Controls the visibility of the yellow warning banner in DiscoveryFeedPage.xaml.
    /// </summary>
    public bool IsOffline
    {
        get => _isOffline;
        set => SetProperty(ref _isOffline, value);
    }

    /// <summary>
    /// Flag bound to RefreshView.IsRefreshing to display the native pull-to-refresh spinner.
    /// </summary>
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    /// <summary>
    /// Currently active sort/filter tab: 'Trending', 'Top', 'Recent', or 'Claimed'.
    /// </summary>
    public string ActiveFilter
    {
        get => _activeFilter;
        set => SetProperty(ref _activeFilter, value);
    }

    /// <summary>
    /// Filter query text bound to the SearchBar. Re-filters items in real-time as the user types.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyCurrentFilter();
            }
        }
    }

    /// <summary>
    /// Command triggered by pull-to-refresh or page initial appearance.
    /// </summary>
    public ICommand RefreshFeedCommand { get; }

    /// <summary>
    /// Command triggered when a user taps the upvote button on a wish card.
    /// </summary>
    public ICommand UpvoteCommand { get; }

    /// <summary>
    /// Command triggered when a user taps a filter pill button ('Trending', 'Top', etc.).
    /// </summary>
    public ICommand ChangeFilterCommand { get; }

    /// <summary>
    /// Command executed when a wish card is selected to navigate to WishDetailPage.
    /// </summary>
    public ICommand WishSelectedCommand { get; }

    /// <summary>
    /// Injects required data services and initializes UI commands.
    /// </summary>
    public DiscoveryFeedViewModel(RestDataService restService, LocalDatabaseService localDb)
    {
        _restService = restService;
        _localDb = localDb;

        RefreshFeedCommand = new Command(async () => await LoadFeedAsync());
        UpvoteCommand = new Command<WishItem>(async (item) => await ExecuteUpvoteAsync(item));
        ChangeFilterCommand = new Command<string>((filter) =>
        {
            ActiveFilter = filter ?? "Trending";
            ApplyCurrentFilter();
        });
        WishSelectedCommand = new Command<WishItem>(async (item) =>
        {
            if (item != null)
            {
                // Decoupled Shell URI navigation with query parameter
                await Shell.Current.GoToAsync($"WishDetailPage?WishId={item.RemoteId}");
            }
        });

        Title = "Explore Wishes";
    }

    /// <summary>
    /// Orchestrates multi-tier data retrieval:
    /// 1. Instantly loads cached wishes from local SQLite database (zero blank screens).
    /// 2. Queries remote cloud REST API if Internet connectivity is present.
    /// 3. Synchronizes updated items back to local SQLite cache.
    /// </summary>
    public async Task LoadFeedAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        IsRefreshing = true;

        try
        {
            // Tier 1: Instant local SQLite cache hydration
            var cached = await _localDb.GetCachedWishesAsync();
            _allLoadedWishes = cached;
            ApplyCurrentFilter();

            // Tier 2: Remote REST synchronization if network is available
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                IsOffline = false;
                var remoteItems = (await _restService.GetTrendingWishesAsync()).ToList();
                if (remoteItems.Any())
                {
                    await _localDb.CacheWishesBatchAsync(remoteItems);
                    _allLoadedWishes = await _localDb.GetCachedWishesAsync();
                    ApplyCurrentFilter();
                }
            }
            else
            {
                IsOffline = true;
            }
        }
        catch (Exception)
        {
            IsOffline = true;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// In-memory LINQ filtering and sorting applied against cached items.
    /// </summary>
    private void ApplyCurrentFilter()
    {
        IEnumerable<WishItem> query = _allLoadedWishes;

        // Apply text search across title, category, existing app, or problem statement
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim().ToLowerInvariant();
            query = query.Where(w =>
                w.Title.ToLowerInvariant().Contains(term) ||
                w.Category.ToLowerInvariant().Contains(term) ||
                w.ProblemStatement.ToLowerInvariant().Contains(term) ||
                w.ExistingAppName.ToLowerInvariant().Contains(term));
        }

        // Apply tab sorting criteria
        query = ActiveFilter switch
        {
            "Top" => query.OrderByDescending(w => w.UpvoteCount),
            "Recent" => query.OrderByDescending(w => w.CreatedAt),
            "Claimed" => query.Where(w => w.LifecycleStatus != "OpenIdea").OrderByDescending(w => w.UpvoteCount),
            _ => query.OrderByDescending(w => w.UpvoteCount) // Default 'Trending'
        };

        Wishes.Clear();
        foreach (var item in query)
        {
            Wishes.Add(item);
        }
    }

    /// <summary>
    /// Executes optimistic UI upvote:
    /// Increments tally immediately in memory, persists to SQLite, and queues async REST POST.
    /// </summary>
    private async Task ExecuteUpvoteAsync(WishItem? item)
    {
        if (item is null)
            return;

        if (item.HasUserUpvoted)
        {
            // Toggle off upvote
            item.UpvoteCount = Math.Max(0, item.UpvoteCount - 1);
            item.HasUserUpvoted = false;
        }
        else
        {
            // Optimistic UI increment
            item.UpvoteCount++;
            item.HasUserUpvoted = true;
        }

        // Re-notify CollectionView of item state change
        int index = Wishes.IndexOf(item);
        if (index >= 0)
        {
            Wishes[index] = item;
        }

        // Persist to local SQLite storage
        await _localDb.SaveWishAsync(item);

        // Synchronize with remote cloud REST service if connected
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await _restService.PostUpvoteAsync(item.RemoteId);
        }
    }
}
