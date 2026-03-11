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
        int?    runId,
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter,
        int     page,
        int     pageSize)
    {
        var q = BuildQuery(runId, senderId, searchTerm, dateFrom, dateTo, ratingFilter);
        return await q.OrderBy(m => m.EmailAddress)
                      .ThenBy(m => m.FromRaw)
                      .ThenBy(m => m.Subject)
                      .ThenByDescending(m => m.InternalDate)
                      .Skip((page - 1) * pageSize)
                      .Take(pageSize)
                      .ToListAsync();
    }

    public async Task<int> CountAsync(
        int?    runId,
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter) =>
        await BuildQuery(runId, senderId, searchTerm, dateFrom, dateTo, ratingFilter).CountAsync();

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
        int?    runId,
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter)
    {
        var q = _db.VMessageWithSenders.AsQueryable();

        if (runId.HasValue)
            q = q.Where(m => m.RunId == runId.Value);

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
            q = q.Where(m => m.RatingName == ratingFilter);

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
        int     page,
        int     pageSize)
    {
        var q = BuildQuery(searchTerm, ratingFilter);
        return await q.OrderByDescending(s => s.MsgCount)
                      .Skip((page - 1) * pageSize)
                      .Take(pageSize)
                      .ToListAsync();
    }

    public async Task<int> CountAsync(string? searchTerm, string? ratingFilter) =>
        await BuildQuery(searchTerm, ratingFilter).CountAsync();

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

    public async Task<IEnumerable<VSenderWithRating>> GetTopBySendCountAsync(int? runId, int take = 10)
    {
        if (runId.HasValue)
        {
            // Get senders who appear in this run, ordered by message count in that run
            var senderIdsInRun = await _db.Messages
                .Where(m => m.RunId == runId.Value)
                .GroupBy(m => m.SenderId)
                .OrderByDescending(g => g.Count())
                .Take(take)
                .Select(g => g.Key)
                .ToListAsync();

            return await _db.VSenderWithRatings
                .Where(s => senderIdsInRun.Contains(s.SenderId))
                .ToListAsync();
        }

        return await _db.VSenderWithRatings
            .OrderByDescending(s => s.MsgCount)
            .Take(take)
            .ToListAsync();
    }

    private IQueryable<VSenderWithRating> BuildQuery(string? searchTerm, string? ratingFilter)
    {
        var q = _db.VSenderWithRatings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            q = q.Where(s =>
                s.EmailAddress.ToLower().Contains(term) ||
                (s.DisplayName != null && s.DisplayName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(ratingFilter))
            q = q.Where(s => s.RatingName == ratingFilter);

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
