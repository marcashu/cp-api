using CottonPrompt.Infrastructure.Constants;
using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models.Invoices;

namespace CottonPrompt.Infrastructure.Extensions
{
    internal static class InvoiceExtensions
    {
        internal static GetInvoicesModel AsGetInvoicesModel(this Invoice entity)
        {
            var phTimeOffset = 8;
            var status = DateTime.UtcNow.AddHours(phTimeOffset) > entity.EndDate ? InvoiceStatuses.Completed : InvoiceStatuses.Ongoing;
            var result = new GetInvoicesModel(entity.Id, entity.StartDate, entity.EndDate, entity.User.Name, entity.Amount, status);
            return result;
        }

        internal static IEnumerable<GetInvoicesModel> AsGetInvoicesModel(this IEnumerable<Invoice> entities) 
        {
            var result = entities.Select(AsGetInvoicesModel);
            return result;
        }

        internal static GetInvoiceModel AsGetInvoiceModel(this Invoice entity)
        {
            var result = new GetInvoiceModel(entity.Id, entity.EndDate, entity.Amount, entity.InvoiceSections.AsModel());
            return result;
        }

        private static GetInvoiceSectionModel AsModel(this InvoiceSection entity)
        {
            var result = new GetInvoiceSectionModel(entity.Name, entity.Rate, entity.Amount, entity.Quantity, entity.InvoiceSectionOrders.Select(iso => iso.OrderNumber));
            return result;
        }

        private static IEnumerable<GetInvoiceSectionModel> AsModel(this IEnumerable<InvoiceSection> entities)
        {
            var result = entities.Select(AsModel);
            return result;
        }
    }
}
