using AuctionNest.Application.Common.Interfaces;
using AuctionNest.Application.Features.Categories.Common;
using AuctionNest.Domain.Common;
using MediatR;

namespace AuctionNest.Application.Features.Categories.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler
    : IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<List<CategoryDto>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.GetAllActiveAsync(cancellationToken);
        return Result.Success(categories.Select(CategoryDto.FromEntity).ToList());
    }
}