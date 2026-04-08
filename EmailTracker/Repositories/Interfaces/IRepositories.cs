using EmailTracker.Models;

namespace EmailTracker.Repositories.Interfaces;

public interface IRunRepository
{
    Task<IEnumerable<Run>> GetAllAsync();
    Task<Run?>             GetByIdAsync(int id);
    Task<Run>              CreateAsync(Run run);
    Task<Run>              UpdateAsync(Run run);
    Task                   DeleteAsync(int id);
    Task<int>              GetMessageCountAsync(int runId);
    Task<int>              GetSenderCountAsync(int runId);
}

public interface IMessageRepository
{
    Task<IEnumerable<VMessageWithSender>> SearchAsync(
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter,
        int     page,
        int     pageSize);

    Task<int> CountAsync(
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter);

    Task<Message?> GetByIdAsync(int id);
    Task<Message>  CreateAsync(Message message);
    Task           DeleteAsync(int id);
    Task<IEnumerable<(string FromRaw, int Count)>> GetFromRawBreakdownAsync(int senderId);
    Task<IEnumerable<VMessageWithSender>> GetByGmailIdsAsync(IEnumerable<string> gmailIds);
}

public interface ISenderRepository
{
    Task<IEnumerable<VSenderWithRating>> SearchAsync(
        string? searchTerm,
        string? ratingFilter,
        string? statusFilter,
        int     page,
        int     pageSize,
        bool    sortAsc = false);

    Task<int>            CountAsync(string? searchTerm, string? ratingFilter, string? statusFilter);
    Task<Sender?>        GetByIdAsync(int id);
    Task<Sender?>        GetByEmailAsync(string email);
    Task<Sender>         CreateAsync(Sender sender);
    Task<Sender>         UpdateAsync(Sender sender);
    Task                 UpdateRatingBulkAsync(IEnumerable<int> senderIds, int ratingId);
    Task                 SetStatusAsync(int senderId, int statusId);
    Task<IEnumerable<VSenderWithRating>> GetTopSendersForRunAsync(int runId, int limit = 10);
}

public interface IRatingRepository
{
    Task<IEnumerable<Rating>> GetAllAsync();
    Task<Rating?>             GetByIdAsync(int id);
}

public interface IPriorityMessageRepository
{
    Task              AddAsync(string gmailMessageId);
    Task              RemoveAsync(string gmailMessageId);
    Task<bool>        IsTrackedAsync(string gmailMessageId);
    Task<HashSet<string>> GetAllIdsAsync();
}
