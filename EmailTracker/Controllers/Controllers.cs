using Microsoft.AspNetCore.Mvc;
using EmailTracker.Services;
using EmailTracker.ViewModels;

namespace EmailTracker.Controllers;

// ── Home / Dashboard ─────────────────────────────────────────────
public class HomeController : Controller
{
    private readonly IRunService    _runService;
    private readonly ISenderService _senderService;

    public HomeController(IRunService runService, ISenderService senderService)
    {
        _runService    = runService;
        _senderService = senderService;
    }

    public async Task<IActionResult> Index()
    {
        var runList    = await _runService.GetRunListAsync(null);
        var senderList = await _senderService.SearchAsync(new SenderSearchViewModel { PageSize = 10 });

        var vm = new DashboardViewModel
        {
            TotalRuns     = runList.TotalRuns,
            TotalMessages = runList.Runs.Sum(r => r.MessageCount),
            TotalSenders  = senderList.TotalCount,
            MostRecentRun = runList.Runs.FirstOrDefault(),
            TopSenders    = senderList.Senders.Take(10)
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

    public MessageController(IMessageService messageService) => _messageService = messageService;

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

    // GET /Sender/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _senderService.GetDetailAsync(id);
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

// ── Request model for UpdateRating ───────────────────────────────
public class UpdateRatingRequest
{
    public int SenderId { get; set; }
    public int RatingId { get; set; }
}
