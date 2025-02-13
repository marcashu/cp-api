using GraphUser = Microsoft.Graph.Models.User;
using UserEntity = CottonPrompt.Infrastructure.Entities.User;
using CottonPrompt.Infrastructure.Models.Users;

namespace CottonPrompt.Infrastructure.Extensions
{
    internal static class UserExtensions
    {
        internal static GetUsersModel AsModel(this GraphUser graphUser)
        {
            var result = new GetUsersModel(Guid.Parse(graphUser.Id ?? string.Empty), graphUser.DisplayName ?? string.Empty, graphUser.UserPrincipalName ?? string.Empty, Enumerable.Empty<string>(), "");
            return result;
        }

        internal static IEnumerable<GetUsersModel> AsModel(this IEnumerable<GraphUser> graphUsers)
        {
            var result = graphUsers.Select(AsModel);
            return result;
        }
        
        internal static GetUsersModel AsModel(this UserEntity entity)
        {
            var result = new GetUsersModel(entity.Id, entity.Name, entity.Email, entity.UserRoles.Select(ur => ur.Role), entity.PaymentLink);
            return result;
        }

        internal static IEnumerable<GetUsersModel> AsModel(this IEnumerable<UserEntity> entities)
        {
            var result = entities.Select(AsModel);
            return result;
        }
    }
}
