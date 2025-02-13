using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models.Designs;
using CottonPrompt.Infrastructure.Models.Orders;

namespace CottonPrompt.Infrastructure.Extensions
{
    internal static class OrderExtensions
    {
        internal static GetOrdersModel AsGetOrdersModel(this Order entity, DateTime? date = null)
        {
            var result = new GetOrdersModel(entity.Id, entity.OrderNumber, entity.Priority, date ?? entity.CreatedOn, entity.ArtistStatus, entity.CheckerStatus, entity.ArtistId, entity.Artist?.Name, entity.CheckerId, entity.Checker?.Name, entity.CustomerStatus, entity.CustomerEmail, entity.OriginalOrderId, entity.ChangeRequestOrderId, entity.OrderReports.FirstOrDefault()?.Reason, entity.AcceptedOn, entity.ChangeRequestedOn, entity.ReportedOn, entity.OrderReports.FirstOrDefault()?.ReportedByNavigation.Name, entity.UserGroupId, entity.UserGroup.Name, entity.OrderReports.FirstOrDefault()?.IsDesignSubmitted, entity.OrderReports.FirstOrDefault()?.IsRedraw, entity.IsCoolDown, entity.UpdatedOn);
            return result;
        }

        internal static IEnumerable<GetOrdersModel> AsGetOrdersModel(this IEnumerable<Order> entities)
        {
            var result = entities.Select(e => e.AsGetOrdersModel(e.CreatedOn));
            return result;
        }

        internal static IEnumerable<GetOrdersModel> AsGetCompletedOrdersModel(this IEnumerable<Order> entities)
        {
            var result = entities.Select(e => e.AsGetOrdersModel(e.AcceptedOn));
            return result;
        }

        internal static IEnumerable<GetOrdersModel> AsGetRejectedOrdersModel(this IEnumerable<Order> entities)
        {
            var result = entities.Select(e => e.AsGetOrdersModel(e.ChangeRequestedOn));
            return result;
        }

        internal static IEnumerable<GetOrdersModel> AsGetReportedOrdersModel(this IEnumerable<Order> entities)
        {
            var result = entities.Select(e => e.AsGetOrdersModel(e.ReportedOn));
            return result;
        }

        internal static IEnumerable<GetOrdersModel> AsGetSentForPrintingOrdersModel(this IEnumerable<Order> entities)
        {
            var result = entities.Select(e => e.AsGetOrdersModel(e.SentForPrintingOn));
            return result;
        }

        internal static GetOrderModel AsGetOrderModel(this Order entity, IEnumerable<DesignModel> designs)
        {
            var currentDesign = ((entity.OriginalOrderId == null && designs.Any()) || (entity.OriginalOrderId != null && designs.Count() > 1)) ? designs.Last() : null;
            var previousDesigns = designs.Where(d => currentDesign == null || d.Id != currentDesign.Id);
            var result = new GetOrderModel(entity.Id, entity.OrderNumber, entity.Priority, entity.Concept, entity.PrintColor.AsModel(), entity.DesignBracket.AsModel(), entity.OutputSize.AsModel(), entity.UserGroupId, entity.CustomerEmail, entity.OrderImageReferences.AsModel(), currentDesign, previousDesigns, entity.ArtistStatus, entity.CheckerStatus, entity.CustomerStatus, entity.ArtistId, entity.CheckerId, entity.UserGroup.Name, entity.OriginalOrderId.HasValue);
            return result;
        }

        internal static ImageReferenceModel AsModel(this OrderImageReference entity)
        {
            var result = new ImageReferenceModel(entity.Type, entity.Url, entity.Name);
            return result;
        }

        internal static IEnumerable<ImageReferenceModel> AsModel(this IEnumerable<OrderImageReference> entities)
        {
            var result = entities.Select(AsModel);
            return result;
        }
    }
}
