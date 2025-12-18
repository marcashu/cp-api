using System.ComponentModel.DataAnnotations;

namespace CottonPrompt.Api.Messages.Rates
{
    public class UpdateRatesRequest
    {
        [Required]
        public decimal QualityControlRate { get; set; }

        [Required]
        public decimal ChangeRequestRate { get; set; }

        [Required]
        public decimal ConceptAuthorRate { get; set; }

        [Required]
        public Guid UpdatedBy { get; set; }
    }
}
