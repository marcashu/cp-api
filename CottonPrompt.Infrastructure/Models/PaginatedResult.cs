namespace CottonPrompt.Infrastructure.Models
{
    public record PaginatedResult<T>(
        IEnumerable<T> Items,
        int TotalCount,
        int Page,
        int PageSize
    )
    {
        public int TotalPages { get; init; } = (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage { get; init; } = Page < (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage { get; init; } = Page > 1;
    }
}
