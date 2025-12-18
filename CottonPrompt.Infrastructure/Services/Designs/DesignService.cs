using Azure.Storage.Blobs;
using CottonPrompt.Infrastructure.Constants;
using CottonPrompt.Infrastructure.Entities;
using CottonPrompt.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CottonPrompt.Infrastructure.Services.Designs
{
    public class DesignService(CottonPromptContext dbContext, BlobServiceClient blobServiceClient, IConfiguration config) : IDesignService
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

                var artistEmail = await dbContext.Users.Where(u => u.Id == order.ArtistId).Select(u => u.Email).FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(artistEmail))
                {
                    await SendArtistEmailComment(artistEmail);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SendArtistEmailComment(string customerEmail)
        {
            var emailTemplates = "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Comment was added!</title>\r\n    <style>\r\n        body {\r\n            font-family: Arial, sans-serif;\r\n            background-color: #f4f4f4;\r\n            padding: 20px;\r\n        }\r\n        .email-container {\r\n            max-width: 600px;\r\n            margin: auto;\r\n            background: #ffffff;\r\n            padding: 20px;\r\n            border-radius: 8px;\r\n            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);\r\n            text-align: center;\r\n        }\r\n        .button {\r\n            display: inline-block;\r\n            padding: 10px 20px;\r\n            margin-top: 20px;\r\n            background-color: #007bff;\r\n            color: white;\r\n            text-decoration: none;\r\n            border-radius: 5px;\r\n        }\r\n    </style>\r\n</head>\r\n<body>\r\n    <div class=\"email-container\">\r\n        <h2>A checker commented on your work!</h2>\r\n        <p>A checker commented on your design. You can check the details in the app.</p>\r\n    </div>\r\n</body>\r\n</html>";
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
                Body = emailTemplates,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
                Subject = "Comment was added!",
            };
            await client.SendMailAsync(message);
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
