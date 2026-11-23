using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Prototype_Final_Project.Models;

namespace Prototype_Final_Project.Services;

/// <summary>
/// Data Access Tier implementation utilizing System.Net.Http.HttpClient.
/// Manages asynchronous consumption of RESTful web services, JSON payload serialization,
/// and graceful fallback for offline prototype resilience (Section 8 of Architecture Specification).
/// </summary>
public class RestDataService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string BaseApiEndpoint = "api/v1/";

    /// <summary>
    /// Configures the shared HttpClient singleton with base URI, timeouts, and JSON headers.
    /// </summary>
    public RestDataService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5001/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Retrieves the highest-velocity wishes from the cloud REST endpoint.
    /// Corresponds to GET /api/v1/wishes/trending.
    /// </summary>
    public async Task<IEnumerable<WishItem>> GetTrendingWishesAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return Enumerable.Empty<WishItem>();

        try
        {
            string json = await _httpClient.GetStringAsync($"{BaseApiEndpoint}wishes/trending");
            return JsonSerializer.Deserialize<List<WishItem>>(json, _jsonOptions) 
                   ?? new List<WishItem>();
        }
        catch (Exception)
        {
            // Resilient fallback for prototype testing when remote web server is offline
            return Enumerable.Empty<WishItem>();
        }
    }

    /// <summary>
    /// Ingests a new dual-mode user wish submission to the remote cloud API.
    /// Corresponds to POST /api/v1/wishes.
    /// </summary>
    public async Task<bool> CreateWishAsync(WishItem newWish)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return false;

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseApiEndpoint}wishes", newWish, _jsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Registers a community upvote on a wish and triggers momentum recalculation.
    /// Corresponds to POST /api/v1/wishes/{id}/upvote.
    /// </summary>
    public async Task<bool> PostUpvoteAsync(string wishId)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return false;

        try
        {
            var response = await _httpClient.PostAsync($"{BaseApiEndpoint}wishes/{wishId}/upvote", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Updates the developer claim ownership and development status lifecycle badge.
    /// Corresponds to PUT /api/v1/wishes/{id}/status.
    /// </summary>
    public async Task<bool> UpdateDeveloperStatusAsync(string wishId, string newStatus, string releaseQuarter, string developerName = "")
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return false;

        try
        {
            var payload = new 
            { 
                Status = newStatus, 
                TargetQuarter = releaseQuarter,
                DeveloperName = developerName 
            };
            var message = new HttpRequestMessage(HttpMethod.Put, $"{BaseApiEndpoint}wishes/{wishId}/status")
            {
                Content = JsonContent.Create(payload, options: _jsonOptions)
            };

            var response = await _httpClient.SendAsync(message);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Retrieves community discussion comments for a wish from the remote API.
    /// Corresponds to GET /api/v1/wishes/{id}/comments.
    /// </summary>
    public async Task<IEnumerable<CommentItem>> GetCommentsAsync(string wishId)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return Enumerable.Empty<CommentItem>();

        try
        {
            string json = await _httpClient.GetStringAsync($"{BaseApiEndpoint}wishes/{wishId}/comments");
            return JsonSerializer.Deserialize<List<CommentItem>>(json, _jsonOptions) ?? new List<CommentItem>();
        }
        catch
        {
            return Enumerable.Empty<CommentItem>();
        }
    }

    /// <summary>
    /// Posts a new user or developer discussion comment to the remote API.
    /// Corresponds to POST /api/v1/wishes/{id}/comments.
    /// </summary>
    public async Task<bool> PostCommentAsync(CommentItem comment)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return false;

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseApiEndpoint}wishes/{comment.WishRemoteId}/comments", comment, _jsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
