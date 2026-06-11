namespace EmailTracker.ViewModels;

// ════════════════════════════════════════════════════════════════
//  SHARED FILTER CONTRACT
// ════════════════════════════════════════════════════════════════

/// <summary>
/// Single filter contract passed to every service that supports filtered queries.
/// Each service applies only the fields relevant to its domain and ignores the rest.
/// </summary>
public record FilterParameters
{
    public string? RatingFilter { get; init; }
    public string? StatusFilter { get; init; }
    public string? DateFrom     { get; init; }
    public string? DateTo       { get; init; }
    public bool    PriorityOnly { get; init; }
}

// ════════════════════════════════════════════════════════════════
//  SHARED FILTER BAR ViewModel
// ════════════════════════════════════════════════════════════════

public class FilterBarViewModel
{
    public IEnumerable<RatingOptionViewModel> AvailableRatings { get; set; } = [];
    public string? SelectedRatings { get; set; }   // comma-separated rating names
    public string? DateFrom        { get; set; }
    public string? DateTo          { get; set; }
    public string? StatusFilter    { get; set; }   // "" | "OPEN" | "RATED"
    public bool    PriorityOnly    { get; set; }
    public bool    ShowDate        { get; set; } = true;
    public bool    ShowStatus      { get; set; } = true;
    public bool    ShowPriority    { get; set; } = true;
}

// ════════════════════════════════════════════════════════════════
//  RUN ViewModels
// ════════════════════════════════════════════════════════════════

public class RunSummaryViewModel
{
    public int     RunId        { get; set; }
    public string  WindowStart  { get; set; } = string.Empty;
    public string  WindowEnd    { get; set; } = string.Empty;
    public string  StartedAt    { get; set; } = string.Empty;
    public string? SourceLabel  { get; set; }
    public int     MessageCount { get; set; }
    public int     SenderCount  { get; set; }

    public string DisplayWindow =>
        $"{FormatDate(WindowStart)} – {FormatDate(WindowEnd)}";

    private static string FormatDate(string iso) =>
        DateTime.TryParse(iso, out var dt) ? dt.ToString("MMM d, yyyy") : iso;
}

public class RunListViewModel
{
    public IEnumerable<RunSummaryViewModel> Runs       { get; set; } = [];
    public string?                          SearchTerm { get; set; }
    public int                              TotalRuns  { get; set; }
}

public class RunDetailViewModel
{
    public int     RunId        { get; set; }
    public string  WindowStart  { get; set; } = string.Empty;
    public string  WindowEnd    { get; set; } = string.Empty;
    public string  StartedAt    { get; set; } = string.Empty;
    public string? SourceLabel  { get; set; }
    public int     MessageCount { get; set; }
    public int     SenderCount  { get; set; }
    public IEnumerable<SenderSummaryViewModel> TopSenders { get; set; } = [];
}

// ════════════════════════════════════════════════════════════════
//  MESSAGE ViewModels
// ════════════════════════════════════════════════════════════════

public class MessageRowViewModel
{
    public int     MessageId      { get; set; }
    public int     RunId          { get; set; }
    public int     SenderId       { get; set; }
    public string  EmailAddress   { get; set; } = string.Empty;
    public string  RatingName     { get; set; } = string.Empty;
    public string? ColorCode      { get; set; }
    public string? Subject        { get; set; }
    public string? Snippet        { get; set; }
    public string? InternalDate   { get; set; }
    public string? FromRaw        { get; set; }
    public string? GmailMessageId { get; set; }
    public string? ThreadId       { get; set; }
    public bool    IsPriority     { get; set; }

    public string DisplayDate =>
        DateTime.TryParse(InternalDate, out var dt) ? dt.ToString("MMM d, yyyy h:mm tt") : (InternalDate ?? "—");
}

public class MessageSearchViewModel
{
    // ── Filter inputs (bound from AJAX request) ──────────────────
    public string? SearchTerm   { get; set; }
    public int?    SenderId     { get; set; }
    public string? DateFrom     { get; set; }
    public string? DateTo       { get; set; }
    public string? RatingFilter { get; set; }
    public string? StatusFilter { get; set; }   // "" | "OPEN" | "RATED"
    public int     Page         { get; set; } = 1;
    public int     PageSize     { get; set; } = 50;

    // ── Results ──────────────────────────────────────────────────
    public IEnumerable<MessageRowViewModel> Messages    { get; set; } = [];
    public int                              TotalCount  { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // ── Filter options for dropdowns ─────────────────────────────
    public IEnumerable<RatingOptionViewModel>  AvailableRatings { get; set; } = [];
}

// ════════════════════════════════════════════════════════════════
//  SENDER ViewModels
// ════════════════════════════════════════════════════════════════

public class SenderSummaryViewModel
{
    public int     SenderId      { get; set; }
    public string  EmailAddress  { get; set; } = string.Empty;
    public string? DisplayName   { get; set; }
    public int     MsgCount      { get; set; }
    public string  RatingName    { get; set; } = string.Empty;
    public string? ColorCode     { get; set; }
    public int     RatingId      { get; set; }
    public int     StatusId      { get; set; } = 1;
    public string  StatusName    { get; set; } = "OPEN";
    public string? FirstSeen     { get; set; }
    public string? LastSeen      { get; set; }

    public string DisplayFirstSeen =>
        DateTime.TryParse(FirstSeen, out var dt) ? dt.ToString("MMM d, yyyy") : (FirstSeen ?? "—");
    public string DisplayLastSeen =>
        DateTime.TryParse(LastSeen,  out var dt) ? dt.ToString("MMM d, yyyy") : (LastSeen  ?? "—");
}

public class SenderSearchViewModel
{
    public string?  SearchTerm   { get; set; }
    public string?  RatingFilter { get; set; }
    public string?  StatusFilter { get; set; }
    public bool     SortAsc      { get; set; }
    public int      Page         { get; set; } = 1;
    public int      PageSize     { get; set; } = 50;

    public IEnumerable<SenderSummaryViewModel> Senders      { get; set; } = [];
    public IEnumerable<RatingOptionViewModel>  AvailableRatings { get; set; } = [];
    public int                                 TotalCount   { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class SenderSubsetOptionViewModel
{
    public int SubsetId { get; set; }
    public string SubsetName { get; set; } = string.Empty;
}

public class FromRawSummaryViewModel
{
    public string FromRaw  { get; set; } = string.Empty;
    public int    MsgCount { get; set; }
}

public class FromRawGroupViewModel
{
    public string                       FromRaw  { get; set; } = string.Empty;
    public List<MessageRowViewModel>    Messages { get; set; } = [];
}

public class SenderDetailViewModel
{
    public int     SenderId     { get; set; }
    public string  EmailAddress { get; set; } = string.Empty;
    public string? DisplayName  { get; set; }
    public int     MsgCount     { get; set; }
    public int     RatingId     { get; set; }
    public string  RatingName   { get; set; } = string.Empty;
    public string? FirstSeen    { get; set; }
    public string? LastSeen     { get; set; }
    public string  CreatedAt    { get; set; } = string.Empty;
    public string  UpdatedAt    { get; set; } = string.Empty;

    public IEnumerable<RatingOptionViewModel>  AvailableRatings { get; set; } = [];
    public IEnumerable<FromRawGroupViewModel>  FromRawGroups    { get; set; } = [];
}

public class SenderSummaryPageViewModel
{
    public int     SenderId     { get; set; }
    public string  EmailAddress { get; set; } = string.Empty;
    public string? DisplayName  { get; set; }
    public int     MsgCount     { get; set; }
    public int     RatingId     { get; set; }
    public string  RatingName   { get; set; } = string.Empty;
    public string? ColorCode    { get; set; }
    public string? FirstSeen    { get; set; }
    public string? LastSeen     { get; set; }

    public IEnumerable<RatingOptionViewModel>  AvailableRatings { get; set; } = [];
    public IEnumerable<FromRawGroupViewModel>  FromRawGroups    { get; set; } = [];

    // ── Pagination ───────────────────────────────────────────────
    public int  Page        { get; set; } = 1;
    public int  PageSize    { get; set; } = 10;
    public int  TotalGroups { get; set; }
    public bool SortAsc     { get; set; }
    public int  TotalPages  => (int)Math.Ceiling((double)TotalGroups / PageSize);
    public bool HasPrev     => Page > 1;
    public bool HasNext     => Page < TotalPages;
    public int  PrevPage    => Page - 1;
    public int  NextPage    => Page + 1;
}

public class SenderPeriodRowViewModel
{
    public int SenderId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public int RatingId { get; set; }
    public string RatingName { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
}

public class SenderPeriodListViewModel
{
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public IEnumerable<SenderPeriodRowViewModel> Senders { get; set; } = [];
    public int TotalCount => Senders.Count();
}

// ════════════════════════════════════════════════════════════════
//  SENDER BROWSE ViewModel  (single-record navigator)
// ════════════════════════════════════════════════════════════════

/// <summary>
/// Powers the single-record Browse view.
/// Sorted by msg_count DESC. Supports NEXT/PREV 1 and NEXT/PREV 20.
/// </summary>
public class SenderBrowseViewModel
{
    // ── Current record ───────────────────────────────────────────
    public int     SenderId     { get; set; }
    public string  EmailAddress { get; set; } = string.Empty;
    public string? DisplayName  { get; set; }
    public int     MsgCount     { get; set; }
    public int     RatingId     { get; set; }
    public string  RatingName   { get; set; } = string.Empty;
    public string? FirstSeen    { get; set; }
    public string? LastSeen     { get; set; }

    // ── Navigation state ─────────────────────────────────────────
    public int  CurrentIndex  { get; set; }   // 0-based position in sorted list
    public int  TotalSenders  { get; set; }
    public bool HasPrev       => CurrentIndex > 0;
    public bool HasNext       => CurrentIndex < TotalSenders - 1;
    public bool HasPrev20     => CurrentIndex >= 20;
    public bool HasNext20     => CurrentIndex <= TotalSenders - 21;

    public int PrevIndex      => Math.Max(0, CurrentIndex - 1);
    public int NextIndex      => Math.Min(TotalSenders - 1, CurrentIndex + 1);
    public int Prev20Index    => Math.Max(0, CurrentIndex - 20);
    public int Next20Index    => Math.Min(TotalSenders - 1, CurrentIndex + 20);

    // ── Position label ───────────────────────────────────────────
    public string PositionLabel => $"{CurrentIndex + 1} of {TotalSenders}";

    // ── Rating dropdown options ───────────────────────────────────
    public IEnumerable<RatingOptionViewModel> AvailableRatings { get; set; } = [];
}

// ════════════════════════════════════════════════════════════════
//  MESSAGE GROUPED ViewModel  (CEA-paged tree view)
// ════════════════════════════════════════════════════════════════

public class CeaGroupViewModel
{
    public int     SenderId        { get; set; }
    public string  EmailAddress    { get; set; } = string.Empty;
    public string  RatingName      { get; set; } = string.Empty;
    public int     RatingId        { get; set; }
    public int     RatingSortOrder { get; set; } = int.MaxValue;
    public string? ColorCode       { get; set; }
    public int     MsgCount        { get; set; }
    public int     StatusId        { get; set; } = 1;
    public string  StatusName      { get; set; } = "OPEN";
    public List<FromRawGroupViewModel> FromRawGroups { get; set; } = [];
}

public class MessageGroupedViewModel
{
    public int?    SenderId     { get; set; }
    public string? SenderIdsCsv { get; set; }
    public int     CurrentRunId  { get; set; }
    public string? SearchTerm   { get; set; }
    public string? RatingFilter { get; set; }
    public string? StatusFilter { get; set; }   // "" | "OPEN" | "RATED"
    public string? DateFrom     { get; set; }
    public string? DateTo       { get; set; }
    public bool    PriorityOnly { get; set; }
    public string  SortBy       { get; set; } = "count";   // "count" | "email"
    public bool    SortAsc      { get; set; }
    public int     Page         { get; set; } = 1;
    public int     PageSize     { get; set; } = 10;
    public int     TotalSenders { get; set; }
    public int     TotalPages   => (int)Math.Ceiling((double)TotalSenders / PageSize);
    public bool    HasPrev      => Page > 1;
    public bool    HasNext      => Page < TotalPages;

    public IEnumerable<SenderSummaryViewModel> AvailableRunSenders { get; set; } = [];
    public IEnumerable<int> SelectedSenderIds { get; set; } = [];
    public string SelectedSenderIdsCsv => string.Join(",", SelectedSenderIds);

    public IEnumerable<CeaGroupViewModel>     CeaGroups        { get; set; } = [];
    public IEnumerable<RatingOptionViewModel> AvailableRatings { get; set; } = [];
}

// ════════════════════════════════════════════════════════════════
//  MESSAGE BROWSE ViewModel  (sender-panel navigator + message list)
// ════════════════════════════════════════════════════════════════

public class MessageBrowseViewModel
{
    // ── Current sender header ─────────────────────────────────────
    public int     SenderId     { get; set; }
    public string  EmailAddress { get; set; } = string.Empty;
    public string  RatingName   { get; set; } = string.Empty;
    public string? ColorCode    { get; set; }
    public int     StatusId     { get; set; } = 1;
    public string  StatusName   { get; set; } = "OPEN";

    // ── Message list (flat, paginated) ────────────────────────────
    public IEnumerable<MessageRowViewModel> Messages    { get; set; } = [];
    public int     MsgTotal    { get; set; }
    public int     MsgPage     { get; set; } = 1;
    public int     MsgPageSize { get; set; } = 50;
    public int     MsgTotalPages => (int)Math.Ceiling((double)MsgTotal / MsgPageSize);
    public string? MsgDateFrom { get; set; }
    public string? MsgDateTo   { get; set; }

    // ── Sender panel state ────────────────────────────────────────
    public string? SenderSearch   { get; set; }
    public bool    SenderSortAsc  { get; set; }
    public int     SenderPage     { get; set; } = 1;
    public int     SenderPageSize { get; set; } = 10;
    public int     SenderTotal    { get; set; }
    public int     SenderRank     { get; set; }   // 1-based; 0 = not in filtered list
    public int SenderTotalPages   => (int)Math.Ceiling((double)SenderTotal / SenderPageSize);

    public IEnumerable<SenderSummaryViewModel> SenderList      { get; set; } = [];
    public int?    PrevSenderId   { get; set; }
    public int?    NextSenderId   { get; set; }
    public int?    Prev10SenderId { get; set; }
    public int?    Next10SenderId { get; set; }

    // ── Rating hotkeys ────────────────────────────────────────────
    public IEnumerable<RatingOptionViewModel> AvailableRatings { get; set; } = [];
}

// ════════════════════════════════════════════════════════════════
//  GMAIL INGEST ViewModel
// ════════════════════════════════════════════════════════════════

public class IngestResultViewModel
{
    public bool    Success           { get; set; }
    public string? Error             { get; set; }
    public int     RunId             { get; set; }
    public int     MessagesLoaded    { get; set; }
    public int     NewSenders        { get; set; }
    public int     UpdatedSenders    { get; set; }
    public int     SkippedDuplicates { get; set; }
    public string? SourceFile        { get; set; }
    public string  StartedAt         { get; set; } = string.Empty;
    public string  CompletedAt       { get; set; } = string.Empty;
}

// ════════════════════════════════════════════════════════════════
//  RATING ViewModels
// ════════════════════════════════════════════════════════════════

public class RatingOptionViewModel
{
    public int     RatingId   { get; set; }
    public string  RatingName { get; set; } = string.Empty;
    public int     SortOrder  { get; set; }
    public string? ColorCode  { get; set; }
}

// ════════════════════════════════════════════════════════════════
//  DASHBOARD ViewModel
// ════════════════════════════════════════════════════════════════

public class DashboardViewModel
{
    public int  TotalRuns     { get; set; }
    public int  TotalMessages { get; set; }
    public int  TotalSenders  { get; set; }
    public int  Page          { get; set; } = 1;
    public int  PageSize      { get; set; } = 20;
    public bool SortAsc       { get; set; }
    public int  TotalPages    => (int)Math.Ceiling((double)TotalSenders / PageSize);
    public bool HasPrev       => Page > 1;
    public bool HasNext       => Page < TotalPages;

    public RunSummaryViewModel?                MostRecentRun   { get; set; }
    public IEnumerable<SenderSummaryViewModel> TopSenders      { get; set; } = [];
    public IEnumerable<RatingOptionViewModel>  RatingBreakdown { get; set; } = [];
}
