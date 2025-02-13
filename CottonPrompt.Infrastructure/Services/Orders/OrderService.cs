using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CottonPrompt.Infrastructure.Constants;
using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Extensions;
using CottonPrompt.Infrastructure.Models.Designs;
using CottonPrompt.Infrastructure.Models.Orders;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using CottonPrompt.Infrastructure.Models;
using CottonPrompt.Infrastructure.Helpers;

namespace CottonPrompt.Infrastructure.Services.Orders
{
    public class OrderService(CottonPromptContext dbContext, BlobServiceClient blobServiceClient, IConfiguration config) : IOrderService
    {
        public async Task ApproveAsync(int id)
        {
            try
            {
                var order = await dbContext.Orders
                    .Include(o => o.OrderDesigns)
                    .Include(o => o.DesignBracket)
                    .SingleOrDefaultAsync(o => o.Id == id);

                if (order is null || order.CheckerId is null || order.ArtistId is null) return;

                await UpdateCheckerStatusAsync(id, OrderStatuses.Approved, order.CheckerId.Value);

                if (order.OriginalOrderId is null)
                {
                    await UpdateArtistStatusAsync(id, OrderStatuses.Completed, order.ArtistId.Value);
                }

                var isRecordedAlready = await dbContext.InvoiceSectionOrders.AnyAsync(iso => iso.OrderId == id);
                if (!isRecordedAlready)
                {
                    order.CompletedOn = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                    await RecordInvoice(order);
                }

                await UpdateCustomerStatusAsync(id, OrderStatuses.ForReview);
                await SendOrderProof(id, order.CustomerEmail);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<CanDoModel> AssignArtistAsync(int id, Guid artistId)
        {
            try
            {
                var order = await dbContext.Orders.FindAsync(id); 
                
                if (order is null)
                {
                    return new CanDoModel(false, "The order is not found");
                }

                if (order.ArtistId.HasValue)
                {
                    return new CanDoModel(false, "The order is already claimed by another Artist");
                }
                    
                await dbContext.Orders
                    .Where(o => o.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.ArtistId, artistId)
                        .SetProperty(o => o.ArtistStatus, OrderStatuses.Claimed)
                        .SetProperty(o => o.UpdatedBy, artistId)
                        .SetProperty(o => o.UpdatedOn, DateTime.UtcNow));

                await CreateOrderHistory(id, OrderStatuses.Claimed, artistId);

                var result = new CanDoModel(true, string.Empty);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<CanDoModel> AssignCheckerAsync(int id, Guid checkerId)
        {
            try
            {
                var order = await dbContext.Orders
                    .Include(o => o.OrderDesigns)
                    .SingleOrDefaultAsync(o => o.Id == id);

                if (order is null)
                {
                    return new CanDoModel(false, "The order is not found");
                }

                if (order.CheckerId.HasValue)
                {
                    return new CanDoModel(false, "The order is already claimed by another Checker");
                }

                var status = order.OrderDesigns.Count > 0 ? OrderStatuses.ForReview : OrderStatuses.Claimed;

                order.CheckerId = checkerId;
                order.CheckerStatus = status;
                order.UpdatedBy = checkerId;
                order.UpdatedOn = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                await CreateOrderHistory(id, status, checkerId);

                var result = new CanDoModel(true, string.Empty);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task CreateAsync(Order order)
        {
            try
            {
                foreach (var imageRef in order.OrderImageReferences)
                {
                    if (imageRef.Type == "File" && imageRef.Url.StartsWith("data"))
                    {
                        var containerName = "order-references";
                        var name = SaltDesignName(imageRef.Name);
                        await UploadFile(name, imageRef.Url, containerName);
                        imageRef.Name = name;
                        imageRef.Url = GetFileUrl(name, containerName, DateTimeOffset.UtcNow.AddYears(100));
                    }
                }

                await dbContext.Orders.AddAsync(order);
                await dbContext.SaveChangesAsync();
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
                var order = await dbContext.Orders.FindAsync(id);

                if (order is null || order.ArtistStatus == OrderStatuses.Completed) return;

                if (order.OriginalOrderId != null)
                {
                    await dbContext.Orders.Where(o => o.Id == order.OriginalOrderId)
                        .ExecuteUpdateAsync(setter => setter
                            .SetProperty(o => o.ChangeRequestOrderId, (int?)null));
                }

                await dbContext.Orders.Where(o => o.Id == id).ExecuteDeleteAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetAsync(bool? priority, string? artistStatus, string? checkerStatus, string? customerStatus, Guid? artistId, Guid? checkerId, bool noArtist = false, bool noChecker = false)
        {
            try
            {
                IQueryable<Order> queryableOrders = dbContext.Orders
                    .Include(o => o.UserGroup)
                    .Where(o => true);

                if (priority != null)
                {
                    queryableOrders = queryableOrders.Where(o => o.Priority == priority);
                }

                // for artists
                if (artistId != null)
                {
                    queryableOrders = queryableOrders.Where(o => o.ArtistId == artistId);
                } 
                else if (noArtist)
                {
                    queryableOrders = queryableOrders.Where(o => o.ArtistId == null);
                }

                // for checkers
                if (checkerId != null)
                {
                    queryableOrders = queryableOrders.Where(o => o.CheckerId == checkerId);
                }
                else if (noChecker)
                {
                    queryableOrders = queryableOrders.Where(o => o.CheckerId == null);
                }

                // order status
                if (!string.IsNullOrEmpty(artistStatus))
                {
                    var statuses = artistStatus.Split(',');
                    queryableOrders = queryableOrders.Where(o => statuses.Contains(o.ArtistStatus));
                }
                if (!string.IsNullOrEmpty(checkerStatus))
                {
                    var statuses = checkerStatus.Split(',');
                    queryableOrders = queryableOrders.Where(o => statuses.Contains(o.CheckerStatus));
                }
                if (!string.IsNullOrEmpty(customerStatus))
                {
                    var statuses = customerStatus.Split(',');
                    queryableOrders = queryableOrders.Where(o => statuses.Contains(o.CustomerStatus));
                }

                var orders = await queryableOrders.OrderByDescending(o => o.Priority).ThenBy(o => o.CreatedOn).ToListAsync();
                var result = orders.AsGetOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetAvailableAsArtistAsync(Guid artistId, bool? priority, bool changeRequest = false)
        {
            try
            {
                var userGroupIds = await dbContext.UserGroupUsers
                    .Where(ugu => ugu.UserId == artistId)
                    .Select(ugu => ugu.UserGroupId)
                    .ToListAsync();

                var queryableOrders = dbContext.Orders
                    .Include(ugu => ugu.UserGroup)
                    .Where(o => o.ArtistId == null
                        && userGroupIds.Contains(o.UserGroupId)
                        && (!o.OrderReports.Any(r => r.ResolvedBy == null)));

                if (changeRequest)
                {
                    queryableOrders = queryableOrders.Where(o => o.OriginalOrderId != null && o.CheckerId != artistId);
                }
                else
                {
                    queryableOrders = queryableOrders.Where(o => o.OriginalOrderId == null);
                }

                if (priority != null)
                {
                    queryableOrders = queryableOrders.Where (o => o.Priority == priority);
                }

                var orders = await queryableOrders.OrderByDescending(o => o.Priority).ThenBy(o => o.CreatedOn).ToListAsync();
                var result = orders.AsGetOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetAvailableAsCheckerAsync(bool? priority, bool trainingGroup = false)
        {
            try
            {
                var trainingGroupArtistsId = await dbContext.Settings.Select(s => s.TrainingGroupArtistsGroupId).FirstOrDefaultAsync();

                var queryableOrders = dbContext.Orders
                    .Include(o => o.UserGroup)
                    .Where(o => o.ArtistStatus == OrderStatuses.DesignSubmitted && o.CheckerId == null);

                if (priority != null)
                {
                    queryableOrders = queryableOrders.Where(o => o.Priority == priority);
                }

                if (trainingGroup)
                {
                    queryableOrders = queryableOrders.Where(o => o.UserGroupId == trainingGroupArtistsId);
                }
                else
                {
                    queryableOrders = queryableOrders.Where(o => o.UserGroupId != trainingGroupArtistsId);
                }

                var orders = await queryableOrders.OrderByDescending(o => o.Priority).ThenBy(o => o.CreatedOn).ToListAsync();
                var result = orders.AsGetOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<GetOrderModel> GetByIdAsync(int id)
        {
            try
            {
                var order = await dbContext.Orders
                    .Include(o => o.DesignBracket)
                    .Include(o => o.PrintColor)
                    .Include(o => o.OutputSize)
                    .Include(o => o.OrderImageReferences)
                    .Include(o => o.OrderDesigns).ThenInclude(od => od.OrderDesignComments)
                    .Include(o => o.UserGroup)
                    .SingleAsync(o => o.Id == id);

                var designs = new List<DesignModel>();

                foreach (var orderDesign in order.OrderDesigns)
                {
                    var url = GetFileUrl(orderDesign.Name, "order-designs", DateTimeOffset.UtcNow.AddHours(1));
                    var design = orderDesign.AsModel(url);
                    designs.Add(design);
                }

                var result = order.AsGetOrderModel(designs);

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SubmitDesignAsync(int id, string designName, string designContent)
        {
            try
            {
                var order = await dbContext.Orders
                    .Include(o => o.OrderDesigns)
                    .SingleAsync(o => o.Id == id);

                if (order is null || order.ArtistId is null) return;

                var saltedDesignName = SaltDesignName(order.OrderNumber, designName);
                await UploadFile(saltedDesignName, designContent, "order-designs");

                order.OrderDesigns.Add(new OrderDesign
                {
                    OrderId = order.Id,
                    Name = saltedDesignName,
                    CreatedBy = order.ArtistId.Value,
                });

                await dbContext.SaveChangesAsync();

                await UpdateArtistStatusAsync(id, OrderStatuses.DesignSubmitted, order.ArtistId.Value);

                if (order.CheckerId is null) return;

                await UpdateCheckerStatusAsync(id, OrderStatuses.ForReview, order.CheckerId.Value);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task UpdateAsync(Order order)
        {
            try
            {
                var currentOrder = await dbContext.Orders.Include(o => o.OrderImageReferences).SingleOrDefaultAsync(o => o.Id == order.Id);

                if (currentOrder is null) return;

                currentOrder.OrderNumber = order.OrderNumber;
                currentOrder.Priority = order.Priority;
                currentOrder.Concept = order.Concept;
                currentOrder.PrintColorId = order.PrintColorId;
                currentOrder.DesignBracketId = order.DesignBracketId;
                currentOrder.OutputSizeId = order.OutputSizeId;
                currentOrder.CustomerEmail = order.CustomerEmail;
                currentOrder.UpdatedBy = order.UpdatedBy;
                currentOrder.UpdatedOn = order.UpdatedOn;
                currentOrder.UserGroupId = order.UserGroupId;

                currentOrder.OrderImageReferences.Clear();
                foreach (var imageRef in order.OrderImageReferences)
                {
                    if (imageRef.Type == "File" && imageRef.Url.StartsWith("data"))
                    {
                        var containerName = "order-references";
                        var name = SaltDesignName(imageRef.Name);
                        await UploadFile(name, imageRef.Url, containerName);
                        imageRef.Name = name;
                        imageRef.Url = GetFileUrl(name, containerName, DateTimeOffset.UtcNow.AddYears(100));
                    }

                    currentOrder.OrderImageReferences.Add(imageRef);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task AcceptAsync(int id, Guid? userId)
        {
            try
            {
                var order = await dbContext.Orders
                    .Include(o => o.DesignBracket)
                    .SingleOrDefaultAsync(o => o.Id == id);

                await UpdateCustomerStatusAsync(id, OrderStatuses.Accepted, userId);

                if (order != null && order.OriginalOrderId != null && order.ArtistId != null)
                {
                    await UpdateArtistStatusAsync(id, OrderStatuses.Completed, order.ArtistId.Value);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ChangeRequestAsync(int id, int designId, string comment, IEnumerable<OrderImageReference> imageReferences)
        {
            try
            {
                await UpdateCustomerStatusAsync(id, OrderStatuses.ChangeRequested);

                var order = await dbContext.Orders
                    .Include(o => o.OrderDesigns)
                    .Include(o => o.OrderImageReferences)
                    .SingleOrDefaultAsync(o => o.Id == id);

                var changeRequestArtistsGroupId = await dbContext.Settings.Select(s => s.ChangeRequestArtistsGroupId).FirstOrDefaultAsync();

                if (order is null) return;

                var customerId = Guid.Empty;
                var currentDesign = order.OrderDesigns.Last();
                var customerComment = new OrderDesignComment
                {
                    UserId = customerId,
                    Comment = comment,
                    CreatedBy = customerId,
                };

                if (order.OriginalOrderId is null)
                {
                    var newOrder = new Order
                    {
                        OrderNumber = $"{order.OrderNumber} CR",
                        Priority = order.Priority,
                        Concept = order.Concept,
                        PrintColorId = order.PrintColorId,
                        DesignBracketId = order.DesignBracketId,
                        OutputSizeId = order.OutputSizeId,
                        UserGroupId = changeRequestArtistsGroupId,
                        CustomerEmail = order.CustomerEmail,
                        CheckerId = order.CheckerId,
                        CheckerStatus = OrderStatuses.Claimed,
                        CreatedBy = customerId,
                        OriginalOrderId = order.Id,
                        OrderDesigns =
                        [
                            new()
                            {
                                Name = currentDesign.Name,
                                OrderDesignComments = new List<OrderDesignComment>
                                {
                                    customerComment,
                                },
                                CreatedBy = customerId,
                            }
                        ],
                        OrderImageReferences = order.OrderImageReferences.Select(oir => new OrderImageReference
                        {
                            LineId = oir.LineId,
                            Type = oir.Type,
                            Url = oir.Url,
                            Name = oir.Name,
                            CreatedBy = customerId,
                        }).ToList(),
                    };

                    foreach (var imageRef in imageReferences)
                    {
                        imageRef.LineId = newOrder.OrderImageReferences.Count() + 1;

                        if (imageRef.Type == "File" && imageRef.Url.StartsWith("data"))
                        {
                            var containerName = "order-references";
                            var name = SaltDesignName(imageRef.Name);
                            await UploadFile(name, imageRef.Url, containerName);
                            imageRef.Name = name;
                            imageRef.Url = GetFileUrl(name, containerName, DateTimeOffset.UtcNow.AddYears(100));
                        }

                        newOrder.OrderImageReferences.Add(imageRef);
                    }

                    await dbContext.Orders.AddAsync(newOrder);
                    await dbContext.SaveChangesAsync();

                    order.ChangeRequestOrderId = newOrder.Id;
                }
                else
                {
                    await UpdateArtistStatusAsync(id, OrderStatuses.ForReupload, order.ArtistId!.Value);
                    await UpdateCheckerStatusAsync(id, OrderStatuses.Claimed, order.CheckerId!.Value);

                    currentDesign.OrderDesignComments.Add(customerComment);

                    foreach (var imageRef in imageReferences)
                    {
                        imageRef.LineId = order.OrderImageReferences.Count() + 1;

                        if (imageRef.Type == "File" && imageRef.Url.StartsWith("data"))
                        {
                            var containerName = "order-references";
                            var name = SaltDesignName(imageRef.Name);
                            await UploadFile(name, imageRef.Url, containerName);
                            imageRef.Name = name;
                            imageRef.Url = GetFileUrl(name, containerName, DateTimeOffset.UtcNow.AddYears(100));
                        }

                        order.OrderImageReferences.Add(imageRef);
                    }
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetOngoingAsync(OrderFiltersModel? filters = null)
        {
            try
            {
                var queryableOrders = dbContext.Orders
                    .Include(o => o.Artist)
                    .Include(o => o.Checker)
                    .Include(o => o.UserGroup)
                    .Where(o => (o.CustomerStatus == null 
                    || o.CustomerStatus == OrderStatuses.ForReview
                    || (o.OriginalOrderId != null && o.CustomerStatus == OrderStatuses.ChangeRequested)) // multi-CR'ed orders
                    && !o.OrderReports.Any(r => r.ResolvedBy == null));  // exclude reported orders 

                queryableOrders = OrderHelper.FilterOrders(queryableOrders, filters);

                var orders = await queryableOrders.OrderByDescending(o => o.Priority).ThenBy(o => o.CreatedOn).ToListAsync();
                var result = orders.AsGetOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetCompletedAsync(OrderFiltersModel? filters = null)
        {
            try
            {
                var queryableOrders = dbContext.Orders
                    .Include(o => o.Artist)
                    .Include(o => o.Checker)
                    .Include(o => o.UserGroup)
                    .Where(o => o.CustomerStatus == OrderStatuses.Accepted
                        && o.SentForPrintingOn == null);

                queryableOrders = OrderHelper.FilterOrders(queryableOrders, filters);

                var orders = await queryableOrders.OrderByDescending(o => o.AcceptedOn).ToListAsync();
                var result = orders.AsGetCompletedOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetRejectedAsync(OrderFiltersModel? filters = null)
        {
            try
            {
                var queryableOrders = dbContext.Orders
                    .Include(o => o.Artist)
                    .Include(o => o.Checker)
                    .Include(o => o.ChangeRequestOrder)
                    .Include(o => o.UserGroup)
                    .Where(o => o.ArtistStatus == OrderStatuses.Completed
                    && o.CustomerStatus == OrderStatuses.ChangeRequested
                    && o.OriginalOrderId == null
                    && o.ChangeRequestOrder.CustomerStatus != OrderStatuses.Accepted);

                queryableOrders = OrderHelper.FilterOrders(queryableOrders, filters);

                var orders = await queryableOrders.OrderByDescending(o => o.ChangeRequestedOn).ToListAsync();
                var result = orders.AsGetRejectedOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetReportedAsync(OrderFiltersModel? filters = null)
        {
            try
            {
                var queryableOrders = dbContext.Orders
                    .Include(o => o.OrderReports.Where(r => r.ResolvedBy == null))
                    .ThenInclude(or => or.ReportedByNavigation)
                    .Include(o => o.UserGroup)
                    .Where(o => o.OrderReports.Any(r => r.ResolvedBy == null));

                queryableOrders = OrderHelper.FilterOrders(queryableOrders, filters);

                var orders = await queryableOrders.OrderByDescending(o => o.ReportedOn).ToListAsync();
                var result = orders.AsGetReportedOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<GetOrdersModel>> GetSentForPrintingAsync(OrderFiltersModel? filters = null)
        {
            try
            {
                var queryableOrders = dbContext.Orders
                    .Include(o => o.Artist)
                    .Include(o => o.Checker)
                    .Include(o => o.UserGroup)
                    .Where(o => o.CustomerStatus == OrderStatuses.Accepted
                        && o.SentForPrintingOn != null);

                queryableOrders = OrderHelper.FilterOrders(queryableOrders, filters);

                var orders = await queryableOrders.OrderByDescending(o => o.SentForPrintingOn).ToListAsync();
                var result = orders.AsGetCompletedOrdersModel();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<DownloadModel> DownloadAsync(int id)
        {
            try
            {
                var order = await dbContext.Orders
                    .Include(o => o.OrderDesigns)
                    .SingleAsync(o => o.Id == id);

                var currentDesign = order.OrderDesigns.Last();
                var container = blobServiceClient.GetBlobContainerClient("order-designs");
                var blob = container.GetBlobClient(currentDesign.Name);
                var response = await blob.DownloadContentAsync();
                var responseValue = response.Value;
                var result = new DownloadModel(responseValue.Content.ToStream(), responseValue.Details.ContentType, currentDesign.Name);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ResendForCustomerReviewAsync(int id)
        {
            try
            {
                var order = await dbContext.Orders.FindAsync(id);

                if (order is null) return;

                if (order.CustomerStatus == OrderStatuses.Accepted
                    || (order.CustomerStatus == OrderStatuses.ChangeRequested && order.ChangeRequestOrderId is null))
                {
                    await UpdateCustomerStatusAsync(id, OrderStatuses.ForReview);
                }

                if (order.SentForPrintingOn != null)
                {
                    order.SentForPrintingOn = null;
                    await dbContext.SaveChangesAsync();
                }

                await SendOrderProof(id, order.CustomerEmail);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ReportAsync(int id, string reason, bool isRedraw)
        {
            try
            {
                var order = await dbContext.Orders
                    .Include(o => o.OrderDesigns)
                    .SingleOrDefaultAsync(o => o.Id == id);

                if (order is null || order.ArtistId is null) return;

                var isDesignSubmitted = (order.OriginalOrderId == null && order.OrderDesigns.Count > 0) || (order.OriginalOrderId != null && order.OrderDesigns.Count > 1);

                var orderReport = new OrderReport
                {
                    OrderId = id,
                    Reason = reason,
                    ReportedBy = order.ArtistId.Value,
                    CheckerId = order.CheckerId,
                    IsRedraw = isRedraw,
                    IsDesignSubmitted = isDesignSubmitted,
                    ArtistStatus = order.ArtistStatus,
                    CheckerStatus = order.CheckerStatus,
                    CustomerStatus = order.CustomerStatus,
                };

                await dbContext.OrderReports.AddAsync(orderReport);

                order.ArtistId = null;
                order.ArtistStatus = null;
                order.CheckerId = null;
                order.CheckerStatus = null;
                order.CustomerStatus = null;
                order.ReportedOn = DateTime.UtcNow;
                
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ResolveAsync(int id, Guid resolvedBy)
        {
            try
            {
                var report = await dbContext.OrderReports.SingleOrDefaultAsync(r => r.OrderId == id && r.ResolvedOn == null);
                var order = await dbContext.Orders.FindAsync(id);

                if (report is null || order is null) return;

                if (report.IsDesignSubmitted || report.IsRedraw)
                {
                    order.ArtistId = report.ReportedBy;
                    order.ArtistStatus = report.ArtistStatus;
                    order.CheckerId = report.CheckerId;
                    order.CheckerStatus = report.CheckerStatus;
                    order.CustomerStatus = report.CustomerStatus;
                }
                else
                {
                    if (order.OriginalOrderId != null)
                    {
                        order.CheckerId = report.CheckerId;
                        order.CheckerStatus = OrderStatuses.Claimed;
                    }

                    order.CreatedOn = DateTime.UtcNow;
                }

                order.UpdatedBy = resolvedBy;
                order.UpdatedOn = DateTime.UtcNow;

                report.ResolvedBy = resolvedBy;
                report.ResolvedOn = DateTime.UtcNow;
                
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SendForPrintingAsync(int id, Guid userId)
        {
            try
            {
                await dbContext.Orders.Where(o => o.Id == id)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(o => o.SentForPrintingOn, DateTime.UtcNow)
                        .SetProperty(o => o.UpdatedBy, userId)
                        .SetProperty(o => o.UpdatedOn, DateTime.UtcNow));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task RedrawAsync(Order order, int changeRequestOrderId)
        {
            try
            {
                await CreateAsync(order);

                var invoiceOrders = await dbContext.InvoiceSectionOrders
                    .Include(o => o.InvoiceSection)
                    .ThenInclude(o => o.Invoice)
                    .Where(o => o.OrderId == changeRequestOrderId).ToListAsync();

                foreach (var invoiceOrder in invoiceOrders)
                {
                    var phTimeOffset = 8;
                    var invoiceSection = invoiceOrder.InvoiceSection;
                    var invoice = invoiceSection.Invoice;

                    if (DateTime.UtcNow.AddHours(phTimeOffset) < invoice.EndDate)
                    {
                        invoice.Amount -= invoiceSection.Rate;
                        invoiceSection.Amount -= invoiceSection.Rate;
                        invoiceSection.Quantity--;

                        dbContext.InvoiceSectionOrders.Remove(invoiceOrder);
                    }
                }

                await dbContext.SaveChangesAsync();

                await DeleteAsync(changeRequestOrderId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task ToggleRedrawMarkAsync(int id)
        {
            try
            {
                await dbContext.OrderReports
                    .Where(or => or.OrderId == id && or.ResolvedBy == null)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(or => or.IsRedraw, or => !or.IsRedraw));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task UpdateArtistStatusAsync(int id, string status, Guid artistId)
        {
            try
            {
                await dbContext.Orders
                    .Where(o => o.Id == id)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(o => o.ArtistStatus, status)
                        .SetProperty(o => o.UpdatedBy, artistId)
                        .SetProperty(o => o.UpdatedOn, DateTime.UtcNow));

                await CreateOrderHistory(id, status, artistId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task UpdateCheckerStatusAsync(int id, string status, Guid checkerId)
        {
            try
            {
                await dbContext.Orders
                    .Where(o => o.Id == id)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(o => o.CheckerStatus, status)
                        .SetProperty(o => o.UpdatedBy, checkerId)
                        .SetProperty(o => o.UpdatedOn, DateTime.UtcNow));

                await CreateOrderHistory(id, status, checkerId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task UpdateCustomerStatusAsync(int id, string status, Guid? userId = null)
        {
            try
            {
                var updateUserId = userId ?? Guid.Empty;

                await dbContext.Orders
                    .Where(o => o.Id == id)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(o => o.CustomerStatus, status)
                        .SetProperty(o => o.UpdatedOn, DateTime.UtcNow)
                        .SetProperty(o => o.UpdatedBy, updateUserId)
                        .SetProperty(o => status == OrderStatuses.Accepted ? o.AcceptedOn : o.ChangeRequestedOn, DateTime.UtcNow));

                await CreateOrderHistory(id, status, updateUserId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task CreateOrderHistory(int orderId, string status, Guid userId)
        {
            try
            {
                await dbContext.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = status,
                    CreatedBy = userId,
                    CreatedOn = DateTime.UtcNow,
                });

                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string SaltDesignName(string orderNumber, string designName)
        {
            var result = SaltDesignName($"{orderNumber}_{designName}");
            return result;
        }

        private string SaltDesignName(string designName)
        {
            var fileName = Path.GetFileNameWithoutExtension(designName);
            var fileExtension = Path.GetExtension(designName);
            var result = $"{fileName}_{Guid.NewGuid().ToString()[..5]}{fileExtension}";

            if (result.Length > 100) result = result.Substring(result.Length - 100); // max length is 100

            return result;
        }

        private async Task RecordInvoice(Order order)
        {
            if (order.CompletedOn is null || order.ArtistId is null || order.CheckerId is null) return;

            var phTimeOffset = 8;
            var completedOn = order.CompletedOn.Value.AddHours(phTimeOffset);
            var daysOffset = completedOn.DayOfWeek != DayOfWeek.Sunday ? (int)completedOn.DayOfWeek : 7;
            var startDate = completedOn.AddDays((daysOffset - (int)DayOfWeek.Monday) * -1).Date + new TimeSpan(0, 0, 0);
            var endDate = completedOn.AddDays(7 - daysOffset).Date + new TimeSpan(23, 59, 59);
            var rates = await dbContext.Settings.FirstAsync();

            var artistInvoice = await dbContext.Invoices
                .Include(i => i.InvoiceSections)
                .SingleOrDefaultAsync(i => i.StartDate == startDate && i.UserId == order.ArtistId);

            if (order.OriginalOrderId is null)
            {
                // record artist invoice
                await RecordInvoice(artistInvoice, order.ArtistId.Value, order.DesignBracket.Name, order.DesignBracket.Value, startDate, endDate, order.Id, order.OrderNumber);
            }
            else
            {
                // record change request artist invoice
                var changeRequestSectionName = "Change Request";
                await RecordInvoice(artistInvoice, order.ArtistId.Value, changeRequestSectionName, rates.ChangeRequestRate, startDate, endDate, order.Id, order.OrderNumber);
            }

            // record checker invoice
            var checkerInvoice = await dbContext.Invoices
                .Include(i => i.InvoiceSections)
                .SingleOrDefaultAsync(i => i.StartDate == startDate && i.UserId == order.CheckerId);
            var checkerSectionName = "Quality Control";
            await RecordInvoice(checkerInvoice, order.CheckerId.Value, checkerSectionName, rates.QualityControlRate, startDate, endDate, order.Id, order.OrderNumber);

            await dbContext.SaveChangesAsync();
        }

        private async Task RecordInvoice(Invoice? invoice, Guid userId, string sectionName, decimal sectionRate, DateTime startDate, DateTime endDate, int orderId, string orderNumber)
        {
            if (invoice is null)
            {
                invoice = new()
                {
                    UserId = userId,
                    Amount = sectionRate,
                    StartDate = startDate,
                    EndDate = endDate,
                    InvoiceSections =
                    [
                        new()
                        {
                            Name = sectionName,
                            Rate = sectionRate,
                            Amount = sectionRate,
                            Quantity = 1,
                            InvoiceSectionOrders =
                            [
                                new()
                                {
                                    OrderId = orderId,
                                    OrderNumber = orderNumber,
                                },
                            ],
                        },
                    ],
                };

                await dbContext.Invoices.AddAsync(invoice);
            }
            else
            {
                var invoiceSection = invoice.InvoiceSections.SingleOrDefault(s => s.Name == sectionName && s.Rate == sectionRate);

                if (invoiceSection is null)
                {
                    invoiceSection = new()
                    {
                        Name = sectionName,
                        Rate = sectionRate,
                        Amount = sectionRate,
                        Quantity = 1,
                        InvoiceSectionOrders =
                        [
                            new()
                            {
                                OrderId = orderId,
                                OrderNumber = orderNumber,
                            },
                        ],
                    };

                    invoice.InvoiceSections.Add(invoiceSection);
                    invoice.Amount += invoiceSection.Amount;
                }
                else
                {
                    invoiceSection.InvoiceSectionOrders.Add(new()
                    {
                        OrderId = orderId,
                        OrderNumber = orderNumber,
                    });

                    invoiceSection.Quantity += 1;
                    invoiceSection.Amount += sectionRate;
                    invoice.Amount += sectionRate;
                }
            }
        }

        private async Task SendOrderProof(int id, string customerEmail)
        {
            var emailTemplates = await dbContext.EmailTemplates.FirstAsync();
            var smtpConfig = config.GetSection("Smtp");
            var client = new SmtpClient(smtpConfig["Host"], Convert.ToInt32(smtpConfig["Port"]))
            {
                Credentials = new NetworkCredential(smtpConfig["Username"], smtpConfig["Password"]),
                EnableSsl = true
            };
            var from = new MailAddress(smtpConfig["SenderEmail"]!, smtpConfig["SenderName"]);
            var to = new MailAddress(customerEmail);
            var message = new MailMessage(from, to)
            {
                Body = emailTemplates.OrderProofReadyEmail.Replace("{link}", $"{config["FrontendUrl"]}/order-proof/{id}").Replace("<p style=\"margin: 0\"></p>", "<br/>"),
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
                Subject = "Order Proof Ready"
            };
            await client.SendMailAsync(message);
        }

        private async Task UploadFile(string name, string content, string containerName)
        {
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();

            var base64 = content.Substring(content.IndexOf("base64,") + 7);
            var bytes = Convert.FromBase64String(base64);
            var contentStream = new MemoryStream(bytes);
            
            var blob = container.GetBlobClient(name);
            await blob.UploadAsync(contentStream);
        }

        private string GetFileUrl(string name, string containerName, DateTimeOffset expiresOn)
        {
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(name);
            var result = blob.GenerateSasUri(BlobSasPermissions.Read, expiresOn).ToString();
            return result;
        }
    }
}
