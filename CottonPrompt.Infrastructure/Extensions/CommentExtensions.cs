using CottonPrompt.Infrastructure.Constants;
using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models.Comments;

namespace CottonPrompt.Infrastructure.Extensions
{
    internal static class CommentExtensions
    {
        internal static CommentModel AsModel(this OrderDesignComment entity)
        {
            var result = new CommentModel(entity.Comment, entity.CreatedBy != Guid.Empty ? UserRoles.Checker.GetDisplayName() : "Customer", entity.CreatedOn);
            return result;
        }

        internal static IEnumerable<CommentModel> AsModel(this IEnumerable<OrderDesignComment> entities)
        {
            var result = entities.Select(AsModel);
            return result;
        }
    }
}
