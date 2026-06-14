using AuctionNest.API.Common;
using AuctionNest.Application.Features.Categories.Common;
using AuctionNest.Application.Features.Categories.Queries.GetCategories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuctionNest.API.Controllers;

[Route("api/categories")]
public sealed class CategoriesController : BaseController
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => HandleResult(await _sender.Send(new GetCategoriesQuery(), ct));
}