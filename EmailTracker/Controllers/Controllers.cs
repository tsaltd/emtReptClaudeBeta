using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EmailTracker.Data;
using EmailTracker.Services;
using EmailTracker.ViewModels;

namespace EmailTracker.Controllers;

// ── Home / Dashboard ─────────────────────────────────────────────
public class HomeController : Controller
{
    private readonly IRunService    _runService;
    private readonly ISenderService _senderService;
    private readonly IRatingService _ratingService;

    public HomeController(IRunService runService, ISenderService senderService, IRatingService ratingService)
    {
        _runService    = runService;
        _senderService = senderService;
        _ratingService = ratingService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, bool sortAsc = false)
    {
        var runList    = await _runService.GetRunListAsync(null);
        var senderList = await _senderService.SearchAsync(new SenderSearchViewModel
        {
            Page     = page,
            PageSize = pageSize,
            SortAsc  = sortAsc
        });
        var ratings = await _ratingService.GetAllAsync();

        var vm = new DashboardViewModel
        {
            TotalRuns       = runList.TotalRuns,
            TotalMessages   = runList.Runs.Sum(r => r.MessageCount),
            TotalSenders    = senderList.TotalCount,
            MostRecentRun   = runList.Runs.FirstOrDefault(),
            TopSenders      = senderList.Senders,
            RatingBreakdown = ratings,
            Page            = page,
            PageSize        = pageSize,
            SortAsc         = sortAsc
        };

        return View(vm);
    }
}

// ── Run Controller ───────────────────────────────────────────────
public class RunController : Controller
{
    private readonly IRunService _runService;

    public RunController(IRunService runService) => _runService = runService;

    // GET /Run  — full page (initial load)
    public async Task<IActionResult> Index(string? search)
    {
        var vm = await _runService.GetRunListAsync(search);
        return View(vm);
    }

    // GET /Run/Rows?search=...  — AJAX partial refresh
    [HttpGet]
    public async Task<IActionResult> Rows(string? search)
    {
        var vm = await _runService.GetRunListAsync(search);
        return PartialView("_RunRows", vm);
    }

    // GET /Run/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _runService.GetRunDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }
}

// ── Message Controller ───────────────────────────────────────────
public class MessageController : Controller
{
    private readonly IMessageService _messageService;
    private readonly ISenderService  _senderService;

    public MessageController(IMessageService messageService, ISenderService senderService)
    {
        _messageService = messageService;
        _senderService  = senderService;
    }

    // GET /Message  — full page
    public async Task<IActionResult> Index(MessageSearchViewModel? filters)
    {
        filters ??= new MessageSearchViewModel();
        var vm = await _messageService.SearchAsync(filters);
        return View(vm);
    }

    // GET /Message/Rows  — AJAX table refresh
    [HttpGet]
    public async Task<IActionResult> Rows(
        string? searchTerm,
        int?    runId,
        int?    senderId,
        string? dateFrom,
        string? dateTo,
        string? ratingFilter,
        int     page = 1,
        int     pageSize = 50)
    {
        var filters = new MessageSearchViewModel
        {
            SearchTerm   = searchTerm,
            RunId        = runId,
            SenderId     = senderId,
            DateFrom     = dateFrom,
            DateTo       = dateTo,
            RatingFilter = ratingFilter,
            Page         = page,
            PageSize     = pageSize
        };

        var vm = await _messageService.SearchAsync(filters);
        return PartialView("_MessageRows", vm);
    }

    // GET /Message/Browse?senderId=90  — sender-panel navigator view
    public async Task<IActionResult> Browse(
        int     senderId,
        string? senderSearch,
        bool    senderSortAsc = false,
        int     senderPage    = 0,
        int     msgPage       = 1)
    {
        var vm = await BuildSenderNav(senderId, senderSearch, senderSortAsc, senderPage);

        var msgResult = await _messageService.SearchAsync(new MessageSearchViewModel
        {
            SenderId = senderId,
            Page     = msgPage,
            PageSize = 50
        });

        vm.Messages = msgResult.Messages;
        vm.MsgTotal = msgResult.TotalCount;
        vm.MsgPage  = msgPage;

        // Fallback: get sender info from messages if not in filtered list
        if (vm.EmailAddress == string.Empty)
        {
            var first = msgResult.Messages.FirstOrDefault();
            vm.EmailAddress = first?.EmailAddress ?? $"Sender #{senderId}";
            vm.RatingName   = first?.RatingName   ?? string.Empty;
        }

        return View(vm);
    }

    // GET /Message/BrowsePanel — AJAX: refresh sender panel content only
    [HttpGet]
    public async Task<IActionResult> BrowsePanel(
        int     senderId,
        string? senderSearch,
        bool    senderSortAsc = false,
        int     senderPage    = 1)
    {
        var vm = await BuildSenderNav(senderId, senderSearch, senderSortAsc, senderPage);
        return PartialView("_BrowseSenderPanel", vm);
    }

    // GET /Message/Grouped — CEA-paged tree view
    public async Task<IActionResult> Grouped(
        string? searchTerm,
        string  sortBy   = "count",
        bool    sortAsc  = false,
        int     page     = 1,
        int     pageSize = 10)
    {
        List<SenderSummaryViewModel> pageSenders;
        int total;

        if (sortBy == "email")
        {
            // Load all matching senders, sort by email address in memory
            var all = await _senderService.SearchAsync(new SenderSearchViewModel
            {
                SearchTerm = searchTerm,
                SortAsc    = false,
                Page       = 1,
                PageSize   = int.MaxValue
            });
            var sorted = (sortAsc
                ? all.Senders.OrderBy(s => s.EmailAddress)
                : all.Senders.OrderByDescending(s => s.EmailAddress)).ToList();
            total       = sorted.Count;
            pageSenders = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else
        {
            var result = await _senderService.SearchAsync(new SenderSearchViewModel
            {
                SearchTerm = searchTerm,
                SortAsc    = sortAsc,
                Page       = page,
                PageSize   = pageSize
            });
            total       = result.TotalCount;
            pageSenders = result.Senders.ToList();
        }

        // Build CEA groups — one lightweight message fetch per sender
        var groups = new List<CeaGroupViewModel>();
        foreach (var s in pageSenders)
        {
            var msgs = await _messageService.GetForSenderAsync(s.SenderId, 200);

            var fromRawGroups = msgs
                .GroupBy(m => m.FromRaw ?? "(no from header)")
                .OrderByDescending(g => g.Count())
                .Select(g => new FromRawGroupViewModel
                {
                    FromRaw  = g.Key,
                    Messages = g.OrderByDescending(m => m.InternalDate).ToList()
                }).ToList();

            groups.Add(new CeaGroupViewModel
            {
                SenderId      = s.SenderId,
                EmailAddress  = s.EmailAddress,
                RatingName    = s.RatingName,
                MsgCount      = s.MsgCount,
                FromRawGroups = fromRawGroups
            });
        }

        var vm = new MessageGroupedViewModel
        {
            SearchTerm   = searchTerm,
            SortBy       = sortBy,
            SortAsc      = sortAsc,
            Page         = page,
            PageSize     = pageSize,
            TotalSenders = total,
            CeaGroups    = groups
        };

        return View(vm);
    }

    private async Task<MessageBrowseViewModel> BuildSenderNav(
        int senderId, string? senderSearch, bool senderSortAsc, int senderPage)
    {
        const int pageSize = 10;

        var result = await _senderService.SearchAsync(new SenderSearchViewModel
        {
            SearchTerm = senderSearch,
            SortAsc    = senderSortAsc,
            Page       = 1,
            PageSize   = int.MaxValue
        });

        var list  = result.Senders.ToList();
        int total = list.Count;
        int ri    = list.FindIndex(s => s.SenderId == senderId);

        int page = senderPage > 0 ? senderPage
                 : ri >= 0        ? (ri / pageSize) + 1
                 : 1;
        int maxPage = Math.Max(1, (int)Math.Ceiling((double)total / pageSize));
        page = Math.Clamp(page, 1, maxPage);

        var cur = ri >= 0 ? list[ri] : null;

        return new MessageBrowseViewModel
        {
            SenderId         = senderId,
            EmailAddress     = cur?.EmailAddress ?? string.Empty,
            RatingName       = cur?.RatingName   ?? string.Empty,
            SenderSearch     = senderSearch,
            SenderSortAsc    = senderSortAsc,
            SenderPage       = page,
            SenderPageSize   = pageSize,
            SenderTotal      = total,
            SenderRank       = ri >= 0 ? ri + 1 : 0,
            SenderList       = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            PrevSenderId     = ri > 0          ? list[ri - 1].SenderId  : null,
            NextSenderId     = ri < total - 1  ? list[ri + 1].SenderId  : null,
            Prev10SenderId   = ri >= 10        ? list[ri - 10].SenderId : null,
            Next10SenderId   = ri + 10 < total ? list[ri + 10].SenderId : null,
            AvailableRatings = result.AvailableRatings,
        };
    }
}

// ── Sender Controller ────────────────────────────────────────────
public class SenderController : Controller
{
    private readonly ISenderService _senderService;

    public SenderController(ISenderService senderService) => _senderService = senderService;

    // GET /Sender  — full page
    public async Task<IActionResult> Index(SenderSearchViewModel? filters)
    {
        filters ??= new SenderSearchViewModel();
        var vm = await _senderService.SearchAsync(filters);
        return View(vm);
    }

    // GET /Sender/Rows  — AJAX table refresh
    [HttpGet]
    public async Task<IActionResult> Rows(
        string? searchTerm,
        string? ratingFilter,
        int     page = 1,
        int     pageSize = 50)
    {
        var filters = new SenderSearchViewModel
        {
            SearchTerm   = searchTerm,
            RatingFilter = ratingFilter,
            Page         = page,
            PageSize     = pageSize
        };

        var vm = await _senderService.SearchAsync(filters);
        return PartialView("_SenderRows", vm);
    }

    // GET /Sender/QuickRate
    public async Task<IActionResult> QuickRate(string? searchTerm, string? ratingFilter, int page = 1)
    {
        var vm = await _senderService.SearchAsync(new SenderSearchViewModel
        {
            SearchTerm   = searchTerm,
            RatingFilter = ratingFilter,
            Page         = page,
            PageSize     = 60
        });
        return View(vm);
    }

    // POST /Sender/UpdateRatingBulk
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRatingBulk([FromBody] UpdateRatingBulkRequest req)
    {
        if (req.SenderIds == null || !req.SenderIds.Any())
            return Json(new { success = false, message = "No senders selected." });

        var ratingName = await _senderService.UpdateRatingBulkAsync(req.SenderIds, req.RatingId);
        if (ratingName == null)
            return Json(new { success = false, message = "Rating not found." });

        return Json(new { success = true, ratingName, count = req.SenderIds.Count() });
    }

    // GET /Sender/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _senderService.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // GET /Sender/Summary/5
    public async Task<IActionResult> Summary(int id, int page = 1, int pageSize = 10, bool sortAsc = true)
    {
        var vm = await _senderService.GetSummaryAsync(id, page, pageSize, sortAsc);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── Browse (single-record navigator) ────────────────────────

    // GET /Sender/Browse?index=0
    // Full page on first load; AJAX partial on nav button clicks
    public async Task<IActionResult> Browse(int index = 0, bool partial = false)
    {
        var vm = await _senderService.GetBrowseAsync(index);
        if (vm == null) return NotFound();

        if (partial)
            return PartialView("_BrowseCard", vm);

        return View(vm);
    }

    // POST /Sender/UpdateRating
    // AJAX only — returns JSON { success, ratingName }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRating([FromBody] UpdateRatingRequest req)
    {
        var ratingName = await _senderService.UpdateRatingAsync(req.SenderId, req.RatingId);
        if (ratingName == null)
            return Json(new { success = false, message = "Sender or rating not found." });

        return Json(new { success = true, ratingName });
    }
}

// ── Gmail Ingest Controller ───────────────────────────────────────
public class GmailIngestController : Controller
{
    private readonly IngestJobStore                    _jobStore;
    private readonly IServiceScopeFactory              _scopeFactory;
    private readonly ILogger<GmailIngestController>   _logger;

    public GmailIngestController(
        IngestJobStore jobStore,
        IServiceScopeFactory scopeFactory,
        ILogger<GmailIngestController> logger)
    {
        _jobStore     = jobStore;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    // GET /GmailIngest
    public IActionResult Index() => View();

    // POST /GmailIngest/Start — fires job, returns jobId immediately
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Start([FromForm] int days = 7)
    {
        days = Math.Clamp(days, 1, 7);

        var job = _jobStore.Create();
        job.Status = "running";

        _logger.LogInformation("GmailIngest: job {JobId} created, launching ingest for {Days} days", job.JobId, days);

        // Fire and forget — use Task.Run with ConfigureAwait to avoid sync context issues
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IGmailIngestService>();
                job.Result = await svc.RunAsync(days, step => job.Step = step).ConfigureAwait(false);
                job.Status = "complete";
                _logger.LogInformation("GmailIngest: job {JobId} complete — {Count} messages loaded", job.JobId, job.Result?.MessagesLoaded);
            }
            catch (Exception ex)
            {
                job.Error  = ex.Message;
                job.Status = "failed";
                _logger.LogError(ex, "GmailIngest: job {JobId} failed", job.JobId);
            }
        });

        return Json(new { jobId = job.JobId });
    }

    // GET /GmailIngest/Status/{id} — polled by browser every 3s
    [HttpGet]
    public IActionResult Status(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("GmailIngest: Status called with empty jobId");
            return Json(new { status = "not_found" });
        }

        var job = _jobStore.Get(id);
        if (job != null)
        {
            _logger.LogDebug("GmailIngest: Status poll {JobId} — status={Status}", id, job.Status);
            return Json(new
            {
                status    = job.Status,
                step      = job.Step,
                result    = job.Result,
                error     = job.Error,
                elapsedMs = (long)(DateTime.UtcNow - job.StartedAt).TotalMilliseconds
            });
        }

        // Job not in memory — app may have restarted mid-ingest.
        // Check for a Run completed in the last 10 minutes as a fallback.
        _logger.LogWarning("GmailIngest: job {JobId} not found in store — checking DB fallback", id);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cutoff = DateTime.UtcNow.AddMinutes(-10).ToString("o");
        var recentRun = db.Runs
            .Where(r => r.StartedAt != null && string.Compare(r.StartedAt, cutoff) >= 0)
            .OrderByDescending(r => r.RunId)
            .FirstOrDefault();

        if (recentRun == null)
        {
            _logger.LogWarning("GmailIngest: DB fallback found no recent run — returning not_found");
            return Json(new { status = "not_found" });
        }

        var msgCount    = db.Messages.Count(m => m.RunId == recentRun.RunId);
        var newSenders  = db.Senders.Count(s => s.CreatedAt != null
                              && string.Compare(s.CreatedAt, recentRun.StartedAt) >= 0);

        _logger.LogInformation("GmailIngest: DB fallback returning run {RunId} — {Msgs} msgs, {New} new senders", recentRun.RunId, msgCount, newSenders);

        var result = new IngestResultViewModel
        {
            Success        = true,
            RunId          = recentRun.RunId,
            MessagesLoaded = msgCount,
            NewSenders     = newSenders,
            SourceFile     = recentRun.SourceLabel ?? string.Empty,
            StartedAt      = recentRun.StartedAt,
            CompletedAt    = recentRun.WindowEnd
        };

        return Json(new { status = "complete", result });
    }
}

// ── Request models ────────────────────────────────────────────────
public class UpdateRatingRequest
{
    public int SenderId { get; set; }
    public int RatingId { get; set; }
}

public class UpdateRatingBulkRequest
{
    public IEnumerable<int> SenderIds { get; set; } = [];
    public int RatingId { get; set; }
}
