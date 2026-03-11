namespace EmailTracker.ViewModels;

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
    public string? Subject        { get; set; }
    public string? Snippet        { get; set; }
    public string? InternalDate   { get; set; }
    public string? FromRaw        { get; set; }
    public string? GmailMessageId { get; set; }
    public string? ThreadId       { get; set; }

    public string DisplayDate =>
        DateTime.TryParse(InternalDate, out var dt) ? dt.ToString("MMM d, yyyy h:mm tt") : (InternalDate ?? "—");
}

public class MessageSearchViewModel
{
    // ── Filter inputs (bound from AJAX request) ──────────────────
    public string? SearchTerm   { get; set; }
    public int?    RunId        { get; set; }
    public int?    SenderId     { get; set; }
    public string? DateFrom     { get; set; }
    public string? DateTo       { get; set; }
    public string? RatingFilter { get; set; }
    public int     Page         { get; set; } = 1;
    public int     PageSize     { get; set; } = 50;

    // ── Results ──────────────────────────────────────────────────
    public IEnumerable<MessageRowViewModel> Messages    { get; set; } = [];
    public int                              TotalCount  { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // ── Filter options for dropdowns ─────────────────────────────
    public IEnumerable<RunSummaryViewModel>    AvailableRuns    { get; set; } = [];
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
    public int     RatingId      { get; set; }
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
    public int      Page         { get; set; } = 1;
    public int      PageSize     { get; set; } = 50;

    public IEnumerable<SenderSummaryViewModel> Senders      { get; set; } = [];
    public IEnumerable<RatingOptionViewModel>  AvailableRatings { get; set; } = [];
    public int                                 TotalCount   { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class FromRawSummaryViewModel
{
    public string FromRaw  { get; set; } = string.Empty;
    public int    MsgCount { get; set; }
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

    public IEnumerable<MessageRowViewModel>     RecentMessages    { get; set; } = [];
    public IEnumerable<RatingOptionViewModel>   AvailableRatings  { get; set; } = [];
    public IEnumerable<FromRawSummaryViewModel> FromRawBreakdown  { get; set; } = [];
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
//  RATING ViewModels
// ════════════════════════════════════════════════════════════════

public class RatingOptionViewModel
{
    public int    RatingId   { get; set; }
    public string RatingName { get; set; } = string.Empty;
    public int    SortOrder  { get; set; }
}

// ════════════════════════════════════════════════════════════════
//  DASHBOARD ViewModel
// ════════════════════════════════════════════════════════════════

public class DashboardViewModel
{
    public int TotalRuns     { get; set; }
    public int TotalMessages { get; set; }
    public int TotalSenders  { get; set; }

    public RunSummaryViewModel?                MostRecentRun   { get; set; }
    public IEnumerable<SenderSummaryViewModel> TopSenders      { get; set; } = [];
    public IEnumerable<RatingOptionViewModel>  RatingBreakdown { get; set; } = [];
}
