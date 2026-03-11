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
        int?    runId,
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter,
        int     page,
        int     pageSize);

    Task<int> CountAsync(
        int?    runId,
        int?    senderId,
        string? searchTerm,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter);

    Task<Message?> GetByIdAsync(int id);
    Task<Message>  CreateAsync(Message message);
    Task           DeleteAsync(int id);
    Task<IEnumerable<(string FromRaw, int Count)>> GetFromRawBreakdownAsync(int senderId);
}

public interface ISenderRepository
{
    Task<IEnumerable<VSenderWithRating>> SearchAsync(
        string? searchTerm,
        string? ratingFilter,
        int     page,
        int     pageSize);

    Task<int>            CountAsync(string? searchTerm, string? ratingFilter);
    Task<Sender?>        GetByIdAsync(int id);
    Task<Sender?>        GetByEmailAsync(string email);
    Task<Sender>         CreateAsync(Sender sender);
    Task<Sender>         UpdateAsync(Sender sender);
    Task<IEnumerable<VSenderWithRating>> GetTopBySendCountAsync(int? runId, int take = 10);
}

public interface IRatingRepository
{
    Task<IEnumerable<Rating>> GetAllAsync();
    Task<Rating?>             GetByIdAsync(int id);
}
