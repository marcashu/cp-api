using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Extensions;
using CottonPrompt.Infrastructure.Models.UserGroups;
using Microsoft.EntityFrameworkCore;

namespace CottonPrompt.Infrastructure.Services.UserGroups
{
    public class UserGroupService(CottonPromptContext dbContext) : IUserGroupService
    {
        public async Task CreateAsync(string name, IEnumerable<Guid> userIds, Guid createdBy)
        {
            try
            {
                var userGroup = new UserGroup
                {
                    Name = name,
                    CreatedBy = createdBy,
                };

                await dbContext.UserGroups.AddAsync(userGroup);
                await dbContext.SaveChangesAsync();

                if (!userIds.Any()) return;

                foreach (var userId in userIds)
                {
                    var userGroupUser = new UserGroupUser
                    {
                        UserGroupId = userGroup.Id,
                        UserId = userId,
                        CreatedBy = createdBy,
                    };

                    await dbContext.UserGroupUsers.AddAsync(userGroupUser);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetUserGroupsModel>> GetAsync()
        {
            try
            {
                var userGroups = await dbContext.UserGroups
                    .Where(ug => !ug.IsDeleted)
                    .Include(ug => ug.UserGroupUsers)
                    .OrderBy(ug => ug.Name)
                    .ToListAsync();
                var result = userGroups.AsModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<GetUserGroupModel> GetByIdAsync(int id)
        {
            try
            {
                var userGroup = await dbContext.UserGroups
                    .Include(ug => ug.UserGroupUsers.OrderBy(ugu => ugu.User.Name))
                    .ThenInclude(ugu => ugu.User)
                    .ThenInclude(u => u.UserRoles.OrderBy(ur => ur.SortOrder))
                    .SingleAsync(ug => ug.Id == id);
                var result = userGroup.AsGetUserGroupModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task RemoveUserAsync(int id, Guid userId)
        {
            try
            {
                await dbContext.UserGroupUsers
                    .Where(ugu => ugu.UserGroupId == id && ugu.UserId == userId)
                    .ExecuteDeleteAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateAsync(int id, string name, IEnumerable<Guid> userIds, Guid updatedBy)
        {
            try
            {
                var userGroup = await dbContext.UserGroups.Include(ug => ug.UserGroupUsers).SingleOrDefaultAsync(ug => ug.Id == id);

                if (userGroup is null) return;

                userGroup.Name = name;
                userGroup.UpdatedBy = updatedBy;
                userGroup.UpdatedOn = DateTime.UtcNow;

                var currentUserIds = userGroup.UserGroupUsers.Select(ugu => ugu.UserId).ToList();
                var newUserIds = userIds.Except(currentUserIds).ToList();

                foreach (var userId in newUserIds)
                {
                    var userGroupUser = new UserGroupUser
                    {
                        UserGroupId = userGroup.Id,
                        UserId = userId,
                        CreatedBy = updatedBy,
                    };

                    userGroup.UserGroupUsers.Add(userGroupUser);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetUserGroupsModel>> GetArtistGroupsAsync()
        {
            try
            {
                var traingGroupCheckersId = await dbContext.Settings.Select(s => s.TrainingGroupCheckersGroupId).FirstOrDefaultAsync();
                var userGroups = await dbContext.UserGroups
                    .Where(ug => !ug.IsDeleted && ug.Id != traingGroupCheckersId)
                    .Include(ug => ug.UserGroupUsers)
                    .OrderBy(ug => ug.Name)
                    .ToListAsync();
                var result = userGroups.AsModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<GetUserGroupModel> GetConceptAuthorsAsync()
        {
            try
            {
                var conceptAuthorsGroupId = await dbContext.Settings.Select(s => s.ConceptAuthorGroupId).FirstOrDefaultAsync();
                var userGroup = await dbContext.UserGroups
                    .Include(ug => ug.UserGroupUsers.OrderBy(ugu => ugu.User.Name))
                    .ThenInclude(ugu => ugu.User)
                    .ThenInclude(u => u.UserRoles.OrderBy(ur => ur.SortOrder))
                    .SingleOrDefaultAsync(ug => ug.Id == conceptAuthorsGroupId);

                if (userGroup is null)
                {
                    return new GetUserGroupModel(0, "Concept Authors", []);
                }

                var result = userGroup.AsGetUserGroupModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var userGroup = await dbContext.UserGroups.SingleOrDefaultAsync(ug => ug.Id == id);

                if (userGroup is null) return;

                userGroup.IsDeleted = true;
                userGroup.UpdatedOn = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
