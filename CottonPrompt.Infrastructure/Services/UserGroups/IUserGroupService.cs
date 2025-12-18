using CottonPrompt.Infrastructure.Models.UserGroups;

namespace CottonPrompt.Infrastructure.Services.UserGroups
{
    public interface IUserGroupService
    {
        Task<IEnumerable<GetUserGroupsModel>> GetAsync();

        Task<GetUserGroupModel> GetByIdAsync(int id);

        Task CreateAsync(string name, IEnumerable<Guid> userIds, Guid createdBy);

        Task UpdateAsync(int id, string name, IEnumerable<Guid> userIds, Guid updatedBy);

        Task RemoveUserAsync(int id, Guid userId);

        Task<IEnumerable<GetUserGroupsModel>> GetArtistGroupsAsync();

        Task<GetUserGroupModel> GetConceptAuthorsAsync();

        Task DeleteAsync(int id);
    }
}
