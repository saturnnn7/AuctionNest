using AuctionNest.Application.Features.Categories.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery : IRequest<Result<List<CategoryDto>>>;