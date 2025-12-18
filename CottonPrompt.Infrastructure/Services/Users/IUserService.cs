using CottonPrompt.Infrastructure.Models;
using CottonPrompt.Infrastructure.Models.Users;

namespace CottonPrompt.Infrastructure.Services.Users
{
    public interface IUserService
    {
        Task<GetUsersModel> LoginAsync(Guid id, string name, string email);

        Task AddAsync(Guid id, string name, string email, IEnumerable<string> roles, Guid createdBy);

        Task<IEnumerable<GetUsersModel>> GetRegisteredAsync();

        Task<IEnumerable<GetUsersModel>> GetUnregisteredAsync();

        Task UpdateRoleAsync(Guid id, Guid updatedBy, IEnumerable<string> roles);

        Task<CanDoModel> CanUpdateRoleAsync(Guid id, IEnumerable<string> roles);

        Task<CanDoModel> CheckerHasWaitingForCustomerAsync(Guid id);

        Task<IEnumerable<GetUsersModel>> GetNotMemberOfGroupAsync(int userGroupId);

        Task AddPaymentLinkAsync(Guid userId, String paymentLink);
    }
}
