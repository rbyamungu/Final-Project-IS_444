using SQLite;
using Prototype_Final_Project.Models;

namespace Prototype_Final_Project.Services;

/// <summary>
/// Encapsulates local SQLite persistence using sqlite-net-pcl with in-memory prototype fallback.
/// Handles offline caching, CRUD operations, and initial seed data for the prototype.
/// Architectural Tier: Local Storage Tier (Section 3 of Architecture Specification).
/// </summary>
public class LocalDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _databasePath;
    private bool _useInMemoryFallback;
    private readonly List<WishItem> _inMemoryWishes = new();
    private readonly List<CommentItem> _inMemoryComments = new();

    /// <summary>
    /// Initializes SQLite native platform batteries and establishes the local file path.
    /// </summary>
    public LocalDatabaseService()
    {
        try
        {
            // Initialize SQLite native provider for Windows and cross-platform runtimes
            SQLitePCL.Batteries_V2.Init();
        }
        catch
        {
            // Native initialization fallback
        }

        try
        {
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "app_wishlist_local.db3");
        }
        catch
        {
            _databasePath = "app_wishlist_local.db3";
        }
    }

    /// <summary>
    /// Lazily establishes the asynchronous SQLite connection and verifies required tables exist.
    /// Falls back to resilient in-memory storage if physical database initialization encounters permission constraints.
    /// </summary>
    public async Task InitAsync()
    {
        if (_database is not null || _useInMemoryFallback)
            return;

        try
        {
            _database = new SQLiteAsyncConnection(_databasePath);
            
            // Create tables if they do not already exist
            await _database.CreateTableAsync<WishItem>();
            await _database.CreateTableAsync<VoteRecord>();
            await _database.CreateTableAsync<CommentItem>();

            // Ensure database has prototype demo data on first launch
            await SeedInitialDataIfEmptyAsync();
        }
        catch
        {
            // Resilient fallback for prototype testing environments
            _useInMemoryFallback = true;
            _database = null;
            SeedInMemoryData();
        }
    }

    /// <summary>
    /// Retrieves all cached wishes ordered by community upvote count (descending).
    /// </summary>
    public async Task<List<WishItem>> GetCachedWishesAsync()
    {
        await InitAsync();
        if (_useInMemoryFallback || _database is null)
        {
            return _inMemoryWishes.OrderByDescending(w => w.UpvoteCount).ToList();
        }

        try
        {
            return await _database.Table<WishItem>()
                                  .OrderByDescending(w => w.UpvoteCount)
                                  .ToListAsync();
        }
        catch
        {
            return _inMemoryWishes.OrderByDescending(w => w.UpvoteCount).ToList();
        }
    }

    /// <summary>
    /// Retrieves a single wish by its unique remote identifier string.
    /// </summary>
    public async Task<WishItem?> GetWishByRemoteIdAsync(string remoteId)
    {
        await InitAsync();
        if (_useInMemoryFallback || _database is null)
        {
            return _inMemoryWishes.FirstOrDefault(w => w.RemoteId == remoteId);
        }

        try
        {
            return await _database.Table<WishItem>()
                                  .FirstOrDefaultAsync(w => w.RemoteId == remoteId);
        }
        catch
        {
            return _inMemoryWishes.FirstOrDefault(w => w.RemoteId == remoteId);
        }
    }

    /// <summary>
    /// Inserts or updates a wish record in local storage.
    /// </summary>
    public async Task SaveWishAsync(WishItem item)
    {
        await InitAsync();
        if (_useInMemoryFallback || _database is null)
        {
            var existing = _inMemoryWishes.FirstOrDefault(w => w.RemoteId == item.RemoteId);
            if (existing != null)
            {
                int index = _inMemoryWishes.IndexOf(existing);
                _inMemoryWishes[index] = item;
            }
            else
            {
                if (item.LocalId == 0) item.LocalId = _inMemoryWishes.Count + 1;
                _inMemoryWishes.Add(item);
            }
            return;
        }

        try
        {
            if (item.LocalId != 0)
            {
                await _database.UpdateAsync(item);
            }
            else
            {
                var existing = await GetWishByRemoteIdAsync(item.RemoteId);
                if (existing != null)
                {
                    item.LocalId = existing.LocalId;
                    await _database.UpdateAsync(item);
                }
                else
                {
                    await _database.InsertAsync(item);
                }
            }
        }
        catch
        {
            // Fallback to in-memory on database write failure
            SaveWishInMemory(item);
        }
    }

    private void SaveWishInMemory(WishItem item)
    {
        var existing = _inMemoryWishes.FirstOrDefault(w => w.RemoteId == item.RemoteId);
        if (existing != null)
        {
            int index = _inMemoryWishes.IndexOf(existing);
            _inMemoryWishes[index] = item;
        }
        else
        {
            _inMemoryWishes.Add(item);
        }
    }

    /// <summary>
    /// Deletes a wish by its remote ID.
    /// </summary>
    public async Task DeleteWishAsync(string remoteId)
    {
        await InitAsync();
        if (_useInMemoryFallback || _database is null)
        {
            var item = _inMemoryWishes.FirstOrDefault(w => w.RemoteId == remoteId);
            if (item != null) _inMemoryWishes.Remove(item);
            return;
        }

        try
        {
            var item = await GetWishByRemoteIdAsync(remoteId);
            if (item != null)
            {
                await _database.DeleteAsync(item);
            }
        }
        catch
        {
            var item = _inMemoryWishes.FirstOrDefault(w => w.RemoteId == remoteId);
            if (item != null) _inMemoryWishes.Remove(item);
        }
    }

    /// <summary>
    /// Batch inserts or updates a collection of remote wish records into the local cache.
    /// </summary>
    public async Task CacheWishesBatchAsync(IEnumerable<WishItem> items)
    {
        await InitAsync();
        foreach (var item in items)
        {
            await SaveWishAsync(item);
        }
    }

    /// <summary>
    /// Retrieves community discussion comments associated with a specific wish.
    /// </summary>
    public async Task<List<CommentItem>> GetCommentsForWishAsync(string wishRemoteId)
    {
        await InitAsync();
        if (_useInMemoryFallback || _database is null)
        {
            return _inMemoryComments.Where(c => c.WishRemoteId == wishRemoteId).OrderBy(c => c.CreatedAt).ToList();
        }

        try
        {
            return await _database.Table<CommentItem>()
                                   .Where(c => c.WishRemoteId == wishRemoteId)
                                   .OrderBy(c => c.CreatedAt)
                                   .ToListAsync();
        }
        catch
        {
            return _inMemoryComments.Where(c => c.WishRemoteId == wishRemoteId).OrderBy(c => c.CreatedAt).ToList();
        }
    }

    /// <summary>
    /// Persists a new user or developer comment to local storage.
    /// </summary>
    public async Task SaveCommentAsync(CommentItem comment)
    {
        await InitAsync();
        if (_useInMemoryFallback || _database is null)
        {
            _inMemoryComments.Add(comment);
            return;
        }

        try
        {
            if (comment.LocalId != 0)
            {
                await _database.UpdateAsync(comment);
            }
            else
            {
                await _database.InsertAsync(comment);
            }
        }
        catch
        {
            _inMemoryComments.Add(comment);
        }
    }

    private void SeedInMemoryData()
    {
        if (_inMemoryWishes.Count > 0) return;
        _inMemoryWishes.AddRange(GetSampleWishes());
        _inMemoryComments.AddRange(GetSampleComments());
    }

    private async Task SeedInitialDataIfEmptyAsync()
    {
        var count = await _database!.Table<WishItem>().CountAsync();
        if (count > 0)
            return;

        var sampleWishes = GetSampleWishes();
        await _database.InsertAllAsync(sampleWishes);

        var sampleComments = GetSampleComments();
        await _database.InsertAllAsync(sampleComments);
    }

    private static List<WishItem> GetSampleWishes() => new()
    {
        new WishItem
        {
            RemoteId = "WISH-1001",
            Title = "Offline Audio Transcriber & Note Summarizer",
            SubmissionType = "NewAppConcept",
            TargetPlatform = "Cross-Platform",
            Category = "Productivity",
            ProblemStatement = "Current voice memo apps require internet for AI transcription. Need an on-device whisper-based transcription tool that creates bulleted meeting summaries without cloud uploads.",
            TargetPersona = "Students, Journalists, and Mobile Professionals",
            UpvoteCount = 142,
            HasUserUpvoted = false,
            LifecycleStatus = "UnderReview",
            ClaimedByDeveloper = "",
            TargetReleaseQuarter = "Q4 2026",
            ExternalUrl = "",
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        },
        new WishItem
        {
            RemoteId = "WISH-1002",
            Title = "Dynamic Spotify Folder-Based Playlist Organizer",
            SubmissionType = "FeatureRequest",
            ExistingAppName = "Spotify",
            TargetPlatform = "Cross-Platform",
            Category = "Entertainment",
            ProblemStatement = "Spotify mobile lacks hierarchical nested folders for playlists that the desktop app has, making library management chaotic with 100+ playlists.",
            TargetPersona = "Music Enthusiasts and DJs",
            UpvoteCount = 215,
            HasUserUpvoted = true,
            LifecycleStatus = "InDevelopment",
            ClaimedByDeveloper = "NovaLabs Mobile Studio",
            TargetReleaseQuarter = "Q3 2026",
            ExternalUrl = "https://github.com/novalabs/spoti-organizer",
            CreatedAt = DateTime.UtcNow.AddDays(-12)
        },
        new WishItem
        {
            RemoteId = "WISH-1003",
            Title = "Multi-Repo Git PR Review & Merge Stack App",
            SubmissionType = "NewAppConcept",
            TargetPlatform = "Cross-Platform",
            Category = "Developer Tools",
            ProblemStatement = "Mobile GitHub app does not handle stacked pull requests or cross-repo atomic reviews cleanly. Need an expedited thumb-friendly diff & approval workflow.",
            TargetPersona = "Lead Software Engineers & Open Source Maintainers",
            UpvoteCount = 310,
            HasUserUpvoted = false,
            LifecycleStatus = "Shipped",
            ClaimedByDeveloper = "Alex Rivera (StackFlow Systems)",
            TargetReleaseQuarter = "Q1 2026",
            ExternalUrl = "https://stackflow-git.dev",
            CreatedAt = DateTime.UtcNow.AddDays(-28)
        },
        new WishItem
        {
            RemoteId = "WISH-1004",
            Title = "Smart Home NFC Automation Trigger Tiles",
            SubmissionType = "NewAppConcept",
            TargetPlatform = "Android",
            Category = "Utilities",
            ProblemStatement = "Home Assistant requires complex scripts for single-tap physical actions. An app with instant NFC tag writing to launch chained lighting and thermostat routines is missing.",
            TargetPersona = "Smart Home Enthusiasts",
            UpvoteCount = 89,
            HasUserUpvoted = false,
            LifecycleStatus = "OpenIdea",
            ClaimedByDeveloper = "",
            TargetReleaseQuarter = "",
            ExternalUrl = "",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        },
        new WishItem
        {
            RemoteId = "WISH-1005",
            Title = "Slack/Teams Unified Notification Sieve",
            SubmissionType = "FeatureRequest",
            ExistingAppName = "Slack & MS Teams",
            TargetPlatform = "Cross-Platform",
            Category = "Productivity",
            ProblemStatement = "Working across multiple contractor Slack and Teams workspaces causes extreme notification fatigue. Need a meta-inbox app prioritizing @mentions and urgent keywords.",
            TargetPersona = "Remote Contractors & Engineering Consultants",
            UpvoteCount = 178,
            HasUserUpvoted = false,
            LifecycleStatus = "InDevelopment",
            ClaimedByDeveloper = "Apex Dev Group",
            TargetReleaseQuarter = "Q4 2026",
            ExternalUrl = "https://github.com/apex-dev/teams-sieve",
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        },
        new WishItem
        {
            RemoteId = "WISH-1006",
            Title = "Receipt OCR Splitter for Group Dining",
            SubmissionType = "NewAppConcept",
            TargetPlatform = "Cross-Platform",
            Category = "Finance",
            ProblemStatement = "Splitting complex restaurant bills with shared appetizers and itemized taxes takes 15 minutes of manual calculation. Need instant camera scan with drag-to-person assignment.",
            TargetPersona = "College Students and Roommates",
            UpvoteCount = 124,
            HasUserUpvoted = false,
            LifecycleStatus = "OpenIdea",
            ClaimedByDeveloper = "",
            TargetReleaseQuarter = "",
            ExternalUrl = "",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        },
        new WishItem
        {
            RemoteId = "WISH-1007",
            Title = "Campus Meal Plan Budget & Nutrition Tracker",
            SubmissionType = "NewAppConcept",
            TargetPlatform = "iOS",
            Category = "Health & Fitness",
            ProblemStatement = "University dining halls publish menus on clunky web portals. An app mapping daily calorie goals and remaining declining balance points would keep students on budget and healthy.",
            TargetPersona = "Undergraduate Campus Students",
            UpvoteCount = 67,
            HasUserUpvoted = false,
            LifecycleStatus = "OpenIdea",
            ClaimedByDeveloper = "",
            TargetReleaseQuarter = "",
            ExternalUrl = "",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        },
        new WishItem
        {
            RemoteId = "WISH-1008",
            Title = "Transit Arrival Live Activity Lockscreen Widget",
            SubmissionType = "FeatureRequest",
            ExistingAppName = "Transit App",
            TargetPlatform = "iOS",
            Category = "Travel",
            ProblemStatement = "Commuters shouldn't have to keep unlocking their phone in freezing weather to check bus ETA. We need persistent iOS Dynamic Island and Live Activity countdowns.",
            TargetPersona = "Daily Urban Transit Commuters",
            UpvoteCount = 95,
            HasUserUpvoted = false,
            LifecycleStatus = "UnderReview",
            ClaimedByDeveloper = "",
            TargetReleaseQuarter = "Q3 2026",
            ExternalUrl = "",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        }
    };

    private static List<CommentItem> GetSampleComments() => new()
    {
        new CommentItem
        {
            RemoteId = "COMM-201",
            WishRemoteId = "WISH-1001",
            AuthorName = "Elena Vance (Graduate Researcher)",
            AuthorRole = "Wisher",
            Content = "An on-device Whisper model like whisper.tflite could run locally on modern NPU chips without draining too much battery!",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        },
        new CommentItem
        {
            RemoteId = "COMM-202",
            WishRemoteId = "WISH-1001",
            AuthorName = "DevDan (Mobile AI Engineer)",
            AuthorRole = "Developer",
            Content = "I'm currently benchmarking quantized Whisper-base models on Android 14. We might be able to prototype this with MAUI and ONNX Runtime.",
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        },
        new CommentItem
        {
            RemoteId = "COMM-203",
            WishRemoteId = "WISH-1002",
            AuthorName = "NovaLabs Lead Dev",
            AuthorRole = "Developer",
            Content = "We have claimed this project! Our initial build uses the Spotify Web API with OAuth PKCE tokens. Alpha build coming in Q3.",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        },
        new CommentItem
        {
            RemoteId = "COMM-204",
            WishRemoteId = "WISH-1002",
            AuthorName = "Marcus K.",
            AuthorRole = "Community",
            Content = "Please make sure it supports batch moving multiple playlists into one folder at once!",
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        },
        new CommentItem
        {
            RemoteId = "COMM-205",
            WishRemoteId = "WISH-1003",
            AuthorName = "Alex Rivera",
            AuthorRole = "Developer",
            Content = "The v1.0 release is now live on the App Store! Direct link attached in the specs above. Thank you everyone for the upvotes and feedback.",
            CreatedAt = DateTime.UtcNow.AddDays(-15)
        }
    };
}
