using CottonPrompt.Api.Messages.Orders;
using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models.Orders;

namespace CottonPrompt.Api.Extensions
{
    public static class OrderExtensions
    {
        public static Order AsEntity(this CreateOrderRequest request)
        {
            var result = new Order
            {
                OrderNumber = request.OrderNumber,
                Priority = request.Priority,
                Concept = request.Concept,
                PrintColorId = request.PrintColorId,
                DesignBracketId = request.DesignBracketId,
                OutputSizeId = request.OutputSizeId,
                CustomerEmail = request.CustomerEmail,
                OrderImageReferences = request.ImageReferences?.AsEntity(request.CreatedBy).ToList(),
                AuthorId = request.AuthorId,
                CreatedBy = request.CreatedBy,
                UserGroupId = request.UserGroupId,
            };
            return result;
        }

        public static Order AsEntity(this UpdateOrderRequest request)
        {
            var result = new Order
            {
                Id = request.Id,
                OrderNumber = request.OrderNumber,
                Priority = request.Priority,
                Concept = request.Concept,
                PrintColorId = request.PrintColorId,
                DesignBracketId = request.DesignBracketId,
                OutputSizeId = request.OutputSizeId,
                CustomerEmail = request.CustomerEmail,
                OrderImageReferences = request.ImageReferences?.AsEntity(request.UpdatedBy, request.Id).ToList(),
                AuthorId = request.AuthorId,
                UpdatedBy = request.UpdatedBy,
                UpdatedOn = DateTime.UtcNow,
                UserGroupId = request.UserGroupId,
            };
            return result;
        }

        public static OrderImageReference AsEntity(this ImageReferenceRequest request, Guid createdBy, int index, int orderId = 0)
        {
            var result = new OrderImageReference
            {
                OrderId = orderId,
                LineId = index + 1,
                Type = request.Type,
                Url = request.Value,
                Name = request.Name,
                CreatedBy = createdBy,
            };
            return result;
        }

        public static IEnumerable<OrderImageReference> AsEntity(this IEnumerable<ImageReferenceRequest> request, Guid createdBy, int orderId = 0)
        {
            var result = request.Select((r, i) => r.AsEntity(createdBy, i, orderId));
            return result;
        }

        public static OrderFiltersModel AsModel(this GetAdminOrdersRequest request)
        {
            var orderNumbers = request.OrderNumbers?.Split(',') ?? [];
            var priorities = request.Priorities?.Split(',') ?? [];
            var artists = request.Artists?.Split(',').Select(Guid.Parse) ?? [];
            var checkers = request.Checkers?.Split(',').Select(Guid.Parse) ?? [];
            var customers = request.Customers?.Split(',') ?? [];
            var status = request.Status?.Split(',') ?? [];
            var userGroups = request.UserGroups?.Split(',').Select(s => Convert.ToInt32(s)) ?? [];
            var result = new OrderFiltersModel(orderNumbers, priorities, artists, checkers, customers, status, userGroups, request.Page, request.PageSize);
            return result;
        }
    }
}
