using Azure.Storage.Blobs;
using CottonPrompt.Infrastructure.Constants;
using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace CottonPrompt.Infrastructure.Services.Designs
{
    public class DesignService(CottonPromptContext dbContext, BlobServiceClient blobServiceClient) : IDesignService
    {
        public async Task<DownloadModel> DownloadAsync(int id)
        {
            try
            {
                var design = await dbContext.OrderDesigns
                    .FindAsync(id);

                var container = blobServiceClient.GetBlobContainerClient("order-designs");
                var blob = container.GetBlobClient(design!.Name);
                var response = await blob.DownloadContentAsync();
                var responseValue = response.Value;
                var result = new DownloadModel(responseValue.Content.ToStream(), responseValue.Details.ContentType, design.Name);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task PostCommentAsync(int id, string comment, Guid userId)
        {
            try
            {
                var design = await dbContext.OrderDesigns.Include(od => od.Order).SingleOrDefaultAsync(od => od.Id == id);

                if (design is null) return;

                // add comment
                var designComment = new OrderDesignComment
                {
                    OrderDesignId = id,
                    UserId = userId,
                    Comment = comment,
                    CreatedBy = userId
                };

                design.OrderDesignComments.Add(designComment);

                // update order status
                var order = design.Order;

                if (order.CheckerStatus == OrderStatuses.ForReview)
                {
                    order.CheckerStatus = OrderStatuses.ReuploadRequested;
                    order.UpdatedBy = userId;
                    order.UpdatedOn = DateTime.UtcNow;

                    await CreateOrderHistory(order.Id, order.CheckerStatus, order.CheckerId);
                }

                if (order.ArtistStatus == OrderStatuses.DesignSubmitted)
                {
                    order.ArtistStatus = OrderStatuses.ForReupload;
                    order.UpdatedBy = userId;
                    order.UpdatedOn = DateTime.UtcNow;

                    await CreateOrderHistory(order.Id, order.ArtistStatus, order.ArtistId);
                }
                
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task CreateOrderHistory(int orderId, string status, Guid? userId)
        {
            if (!userId.HasValue) return;

            try
            {
                await dbContext.OrderStatusHistories.AddAsync(new OrderStatusHistory
                {
                    OrderId = orderId,
                    Status = status,
                    CreatedBy = userId.Value,
                    CreatedOn = DateTime.UtcNow,
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
