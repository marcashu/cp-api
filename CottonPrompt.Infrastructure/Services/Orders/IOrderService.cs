using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models;
using CottonPrompt.Infrastructure.Models.Orders;

namespace CottonPrompt.Infrastructure.Services.Orders
{
    public interface IOrderService
    {
        Task<IEnumerable<GetOrdersModel>> GetAsync(bool? priority, string? artistStatus, string? checkerStatus, string? customerStatus, Guid? artistId, Guid? checkerId, bool noArtist = false, bool noChecker = false);

        Task<IEnumerable<GetOrdersModel>> GetOngoingAsync(OrderFiltersModel? filters = null);

        Task<PaginatedResult<GetOrdersModel>> GetRejectedAsync(OrderFiltersModel? filters = null);

        Task<IEnumerable<GetOrdersModel>> GetRejectedFilterOptionsAsync();

        Task<IEnumerable<GetOrdersModel>> GetCompletedAsync(OrderFiltersModel? filters = null);

        Task<IEnumerable<GetOrdersModel>> GetReportedAsync(OrderFiltersModel? filters = null);

        Task<PaginatedResult<GetOrdersModel>> GetSentForPrintingAsync(OrderFiltersModel? filters = null);

        Task<IEnumerable<GetOrdersModel>> GetSentForPrintingFilterOptionsAsync();

        Task<IEnumerable<GetOrdersModel>> GetAllAsync(OrderFiltersModel? filters = null);

        Task<IEnumerable<GetOrdersModel>> GetAvailableAsArtistAsync(Guid artistId, bool? priority, bool changeRequest = false);

        Task<IEnumerable<GetOrdersModel>> GetAvailableAsCheckerAsync(bool? priority, bool trainingGroup = false);

        Task<GetOrderModel> GetByIdAsync(int id);

        Task CreateAsync(Order order);

        Task UpdateAsync(Order order);

        Task DeleteAsync(int id);

        Task<CanDoModel> AssignArtistAsync(int id, Guid artistId);

        Task<CanDoModel> AssignCheckerAsync(int id, Guid checkerId);

        Task SubmitDesignAsync(int id, string designName, string designContent);

        Task ApproveAsync(int id, Guid? approvedBy = null, bool isAdminApproval = false);

        Task AcceptAsync(int id, Guid? userId);

        Task ChangeRequestAsync(int id, int designId, string comment, IEnumerable<OrderImageReference> imageReferences);

        Task<DownloadModel> DownloadAsync(int id);

        Task ResendForCustomerReviewAsync(int id);

        Task ReportAsync(int id, string reason, bool isRedraw);

        Task ResolveAsync(int id, Guid resolvedBy);

        Task SendForPrintingAsync(int id, Guid userId);

        Task RedrawAsync(Order order, int changeRequestOrderId);

        Task ToggleRedrawMarkAsync(int id);

        Task<IEnumerable<GetOrdersModel>> SearchAsync(string orderNumber);

        Task<int> CleanupOldOrdersAsync(int olderThanDays = 30);
    }
}
