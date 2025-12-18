using System.ComponentModel.DataAnnotations;

namespace CottonPrompt.Api.Messages.Users
{
    public class PaymentLinkRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string PaymentLink { get; set; }

     
    }
}
