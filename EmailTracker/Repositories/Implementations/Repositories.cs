using Microsoft.EntityFrameworkCore;
using EmailTracker.Data;
using EmailTracker.Models;
using EmailTracker.Repositories.Interfaces;

namespace EmailTracker.Repositories.Implementations;

// ── Run Repository ───────────────────────────────────────────────
public class RunRepository : IRunRepository
{
    private readonly AppDbContext _db;
    public RunRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Run>> GetAllAsync() =>
        await _db.Runs.OrderByDescending(r => r.StartedAt).ToListAsync();

    public async Task<Run?> GetByIdAsync(int id) =>
        await _db.Runs.FindAsync(id);

    public async Task<Run> CreateAsync(Run run)
    {
        _db.Runs.Add(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task<Run> UpdateAsync(Run run)
    {
        _db.Runs.Update(run);
        await _db.SaveChangesAsync();
        return run;
    }

    public async Task DeleteAsync(int id)
    {
        var run = await _db.Runs.FindAsync(id);
        if (run != null) { _db.Runs.Remove(run); await _db.SaveChangesAsync(); }
    }

    public async Task<int> GetMessageCountAsync(int runId) =>
        await _db.Messages.CountAsync(m => m.RunId == runId);

    public async Task<int> GetSenderCountAsync(int runId) =>
        await _db.Messages.Where(m => m.RunId == runId)
                          .Select(m => m.SenderId)
                          .Distinct()
                          .CountAsync();
}

// ── Message Repository ───────────────────────────────────────────
public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _db;
    public MessageRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<VMessageWithSender>> SearchAsync(
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter,
        string? statusFilter,
        int     page,
        int     pageSize)
    {
        var q = BuildQuery(senderId, searchTerm, dateFrom, dateTo, ratingFilter, statusFilter);
        return await q.OrderBy(m => m.SortOrder == 0 ? int.MaxValue : m.SortOrder)
                      .ThenBy(m => m.EmailAddress)
                      .ThenBy(m => m.FromRaw)
                      .ThenBy(m => m.Subject)
                      .ThenByDescending(m => m.InternalDate)
                      .Skip((page - 1) * pageSize)
                      .Take(pageSize)
                      .ToListAsync();
    }

    public async Task<int> CountAsync(
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter,
        string? statusFilter) =>
        await BuildQuery(senderId, searchTerm, dateFrom, dateTo, ratingFilter, statusFilter).CountAsync();

    public async Task<Message?> GetByIdAsync(int id) =>
        await _db.Messages.Include(m => m.Sender).Include(m => m.Run).FirstOrDefaultAsync(m => m.MessageId == id);

    public async Task<Message> CreateAsync(Message message)
    {
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    public async Task DeleteAsync(int id)
    {
        var msg = await _db.Messages.FindAsync(id);
        if (msg != null) { _db.Messages.Remove(msg); await _db.SaveChangesAsync(); }
    }

    public async Task<IEnumerable<VMessageWithSender>> GetByGmailIdsAsync(IEnumerable<string> gmailIds)
    {
        var ids = gmailIds.ToList();
        return await _db.VMessageWithSenders
            .Where(m => m.GmailMessageId != null && ids.Contains(m.GmailMessageId))
            .ToListAsync();
    }

    public async Task<IEnumerable<(string FromRaw, int Count)>> GetFromRawBreakdownAsync(int senderId)
    {
        var result = await _db.Messages
            .Where(m => m.SenderId == senderId && m.FromRaw != null)
            .GroupBy(m => m.FromRaw!)
            .Select(g => new { FromRaw = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
        return result.Select(x => (x.FromRaw, x.Count));
    }

    private IQueryable<VMessageWithSender> BuildQuery(
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter,
        string? statusFilter)
    {
        var q = _db.VMessageWithSenders.AsQueryable();

        if (senderId.HasValue)
            q = q.Where(m => m.SenderId == senderId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            q = q.Where(m =>
                (m.Subject != null && m.Subject.ToLower().Contains(term)) ||
                (m.EmailAddress.ToLower().Contains(term)) ||
                (m.FromRaw != null && m.FromRaw.ToLower().Contains(term)) ||
                (m.Snippet != null && m.Snippet.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(dateFrom))
            q = q.Where(m => m.InternalDate != null && string.Compare(m.InternalDate, dateFrom) >= 0);

        if (!string.IsNullOrWhiteSpace(dateTo))
            q = q.Where(m => m.InternalDate != null && string.Compare(m.InternalDate, dateTo) <= 0);

        if (!string.IsNullOrWhiteSpace(ratingFilter))
        {
            var filters = ratingFilter
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.ToLower())
                .ToList();
            if (filters.Count > 0)
                q = q.Where(m => m.RatingName != null && filters.Contains(m.RatingName.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
            q = q.Where(m => m.StatusName == statusFilter);

        return q;
    }
}

// ── Sender Repository ────────────────────────────────────────────
public class SenderRepository : ISenderRepository
{
    private readonly AppDbContext _db;
    public SenderRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<VSenderWithRating>> SearchAsync(
        string? searchTerm,
        string? ratingFilter,
        string? statusFilter,
        int     page,
        int     pageSize,
        bool    sortAsc = false)
    {
        var q = BuildQuery(searchTerm, ratingFilter, statusFilter);
        var ordered = sortAsc
            ? q.OrderBy(s => s.MsgCount)
            : q.OrderByDescending(s => s.MsgCount);
        return await ordered
                      .Skip((page - 1) * pageSize)
                      .Take(pageSize)
                      .ToListAsync();
    }

    public async Task<int> CountAsync(string? searchTerm, string? ratingFilter, string? statusFilter) =>
        await BuildQuery(searchTerm, ratingFilter, statusFilter).CountAsync();

    public async Task<Sender?> GetByIdAsync(int id) =>
        await _db.Senders.Include(s => s.Rating).FirstOrDefaultAsync(s => s.SenderId == id);

    public async Task<Sender?> GetByEmailAsync(string email) =>
        await _db.Senders.Include(s => s.Rating)
                         .FirstOrDefaultAsync(s => s.EmailAddress == email.ToLower());

    public async Task<Sender> CreateAsync(Sender sender)
    {
        _db.Senders.Add(sender);
        await _db.SaveChangesAsync();
        return sender;
    }

    public async Task<Sender> UpdateAsync(Sender sender)
    {
        _db.Senders.Update(sender);
        await _db.SaveChangesAsync();
        return sender;
    }

    public async Task UpdateRatingBulkAsync(IEnumerable<int> senderIds, int ratingId)
    {
        var ids = senderIds.ToList();
        await _db.Senders
            .Where(s => ids.Contains(s.SenderId))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RatingId, ratingId));
    }

    public async Task<int> GetBestRatingIdByDomainAsync(string domain)
    {
        return await _db.Senders
            .Where(s => s.EmailAddress == domain)
            .Join(_db.Ratings, s => s.RatingId, r => r.RatingId, (s, r) => r)
            .OrderBy(r => r.SortOrder)
            .Select(r => r.RatingId)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateRatingByDomainAsync(string domain, int ratingId)
    {
        await _db.Senders
            .Where(s => s.EmailAddress == domain)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RatingId, ratingId));
    }

    public async Task SetStatusAsync(int senderId, int statusId)
    {
        await _db.Senders
            .Where(s => s.SenderId == senderId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.StatusId, statusId));
    }

    public async Task<IEnumerable<VSenderWithRating>> GetTopSendersForRunAsync(int runId, int limit = 10)
    {
        var senderIdsInRun = _db.Messages
            .Where(m => m.RunId == runId)
            .Select(m => m.SenderId)
            .Distinct();

        return await _db.VSenderWithRatings
            .Where(s => senderIdsInRun.Contains(s.SenderId))
            .OrderByDescending(s => s.MsgCount)
            .Take(limit)
            .ToListAsync();
    }

    private IQueryable<VSenderWithRating> BuildQuery(string? searchTerm, string? ratingFilter, string? statusFilter)
    {
        var q = _db.VSenderWithRatings.Where(s => s.MsgCount > 0);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            q = q.Where(s =>
                s.EmailAddress.ToLower().Contains(term) ||
                (s.DisplayName != null && s.DisplayName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(ratingFilter))
        {
            var filters = ratingFilter
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Select(f => f.ToLower())
                .ToList();
            if (filters.Count > 0)
                q = q.Where(s => s.RatingName != null && filters.Contains(s.RatingName.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
            q = q.Where(s => s.StatusName == statusFilter);

        return q;
    }
}

// ── Rating Repository ────────────────────────────────────────────
public class RatingRepository : IRatingRepository
{
    private readonly AppDbContext _db;
    public RatingRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Rating>> GetAllAsync() =>
        await _db.Ratings.OrderBy(r => r.SortOrder).ToListAsync();

    public async Task<Rating?> GetByIdAsync(int id) =>
        await _db.Ratings.FindAsync(id);
}

// ── Priority Message Repository ──────────────────────────────────
public class PriorityMessageRepository : IPriorityMessageRepository
{
    private readonly AppDbContext _db;
    public PriorityMessageRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(string gmailMessageId)
    {
        _db.PriorityMessages.Add(new PriorityMessage { GmailMessageId = gmailMessageId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(string gmailMessageId)
    {
        var row = await _db.PriorityMessages.FirstOrDefaultAsync(p => p.GmailMessageId == gmailMessageId);
        if (row != null) { _db.PriorityMessages.Remove(row); await _db.SaveChangesAsync(); }
    }

    public async Task<bool> IsTrackedAsync(string gmailMessageId) =>
        await _db.PriorityMessages.AnyAsync(p => p.GmailMessageId == gmailMessageId);

    public async Task<HashSet<string>> GetAllIdsAsync() =>
        (await _db.PriorityMessages.Select(p => p.GmailMessageId).ToListAsync()).ToHashSet();
}

// ── Sender Subset Repository ────────────────────────────────────
public class SenderSubsetRepository : ISenderSubsetRepository
{
    private readonly AppDbContext _db;

    public SenderSubsetRepository(AppDbContext db) => _db = db;

    public async Task EnsureSchemaAsync()
    {
        const string sqlSubset = @"
CREATE TABLE IF NOT EXISTS sender_subset (
    subset_id    INTEGER PRIMARY KEY,
    subset_name  TEXT NOT NULL UNIQUE,
    created_at   TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at   TEXT NOT NULL DEFAULT (datetime('now'))
);";

        const string sqlMember = @"
CREATE TABLE IF NOT EXISTS sender_subset_member (
    subset_id    INTEGER NOT NULL,
    sender_id    INTEGER NOT NULL,
    created_at   TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (subset_id, sender_id),
    FOREIGN KEY (subset_id) REFERENCES sender_subset(subset_id) ON DELETE CASCADE,
    FOREIGN KEY (sender_id) REFERENCES sender(sender_id) ON DELETE CASCADE
);";

        const string sqlIndex = @"CREATE INDEX IF NOT EXISTS ix_sender_subset_member_sender_id
    ON sender_subset_member(sender_id);";

        await _db.Database.ExecuteSqlRawAsync(sqlSubset);
        await _db.Database.ExecuteSqlRawAsync(sqlMember);
        await _db.Database.ExecuteSqlRawAsync(sqlIndex);
    }

    public async Task<IEnumerable<SenderSubset>> GetAllAsync()
    {
        await EnsureSchemaAsync();
        return await _db.SenderSubsets
            .AsNoTracking()
            .OrderBy(s => s.SubsetName)
            .ToListAsync();
    }

    public async Task<HashSet<int>> GetSenderIdsAsync(int subsetId)
    {
        await EnsureSchemaAsync();
        var ids = await _db.SenderSubsetMembers
            .Where(m => m.SubsetId == subsetId)
            .Select(m => m.SenderId)
            .ToListAsync();
        return ids.ToHashSet();
    }

    public async Task<SenderSubset> SaveAsync(string subsetName, IEnumerable<int> senderIds)
    {
        await EnsureSchemaAsync();

        var name = subsetName.Trim();
        var ids = senderIds.Distinct().ToList();

        var subset = await _db.SenderSubsets
            .FirstOrDefaultAsync(s => s.SubsetName == name);

        if (subset == null)
        {
            subset = new SenderSubset
            {
                SubsetName = name,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };
            _db.SenderSubsets.Add(subset);
            await _db.SaveChangesAsync();
        }
        else
        {
            subset.UpdatedAt = DateTime.UtcNow.ToString("o");
            _db.SenderSubsets.Update(subset);
            await _db.SaveChangesAsync();
        }

        var existing = _db.SenderSubsetMembers.Where(m => m.SubsetId == subset.SubsetId);
        _db.SenderSubsetMembers.RemoveRange(existing);

        foreach (var senderId in ids)
        {
            _db.SenderSubsetMembers.Add(new SenderSubsetMember
            {
                SubsetId = subset.SubsetId,
                SenderId = senderId,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
        }

        await _db.SaveChangesAsync();
        return subset;
    }
}
