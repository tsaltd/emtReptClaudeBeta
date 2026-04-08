using System.Collections.Concurrent;
using System.Diagnostics;
using EmailTracker.Data;
using EmailTracker.Models;
using EmailTracker.Repositories.Interfaces;
using EmailTracker.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace EmailTracker.Services;

// ── Interfaces ───────────────────────────────────────────────────

public interface IRunService
{
    Task<RunListViewModel>   GetRunListAsync(string? searchTerm);
    Task<RunDetailViewModel?> GetRunDetailAsync(int runId);
}

public interface IMessageService
{
    Task<MessageSearchViewModel> SearchAsync(MessageSearchViewModel filters);

    /// <summary>Lightweight fetch: messages for one sender, no dropdown data loaded.</summary>
    Task<IEnumerable<MessageRowViewModel>> GetForSenderAsync(int senderId, int limit = 200);

    /// <summary>Fetch messages by a set of gmail message IDs (for priority filter).</summary>
    Task<IEnumerable<MessageRowViewModel>> GetByGmailIdsAsync(IEnumerable<string> gmailIds);
}

public interface ISenderService
{
    Task<SenderSearchViewModel>  SearchAsync(SenderSearchViewModel filters);
    Task<SenderDetailViewModel?>  GetDetailAsync(int senderId);
    Task<SenderSummaryPageViewModel?> GetSummaryAsync(int senderId, int page = 1, int pageSize = 10, bool sortAsc = true);

    /// <summary>
    /// Extracts the canonical email address from a raw From header.
    /// e.g. "Some Disguised Name [Org]" &lt;admin@e.example.org&gt;  → admin@e.example.org
    /// </summary>
    /// <summary>Browse single-record view at position index (sorted by msg_count DESC).</summary>
    Task<SenderBrowseViewModel?> GetBrowseAsync(int index);

    /// <summary>Update a sender's rating. Returns updated rating name or null on failure.</summary>
    Task<string?> UpdateRatingAsync(int senderId, int ratingId);

    /// <summary>Bulk update rating for multiple senders. Returns rating name or null on failure.</summary>
    Task<string?> UpdateRatingBulkAsync(IEnumerable<int> senderIds, int ratingId);

    string ExtractCanonicalEmail(string fromRaw);
}

public interface IRatingService
{
    Task<IEnumerable<RatingOptionViewModel>> GetAllAsync();
}

// ── Implementations ──────────────────────────────────────────────

public class RunService : IRunService
{
    private readonly IRunRepository     _runRepo;
    private readonly IMessageRepository _msgRepo;
    private readonly ISenderRepository  _senderRepo;

    public RunService(IRunRepository runRepo, IMessageRepository msgRepo, ISenderRepository senderRepo)
    {
        _runRepo    = runRepo;
        _msgRepo    = msgRepo;
        _senderRepo = senderRepo;
    }

    public async Task<RunListViewModel> GetRunListAsync(string? searchTerm)
    {
        var runs = await _runRepo.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            runs = runs.Where(r =>
                (r.SourceLabel != null && r.SourceLabel.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

        var summaries = new List<RunSummaryViewModel>();
        foreach (var r in runs)
        {
            summaries.Add(new RunSummaryViewModel
            {
                RunId        = r.RunId,
                WindowStart  = r.WindowStart,
                WindowEnd    = r.WindowEnd,
                StartedAt    = r.StartedAt,
                SourceLabel  = r.SourceLabel,
                MessageCount = await _runRepo.GetMessageCountAsync(r.RunId),
                SenderCount  = await _runRepo.GetSenderCountAsync(r.RunId)
            });
        }

        return new RunListViewModel
        {
            Runs       = summaries,
            SearchTerm = searchTerm,
            TotalRuns  = summaries.Count
        };
    }

    public async Task<RunDetailViewModel?> GetRunDetailAsync(int runId)
    {
        var run = await _runRepo.GetByIdAsync(runId);
        if (run == null) return null;

        var topSenders = await _senderRepo.GetTopSendersForRunAsync(runId);

        return new RunDetailViewModel
        {
            RunId        = run.RunId,
            WindowStart  = run.WindowStart,
            WindowEnd    = run.WindowEnd,
            StartedAt    = run.StartedAt,
            SourceLabel  = run.SourceLabel,
            MessageCount = await _runRepo.GetMessageCountAsync(runId),
            SenderCount  = await _runRepo.GetSenderCountAsync(runId),
            TopSenders   = topSenders.Select(s => new SenderSummaryViewModel
            {
                SenderId     = s.SenderId,
                EmailAddress = s.EmailAddress,
                DisplayName  = s.DisplayName,
                MsgCount     = s.MsgCount,
                RatingName   = s.RatingName,
                ColorCode    = s.ColorCode,
                RatingId     = s.RatingId
            })
        };
    }
}

public class MessageService : IMessageService
{
    private readonly IMessageRepository _msgRepo;
    private readonly IRatingRepository  _ratingRepo;

    public MessageService(IMessageRepository msgRepo, IRatingRepository ratingRepo)
    {
        _msgRepo    = msgRepo;
        _ratingRepo = ratingRepo;
    }

    public async Task<MessageSearchViewModel> SearchAsync(MessageSearchViewModel filters)
    {
        var messages = await _msgRepo.SearchAsync(
            filters.SenderId, filters.SearchTerm,
            filters.DateFrom, filters.DateTo, filters.RatingFilter,
            filters.Page, filters.PageSize);

        var total = await _msgRepo.CountAsync(
            filters.SenderId, filters.SearchTerm,
            filters.DateFrom, filters.DateTo, filters.RatingFilter);

        var ratings = await _ratingRepo.GetAllAsync();

        filters.Messages = messages.Select(m => new MessageRowViewModel
        {
            MessageId      = m.MessageId,
            RunId          = m.RunId,
            SenderId       = m.SenderId,
            EmailAddress   = m.EmailAddress,
            RatingName     = m.RatingName,
            Subject        = m.Subject,
            Snippet        = m.Snippet,
            InternalDate   = m.InternalDate,
            FromRaw        = m.FromRaw,
            GmailMessageId = m.GmailMessageId,
            ThreadId       = m.ThreadId
        });

        filters.TotalCount = total;
        filters.AvailableRatings = ratings.Select(r => new RatingOptionViewModel
        {
            RatingId   = r.RatingId,
            RatingName = r.RatingName,
            SortOrder  = r.SortOrder,
            ColorCode  = r.ColorCode
        });

        return filters;
    }

    public async Task<IEnumerable<MessageRowViewModel>> GetForSenderAsync(int senderId, int limit = 200)
    {
        var msgs = await _msgRepo.SearchAsync(
            senderId, null, null, null, null, 1, limit);

        return msgs.Select(m => new MessageRowViewModel
        {
            MessageId      = m.MessageId,
            RunId          = m.RunId,
            SenderId       = m.SenderId,
            EmailAddress   = m.EmailAddress,
            RatingName     = m.RatingName,
            Subject        = m.Subject,
            Snippet        = m.Snippet,
            InternalDate   = m.InternalDate,
            FromRaw        = m.FromRaw,
            GmailMessageId = m.GmailMessageId
        });
    }

    public async Task<IEnumerable<MessageRowViewModel>> GetByGmailIdsAsync(IEnumerable<string> gmailIds)
    {
        var msgs = await _msgRepo.GetByGmailIdsAsync(gmailIds);
        return msgs.Select(m => new MessageRowViewModel
        {
            MessageId      = m.MessageId,
            RunId          = m.RunId,
            SenderId       = m.SenderId,
            EmailAddress   = m.EmailAddress,
            RatingName     = m.RatingName,
            Subject        = m.Subject,
            Snippet        = m.Snippet,
            InternalDate   = m.InternalDate,
            FromRaw        = m.FromRaw,
            GmailMessageId = m.GmailMessageId,
            IsPriority     = true
        });
    }
}

public class SenderService : ISenderService
{
    private readonly ISenderRepository  _senderRepo;
    private readonly IMessageRepository _msgRepo;
    private readonly IRatingRepository  _ratingRepo;

    public SenderService(ISenderRepository senderRepo, IMessageRepository msgRepo, IRatingRepository ratingRepo)
    {
        _senderRepo = senderRepo;
        _msgRepo    = msgRepo;
        _ratingRepo = ratingRepo;
    }

    public async Task<SenderSearchViewModel> SearchAsync(SenderSearchViewModel filters)
    {
        var senders = await _senderRepo.SearchAsync(
            filters.SearchTerm, filters.RatingFilter, filters.Page, filters.PageSize, filters.SortAsc);

        var total   = await _senderRepo.CountAsync(filters.SearchTerm, filters.RatingFilter);
        var ratings = await _ratingRepo.GetAllAsync();

        filters.Senders = senders.Select(s => new SenderSummaryViewModel
        {
            SenderId     = s.SenderId,
            EmailAddress = s.EmailAddress,
            DisplayName  = s.DisplayName,
            MsgCount     = s.MsgCount,
            RatingName   = s.RatingName,
            ColorCode    = s.ColorCode,
            RatingId     = s.RatingId,
            FirstSeen    = s.FirstSeen,
            LastSeen     = s.LastSeen
        });

        filters.TotalCount       = total;
        filters.AvailableRatings = ratings.Select(r => new RatingOptionViewModel
        {
            RatingId   = r.RatingId,
            RatingName = r.RatingName,
            SortOrder  = r.SortOrder,
            ColorCode  = r.ColorCode
        });

        return filters;
    }

    public async Task<SenderDetailViewModel?> GetDetailAsync(int senderId)
    {
        var sender = await _senderRepo.GetByIdAsync(senderId);
        if (sender == null) return null;

        var allMessages = await _msgRepo.SearchAsync(
            senderId, null, null, null, null, 1, int.MaxValue);

        var ratings = await _ratingRepo.GetAllAsync();

        var fromRawGroups = allMessages
            .GroupBy(m => m.FromRaw ?? "(no from header)")
            .OrderBy(g => g.Count())
            .Select(g => new FromRawGroupViewModel
            {
                FromRaw  = g.Key,
                Messages = g.OrderByDescending(m => m.InternalDate).Select(m => new MessageRowViewModel
                {
                    MessageId    = m.MessageId,
                    RunId        = m.RunId,
                    SenderId     = m.SenderId,
                    EmailAddress = m.EmailAddress,
                    RatingName   = m.RatingName,
                    Subject      = m.Subject,
                    Snippet      = m.Snippet,
                    InternalDate = m.InternalDate,
                    FromRaw      = m.FromRaw
                }).ToList()
            });

        return new SenderDetailViewModel
        {
            SenderId         = sender.SenderId,
            EmailAddress     = sender.EmailAddress,
            DisplayName      = sender.DisplayName,
            MsgCount         = sender.MsgCount,
            RatingId         = sender.RatingId,
            RatingName       = sender.Rating?.RatingName ?? string.Empty,
            FirstSeen        = sender.FirstSeen,
            LastSeen         = sender.LastSeen,
            CreatedAt        = sender.CreatedAt,
            UpdatedAt        = sender.UpdatedAt,
            FromRawGroups    = fromRawGroups,
            AvailableRatings = ratings.Select(r => new RatingOptionViewModel
            {
                RatingId   = r.RatingId,
                RatingName = r.RatingName,
                SortOrder  = r.SortOrder
            })
        };
    }

    public async Task<SenderSummaryPageViewModel?> GetSummaryAsync(int senderId, int page = 1, int pageSize = 10, bool sortAsc = true)
    {
        var sender = await _senderRepo.GetByIdAsync(senderId);
        if (sender == null) return null;

        var allMessages = await _msgRepo.SearchAsync(
            senderId, null, null, null, null, 1, int.MaxValue);

        var ratings = await _ratingRepo.GetAllAsync();

        var allGroups = allMessages
            .GroupBy(m => m.FromRaw ?? "(no from header)")
            .OrderBy(g => sortAsc ? g.Count() : -g.Count())
            .ToList();

        int totalGroups = allGroups.Count;

        var pagedGroups = allGroups
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new FromRawGroupViewModel
            {
                FromRaw  = g.Key,
                Messages = g.OrderByDescending(m => m.InternalDate).Select(m => new MessageRowViewModel
                {
                    MessageId    = m.MessageId,
                    RunId        = m.RunId,
                    SenderId     = m.SenderId,
                    EmailAddress = m.EmailAddress,
                    RatingName   = m.RatingName,
                    Subject      = m.Subject,
                    Snippet      = m.Snippet,
                    InternalDate = m.InternalDate,
                    FromRaw      = m.FromRaw
                }).ToList()
            });

        return new SenderSummaryPageViewModel
        {
            SenderId         = sender.SenderId,
            EmailAddress     = sender.EmailAddress,
            DisplayName      = sender.DisplayName,
            MsgCount         = sender.MsgCount,
            RatingId         = sender.RatingId,
            RatingName       = sender.Rating?.RatingName ?? string.Empty,
            ColorCode        = sender.Rating?.ColorCode,
            FirstSeen        = sender.FirstSeen,
            LastSeen         = sender.LastSeen,
            FromRawGroups    = pagedGroups,
            Page             = page,
            PageSize         = pageSize,
            TotalGroups      = totalGroups,
            SortAsc          = sortAsc,
            AvailableRatings = ratings.Select(r => new RatingOptionViewModel
            {
                RatingId   = r.RatingId,
                RatingName = r.RatingName,
                SortOrder  = r.SortOrder,
                ColorCode  = r.ColorCode
            })
        };
    }

    /// <summary>
    /// Extracts canonical email from raw From header.
    /// Handles formats like:
    ///   "Display Name [Org]" &lt;user@domain.com&gt;
    ///   user@domain.com
    ///   Display Name &lt;user@domain.com&gt;
    /// </summary>
    public async Task<SenderBrowseViewModel?> GetBrowseAsync(int index)
    {
        // Pull full ordered list (msg_count DESC) — lightweight: only ids needed
        var all = await _senderRepo.SearchAsync(null, null, 1, int.MaxValue, false);
        var list = all.ToList();

        int total = list.Count;
        if (total == 0 || index < 0 || index >= total) return null;

        var s       = list[index];
        var ratings = await _ratingRepo.GetAllAsync();

        return new SenderBrowseViewModel
        {
            SenderId         = s.SenderId,
            EmailAddress     = s.EmailAddress,
            DisplayName      = s.DisplayName,
            MsgCount         = s.MsgCount,
            RatingId         = s.RatingId,
            RatingName       = s.RatingName,
            FirstSeen        = s.FirstSeen,
            LastSeen         = s.LastSeen,
            CurrentIndex     = index,
            TotalSenders     = total,
            AvailableRatings = ratings.Select(r => new RatingOptionViewModel
            {
                RatingId   = r.RatingId,
                RatingName = r.RatingName,
                SortOrder  = r.SortOrder
            })
        };
    }

    public async Task<string?> UpdateRatingBulkAsync(IEnumerable<int> senderIds, int ratingId)
    {
        var rating = await _ratingRepo.GetByIdAsync(ratingId);
        if (rating == null) return null;
        await _senderRepo.UpdateRatingBulkAsync(senderIds, ratingId);
        return rating.RatingName;
    }

    public async Task<string?> UpdateRatingAsync(int senderId, int ratingId)
    {
        var sender = await _senderRepo.GetByIdAsync(senderId);
        if (sender == null) return null;

        var rating = await _ratingRepo.GetByIdAsync(ratingId);
        if (rating == null) return null;

        sender.RatingId = ratingId;
        await _senderRepo.UpdateAsync(sender);
        return rating.RatingName;
    }

    public string ExtractCanonicalEmail(string fromRaw)
    {
        if (string.IsNullOrWhiteSpace(fromRaw))
            return fromRaw;

        // Try to extract from angle brackets first: <email@domain.com>
        var start = fromRaw.LastIndexOf('<');
        var end   = fromRaw.LastIndexOf('>');
        if (start >= 0 && end > start)
        {
            var extracted = fromRaw.Substring(start + 1, end - start - 1).Trim();
            if (extracted.Contains('@'))
                return extracted.ToLowerInvariant();
        }

        // Fallback: treat the whole string as an email if it contains @
        var trimmed = fromRaw.Trim();
        if (trimmed.Contains('@'))
            return trimmed.ToLowerInvariant();

        return fromRaw;
    }
}

public interface IPriorityMessageService
{
    /// <summary>Toggle priority state. Returns true if now priority, false if removed.</summary>
    Task<bool>        ToggleAsync(string gmailMessageId);
    Task<HashSet<string>> GetAllIdsAsync();
}

public class PriorityMessageService : IPriorityMessageService
{
    private readonly IPriorityMessageRepository _repo;
    public PriorityMessageService(IPriorityMessageRepository repo) => _repo = repo;

    public async Task<bool> ToggleAsync(string gmailMessageId)
    {
        if (await _repo.IsTrackedAsync(gmailMessageId))
        {
            await _repo.RemoveAsync(gmailMessageId);
            return false;
        }
        await _repo.AddAsync(gmailMessageId);
        return true;
    }

    public Task<HashSet<string>> GetAllIdsAsync() => _repo.GetAllIdsAsync();
}

public class RatingService : IRatingService
{
    private readonly IRatingRepository _ratingRepo;
    public RatingService(IRatingRepository ratingRepo) => _ratingRepo = ratingRepo;

    public async Task<IEnumerable<RatingOptionViewModel>> GetAllAsync()
    {
        var ratings = await _ratingRepo.GetAllAsync();
        return ratings.Select(r => new RatingOptionViewModel
        {
            RatingId   = r.RatingId,
            RatingName = r.RatingName,
            SortOrder  = r.SortOrder,
            ColorCode  = r.ColorCode
        });
    }
}

// ── Gmail Ingest ──────────────────────────────────────────────────

/// <summary>Tracks the lifecycle of a single ingest job.</summary>
public class IngestJob
{
    public string  JobId     { get; set; } = string.Empty;
    public string  Status    { get; set; } = "queued";   // queued | running | complete | failed
    public string? Step      { get; set; }               // current step label for UI progress
    public string? Error     { get; set; }
    public IngestResultViewModel? Result { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Singleton store — survives across HTTP requests.</summary>
public class IngestJobStore
{
    private readonly ConcurrentDictionary<string, IngestJob> _jobs = new();

    public IngestJob Create()
    {
        var job = new IngestJob { JobId = Guid.NewGuid().ToString("N") };
        _jobs[job.JobId] = job;
        return job;
    }

    public IngestJob? Get(string? jobId) =>
        string.IsNullOrEmpty(jobId) ? null : _jobs.TryGetValue(jobId, out var job) ? job : null;
}

/// <summary>Top-level payload produced by gmail_headers_export.py</summary>
public class GmailPayload
{
    [JsonProperty("meta")]
    public GmailMeta Meta { get; set; } = new();

    [JsonProperty("messages")]
    public List<GmailHeaderRecord> Messages { get; set; } = [];
}

/// <summary>Run-level metadata from the harvester.</summary>
public class GmailMeta
{
    [JsonProperty("query")]
    public string Query { get; set; } = string.Empty;

    [JsonProperty("max_messages")]
    public int MaxMessages { get; set; }

    [JsonProperty("fetched_at_utc")]
    public string FetchedAtUtc { get; set; } = string.Empty;

    [JsonProperty("record_count")]
    public int RecordCount { get; set; }
}

/// <summary>Per-message record produced by gmail_headers_export.py</summary>
public class GmailHeaderRecord
{
    [JsonProperty("gmail_message_id")]
    public string GmailMessageId { get; set; } = string.Empty;

    [JsonProperty("thread_id")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonProperty("internal_date")]
    public string InternalDate { get; set; } = string.Empty;

    [JsonProperty("header_date")]
    public string HeaderDate { get; set; } = string.Empty;

    [JsonProperty("from_raw")]
    public string FromRaw { get; set; } = string.Empty;

    [JsonProperty("canonical_email")]
    public string CanonicalEmail { get; set; } = string.Empty;

    [JsonProperty("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonProperty("to_raw")]
    public string ToRaw { get; set; } = string.Empty;

    [JsonProperty("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonProperty("snippet")]
    public string Snippet { get; set; } = string.Empty;
}

public interface IGmailIngestService
{
    Task<IngestResultViewModel> RunAsync(int days = 14, Action<string>? progress = null);
}

public class GmailIngestService : IGmailIngestService
{
    private readonly AppDbContext   _db;
    private readonly IConfiguration _config;

    public GmailIngestService(AppDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    public async Task<IngestResultViewModel> RunAsync(int days = 14, Action<string>? progress = null)
    {
        days = Math.Clamp(days, 1, 14);

        var startedAt  = DateTime.UtcNow;
        var pythonExe  = _config["GmailIngest:PythonExe"]  ?? "python";
        var scriptPath = _config["GmailIngest:ScriptPath"] ?? throw new InvalidOperationException("GmailIngest:ScriptPath not configured");
        var exportsDir = _config["GmailIngest:ExportsDir"] ?? throw new InvalidOperationException("GmailIngest:ExportsDir not configured");
        var workingDir = _config["GmailIngest:WorkingDir"] ?? Path.GetDirectoryName(scriptPath)!;

        // Step 1: Wipe existing batch data — preserve starred messages older than batch window
        progress?.Invoke("Clearing previous batch (keeping older starred)…");
        var batchCutoff = DateTime.UtcNow.AddDays(-days).ToString("o");
        var starredIds = await _db.PriorityMessages
            .Select(p => p.GmailMessageId)
            .ToListAsync();
        var starredIdSet = starredIds.ToHashSet();

        await _db.Messages
            .Where(m =>
                // Delete if: not starred, OR starred but within batch window (will be re-imported)
                m.GmailMessageId == null
                || !starredIdSet.Contains(m.GmailMessageId)
                || (starredIdSet.Contains(m.GmailMessageId) && m.InternalDate != null && m.InternalDate.CompareTo(batchCutoff) >= 0))
            .ExecuteDeleteAsync();

        // Delete runs that no longer have any messages
        var runsWithMessages = _db.Messages.Select(m => m.RunId).Distinct();
        await _db.Runs
            .Where(r => !runsWithMessages.Contains(r.RunId))
            .ExecuteDeleteAsync();

        // Step 2: Run the Python harvester with the selected day range
        progress?.Invoke("Running Python script…");
        var scriptArgs = $"--query \"newer_than:{days}d\" --max 5000 --outdir \"{exportsDir}\"";
        await RunPythonScriptAsync(pythonExe, scriptPath, workingDir, scriptArgs);

        // Step 3: Read and deserialize the ETL contract JSON
        progress?.Invoke("Reading extracted data…");
        var jsonPath = Path.Combine(exportsDir, "gmail_headers.json");
        var json     = await File.ReadAllTextAsync(jsonPath);
        var payload  = JsonConvert.DeserializeObject<GmailPayload>(json)
                       ?? throw new InvalidOperationException("Failed to deserialize gmail_headers.json");

        var meta    = payload.Meta;
        var records = payload.Messages;

        // Step 4: Create run record — sourced from harvester metadata
        progress?.Invoke($"Creating run record ({records.Count} messages)…");
        var now = DateTime.UtcNow;
        var run = new Run
        {
            WindowStart = now.AddDays(-days).ToString("o"),
            WindowEnd   = meta.FetchedAtUtc,
            StartedAt   = startedAt.ToString("o"),
            SourceLabel = $"gmail_extract query={meta.Query} max={meta.MaxMessages}"
        };
        _db.Runs.Add(run);
        await _db.SaveChangesAsync();

        // Step 5: Upsert senders — preserve existing ratings
        // Count surviving starred messages per sender before resetting
        progress?.Invoke("Resetting sender message counts…");
        var starredCountsBySender = await _db.Messages
            .GroupBy(m => m.SenderId)
            .Select(g => new { SenderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SenderId, x => x.Count);
        await _db.Senders.ExecuteUpdateAsync(s => s.SetProperty(x => x.MsgCount, 0));

        progress?.Invoke("Upserting senders…");
        var groups = records
            .GroupBy(r => ResolveEmail(r.CanonicalEmail, r.FromRaw, r.GmailMessageId))
            .ToList();

        int newSenders = 0, updatedSenders = 0;
        var senderIdMap = new Dictionary<string, int>();

        var incomingEmails = groups.Select(g => g.Key).ToList();
        var existingSenders = await _db.Senders
            .Where(s => incomingEmails.Contains(s.EmailAddress))
            .ToListAsync();
        var existingMap = existingSenders.ToDictionary(s => s.EmailAddress);

        var newSenderEntities = new List<Sender>();

        foreach (var group in groups)
        {
            var email      = group.Key;
            var batchCount = group.Count();
            var first      = group.First();

            if (existingMap.TryGetValue(email, out var existing))
            {
                starredCountsBySender.TryGetValue(existing.SenderId, out var kept);
                existing.MsgCount  = batchCount + kept;
                existing.LastSeen  = now.ToString("o");
                existing.UpdatedAt = now.ToString("o");
                if (string.IsNullOrEmpty(existing.DisplayName) && !string.IsNullOrEmpty(first.DisplayName))
                    existing.DisplayName = first.DisplayName;
                updatedSenders++;
            }
            else
            {
                var sender = new Sender
                {
                    EmailAddress = email,
                    DisplayName  = string.IsNullOrEmpty(first.DisplayName) ? null : first.DisplayName,
                    FirstSeen    = now.ToString("o"),
                    LastSeen     = now.ToString("o"),
                    MsgCount     = batchCount,
                    RatingId     = 3,
                    CreatedAt    = now.ToString("o"),
                    UpdatedAt    = now.ToString("o")
                };
                _db.Senders.Add(sender);
                newSenderEntities.Add(sender);
                newSenders++;
            }
        }

        await _db.SaveChangesAsync();

        foreach (var s in existingSenders)
            senderIdMap[s.EmailAddress] = s.SenderId;
        foreach (var s in newSenderEntities)
            senderIdMap[s.EmailAddress] = s.SenderId;

        // Step 6: Insert new messages — skip any that survived as starred
        progress?.Invoke($"Inserting {records.Count} messages…");
        var existingGmailIds = await _db.Messages
            .Where(m => m.GmailMessageId != null)
            .Select(m => m.GmailMessageId!)
            .ToListAsync();
        var existingIdSet = existingGmailIds.ToHashSet();

        var messages = new List<Message>();
        int skipped = 0;
        foreach (var rec in records)
        {
            var email = ResolveEmail(rec.CanonicalEmail, rec.FromRaw, rec.GmailMessageId);
            if (!senderIdMap.TryGetValue(email, out var senderId)) continue;

            // Skip — this message survived the delete as a starred message
            if (existingIdSet.Contains(rec.GmailMessageId)) { skipped++; continue; }

            messages.Add(new Message
            {
                RunId          = run.RunId,
                SenderId       = senderId,
                GmailMessageId = rec.GmailMessageId,
                ThreadId       = string.IsNullOrEmpty(rec.ThreadId)     ? null : rec.ThreadId,
                InternalDate   = string.IsNullOrEmpty(rec.InternalDate) ? null : rec.InternalDate,
                HeaderDate     = string.IsNullOrEmpty(rec.HeaderDate)   ? null : rec.HeaderDate,
                Subject        = string.IsNullOrEmpty(rec.Subject)      ? null : rec.Subject,
                Snippet        = string.IsNullOrEmpty(rec.Snippet)      ? null : rec.Snippet,
                FromRaw        = string.IsNullOrEmpty(rec.FromRaw)      ? null : rec.FromRaw,
                ToRaw          = string.IsNullOrEmpty(rec.ToRaw)        ? null : rec.ToRaw,
                CreatedAt      = now.ToString("o")
            });
        }

        _db.Messages.AddRange(messages);
        await _db.SaveChangesAsync();

        return new IngestResultViewModel
        {
            Success           = true,
            RunId             = run.RunId,
            MessagesLoaded    = messages.Count,
            NewSenders        = newSenders,
            UpdatedSenders    = updatedSenders,
            SkippedDuplicates = skipped,
            SourceFile        = jsonPath,
            StartedAt         = startedAt.ToString("o"),
            CompletedAt       = DateTime.UtcNow.ToString("o")
        };
    }

    /// <summary>
    /// Resolves the canonical email key for a record using the fallback chain:
    ///   1. canonical_email from JSON (legacy rule)
    ///   2. ExtractCanonicalEmail(from_raw) if legacy is blank
    ///   3. from_raw as-is if no @ found (identifiable exception)
    ///   4. [no-from:{gmailMessageId}] if from_raw is also blank
    /// </summary>
    private static string ResolveEmail(string canonicalEmail, string fromRaw, string gmailMessageId)
    {
        // 1. Legacy canonical_email
        if (!string.IsNullOrWhiteSpace(canonicalEmail))
            return canonicalEmail.Trim().ToLower();

        // 2. Extract from from_raw
        if (!string.IsNullOrWhiteSpace(fromRaw))
        {
            var start = fromRaw.LastIndexOf('<');
            var end   = fromRaw.LastIndexOf('>');
            if (start >= 0 && end > start)
            {
                var extracted = fromRaw.Substring(start + 1, end - start - 1).Trim();
                if (extracted.Contains('@'))
                    return extracted.ToLowerInvariant();
            }
            var trimmed = fromRaw.Trim();
            if (trimmed.Contains('@'))
                return trimmed.ToLowerInvariant();

            // 3. from_raw unparseable — store as-is (no @ flags it as exception)
            return trimmed.ToLower();
        }

        // 4. Nothing available
        return $"[no-from:{gmailMessageId}]";
    }

    private static async Task RunPythonScriptAsync(string pythonExe, string scriptPath, string workingDir, string scriptArgs = "")
    {
        var psi = new ProcessStartInfo
        {
            FileName               = pythonExe,
            Arguments              = $"\"{scriptPath}\" {scriptArgs}".TrimEnd(),
            WorkingDirectory       = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Python extract failed (exit code {process.ExitCode})." +
                (string.IsNullOrWhiteSpace(stderr) ? "" : $"\nSTDERR:\n{stderr}") +
                (string.IsNullOrWhiteSpace(stdout) ? "" : $"\nSTDOUT:\n{stdout}"));
    }

}
