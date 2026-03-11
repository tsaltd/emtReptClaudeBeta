using EmailTracker.Models;
using EmailTracker.Repositories.Interfaces;
using EmailTracker.ViewModels;

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
}

public interface ISenderService
{
    Task<SenderSearchViewModel>  SearchAsync(SenderSearchViewModel filters);
    Task<SenderDetailViewModel?> GetDetailAsync(int senderId);

    /// <summary>
    /// Extracts the canonical email address from a raw From header.
    /// e.g. "Some Disguised Name [Org]" &lt;admin@e.example.org&gt;  → admin@e.example.org
    /// </summary>
    /// <summary>Browse single-record view at position index (sorted by msg_count DESC).</summary>
    Task<SenderBrowseViewModel?> GetBrowseAsync(int index);

    /// <summary>Update a sender's rating. Returns updated rating name or null on failure.</summary>
    Task<string?> UpdateRatingAsync(int senderId, int ratingId);

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

    public RunService(IRunRepository runRepo, IMessageRepository msgRepo)
    {
        _runRepo = runRepo;
        _msgRepo = msgRepo;
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

        return new RunDetailViewModel
        {
            RunId        = run.RunId,
            WindowStart  = run.WindowStart,
            WindowEnd    = run.WindowEnd,
            StartedAt    = run.StartedAt,
            SourceLabel  = run.SourceLabel,
            MessageCount = await _runRepo.GetMessageCountAsync(runId),
            SenderCount  = await _runRepo.GetSenderCountAsync(runId)
        };
    }
}

public class MessageService : IMessageService
{
    private readonly IMessageRepository _msgRepo;
    private readonly IRunRepository     _runRepo;
    private readonly IRatingRepository  _ratingRepo;

    public MessageService(IMessageRepository msgRepo, IRunRepository runRepo, IRatingRepository ratingRepo)
    {
        _msgRepo    = msgRepo;
        _runRepo    = runRepo;
        _ratingRepo = ratingRepo;
    }

    public async Task<MessageSearchViewModel> SearchAsync(MessageSearchViewModel filters)
    {
        var messages = await _msgRepo.SearchAsync(
            filters.RunId, filters.SenderId, filters.SearchTerm,
            filters.DateFrom, filters.DateTo, filters.RatingFilter,
            filters.Page, filters.PageSize);

        var total = await _msgRepo.CountAsync(
            filters.RunId, filters.SenderId, filters.SearchTerm,
            filters.DateFrom, filters.DateTo, filters.RatingFilter);

        var runs    = await _runRepo.GetAllAsync();
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
        filters.AvailableRuns = runs.Select(r => new RunSummaryViewModel
        {
            RunId       = r.RunId,
            WindowStart = r.WindowStart,
            WindowEnd   = r.WindowEnd,
            SourceLabel = r.SourceLabel
        });
        filters.AvailableRatings = ratings.Select(r => new RatingOptionViewModel
        {
            RatingId   = r.RatingId,
            RatingName = r.RatingName,
            SortOrder  = r.SortOrder
        });

        return filters;
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
            filters.SearchTerm, filters.RatingFilter, filters.Page, filters.PageSize);

        var total   = await _senderRepo.CountAsync(filters.SearchTerm, filters.RatingFilter);
        var ratings = await _ratingRepo.GetAllAsync();

        filters.Senders = senders.Select(s => new SenderSummaryViewModel
        {
            SenderId     = s.SenderId,
            EmailAddress = s.EmailAddress,
            DisplayName  = s.DisplayName,
            MsgCount     = s.MsgCount,
            RatingName   = s.RatingName,
            RatingId     = s.RatingId,
            FirstSeen    = s.FirstSeen,
            LastSeen     = s.LastSeen
        });

        filters.TotalCount       = total;
        filters.AvailableRatings = ratings.Select(r => new RatingOptionViewModel
        {
            RatingId   = r.RatingId,
            RatingName = r.RatingName,
            SortOrder  = r.SortOrder
        });

        return filters;
    }

    public async Task<SenderDetailViewModel?> GetDetailAsync(int senderId)
    {
        var sender = await _senderRepo.GetByIdAsync(senderId);
        if (sender == null) return null;

        var recentMessages = await _msgRepo.SearchAsync(
            null, senderId, null, null, null, null, 1, 20);

        var fromRawBreakdown = await _msgRepo.GetFromRawBreakdownAsync(senderId);

        var ratings = await _ratingRepo.GetAllAsync();

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
            RecentMessages   = recentMessages.Select(m => new MessageRowViewModel
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
            }),
            FromRawBreakdown = fromRawBreakdown.Select(x => new FromRawSummaryViewModel
            {
                FromRaw  = x.FromRaw,
                MsgCount = x.Count
            }),
            AvailableRatings = ratings.Select(r => new RatingOptionViewModel
            {
                RatingId   = r.RatingId,
                RatingName = r.RatingName,
                SortOrder  = r.SortOrder
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
        var all = await _senderRepo.SearchAsync(null, null, 1, int.MaxValue);
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
            SortOrder  = r.SortOrder
        });
    }
}
