using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Features.AuditLogs.Queries;

namespace Security.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class AuditController(IMediator mediator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, string? action = null)
    {
        var result = await mediator.Send(new GetAuditLogsQuery(page, 20, search, action));
        ViewBag.Search = search;
        ViewBag.Action = action;
        return View(result);
    }
}
