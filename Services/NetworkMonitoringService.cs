using Prototype_Final_Project.Models;

namespace Prototype_Final_Project.Services;

/// <summary>
/// Hardware network connectivity observer utilizing Microsoft.Maui.Networking.Connectivity.
/// Implements reactive offline caching fallback and background synchronization upon reconnection.
/// Architectural Tier: Network Resilience Tier (Section 9 of Architecture Specification).
/// </summary>
public class NetworkMonitoringService
{
    private readonly RestDataService _restService;
    private readonly LocalDatabaseService _localDb;

    /// <summary>
    /// Event triggered whenever the physical network status changes between connected and disconnected.
    /// Passes a boolean indicating whether active Internet access is present.
    /// </summary>
    public event Action<bool>? NetworkStatusChanged;

    /// <summary>
    /// Injects required data services and registers the event listener for hardware connectivity state shifts.
    /// </summary>
    public NetworkMonitoringService(RestDataService restService, LocalDatabaseService localDb)
    {
        _restService = restService;
        _localDb = localDb;

        // Subscribe to dynamic hardware network notifications
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    /// <summary>
    /// Asynchronous callback invoked when physical network connectivity changes.
    /// Automatically updates the UI offline banner and synchronizes cached data when online.
    /// </summary>
    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        bool hasInternet = e.NetworkAccess == NetworkAccess.Internet;
        NetworkStatusChanged?.Invoke(hasInternet);

        if (hasInternet)
        {
            try
            {
                // Auto-synchronize latest feed from cloud REST endpoint upon reconnection
                var latestWishes = await _restService.GetTrendingWishesAsync();
                var wishesList = latestWishes.ToList();
                if (wishesList.Count > 0)
                {
                    await _localDb.CacheWishesBatchAsync(wishesList);
                }
            }
            catch
            {
                // Suppress transient background sync failures
            }
        }
    }
}
