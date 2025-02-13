using CottonPrompt.Infrastructure.Constants;
using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Extensions;
using CottonPrompt.Infrastructure.Models;
using CottonPrompt.Infrastructure.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace CottonPrompt.Infrastructure.Services.Users
{
    public class UserService(CottonPromptContext dbContext, IServiceProvider serviceProvider) : IUserService
    {
        public async Task<CanDoModel> CanUpdateRoleAsync(Guid id, IEnumerable<string> roles)
        {
			try
			{
				var userRoles = await dbContext.UserRoles.Where(ur => ur.UserId == id).Select(ur => ur.Role).ToListAsync();
				var rolesToRemove = userRoles.Except(roles);
				var rolesToAdd = roles.Except(userRoles);

				if (rolesToRemove.Contains(UserRoles.Checker.GetDisplayName()))
                {
                    var hasPendingOrdersAsChecker = await dbContext.Orders.AnyAsync(o => o.CheckerId == id && o.CheckerStatus != OrderStatuses.Approved);

                    if (hasPendingOrdersAsChecker)
                    {
                        return new CanDoModel(false, "Can't remove the Checker role because the user still has orders to complete as Checker.");
                    }
                }

                if (rolesToRemove.Contains(UserRoles.Artist.GetDisplayName()))
                {
                    var hasPendingOrdersAsArtist = await dbContext.Orders.AnyAsync(o => o.ArtistId == id && o.CheckerStatus != OrderStatuses.Approved);

                    if (hasPendingOrdersAsArtist)
                    {
                        return new CanDoModel(false, "Can't remove the Artist role because the user still has orders to complete as Artist.");
                    }
                }

                return new CanDoModel(true, string.Empty);
            }
			catch (Exception)
			{
				throw;
			}
        }

        public async Task<IEnumerable<GetUsersModel>> GetUnregisteredAsync()
        {
			try
            {
                var result = Enumerable.Empty<GetUsersModel>();
                
				var graphClient = serviceProvider.GetRequiredService<GraphServiceClient>();
				var msUsersResponse = await graphClient.Users.GetAsync((config) =>
				{
					config.QueryParameters.Top = 500;
					config.QueryParameters.Orderby = ["displayName"];
                });

                if (msUsersResponse is null || msUsersResponse.Value is null) return result;

				var msUsers = msUsersResponse.Value;
                var dbUserIds = await dbContext.Users.OrderBy(u => u.Name).Select(u => u.Id.ToString().ToLower()).ToListAsync();

				result = msUsers.Where(u => !dbUserIds.Contains(u.Id?.ToLower() ?? string.Empty)).AsModel();

                return result;
			}
			catch (Exception)
			{
				throw;
			}
        }

        public async Task<IEnumerable<GetUsersModel>> GetRegisteredAsync()
        {
			try
			{
				var users = await dbContext.Users.Include(u => u.UserRoles.OrderBy(ur => ur.SortOrder)).OrderBy(u => u.Name).ToListAsync();
				var result = users.AsModel();
				return result;
			}
			catch (Exception)
			{
				throw;
			}
        }

        public async Task<GetUsersModel> LoginAsync(Guid id, string name, string email)
        {
			try
			{
				var user = await dbContext.Users.Include(u => u.UserRoles.OrderBy(ur => ur.SortOrder)).SingleOrDefaultAsync(u => u.Id == id);

				if (user == null)
				{
                    return new GetUsersModel(id, name, email, [], "");
                }
				else
				{
					user.LastLoggedOn = DateTime.UtcNow;
					user.UpdatedOn = DateTime.UtcNow;
					user.UpdatedBy = id;
                    await dbContext.SaveChangesAsync();

                    var result = user.AsModel();
                    return result;
                }
			}
			catch (Exception)
			{
				throw;
			}
        }

        public async Task UpdateRoleAsync(Guid id, Guid updatedBy, IEnumerable<string> roles)
        {
			try
			{
				await dbContext.UserRoles.Where(ur => ur.UserId == id).ExecuteDeleteAsync();

				foreach (var role in roles)
				{
					var newRole = new UserRole
					{
						UserId = id,
						Role = role,
						SortOrder = (int)EnumExtensions.GetEnumFromName<UserRoles>(role),
						CreatedBy = updatedBy,
					};

					await dbContext.UserRoles.AddAsync(newRole);
				}

				await dbContext.SaveChangesAsync();
			}
			catch (Exception)
			{
				throw;
			}
        }

        public async Task AddAsync(Guid id, string name, string email, IEnumerable<string> roles, Guid createdBy)
        {
			try
			{
                var user = new User
                {
                    Id = id,
                    Name = name,
                    Email = email,
                    CreatedBy = createdBy,
                    UserRoles = roles.Select(r => new UserRole
                    {
                        Role = r,
                        SortOrder = (int)EnumExtensions.GetEnumFromName<UserRoles>(r),
                        CreatedBy = createdBy,
                    }).ToList()
                };

                await dbContext.Users.AddAsync(user);
				await dbContext.SaveChangesAsync();
			}
			catch (Exception)
			{
				throw;
			}
        }

        public async Task<CanDoModel> CheckerHasWaitingForCustomerAsync(Guid id)
        {
			try
			{
                var hasWaitingForCustomer = await dbContext.Orders.AnyAsync(o => o.CheckerId == id && o.CheckerStatus == OrderStatuses.Approved && o.CustomerStatus == OrderStatuses.ForReview && o.OriginalOrderId != null);
				return new CanDoModel(hasWaitingForCustomer, string.Empty);
            }
			catch (Exception)
			{
				throw;
			}
        }

        public async Task<IEnumerable<GetUsersModel>> GetNotMemberOfGroupAsync(int userGroupId)
        {
			try
			{
				var users = await dbContext.Users
					.Where(u => !u.UserGroupUsers.Any(ugu => ugu.UserGroupId == userGroupId))
					.OrderBy(u => u.Name)
					.ToListAsync();
				var result = users.AsModel();
				return result;
			}
			catch (Exception)
			{
				throw;
			}
        }

		public async Task AddPaymentLinkAsync(Guid userId, String paymentLink)
		{
			try
			{
				var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId);
				if (user == null) throw new Exception("User not found");

				user.PaymentLink = paymentLink;
				await dbContext.SaveChangesAsync();
			}
			catch (Exception)
			{
				throw;
			}
		}
    }
}
