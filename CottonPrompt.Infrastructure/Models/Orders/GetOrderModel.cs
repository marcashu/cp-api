using CottonPrompt.Infrastructure.Models.DesignBrackets;
using CottonPrompt.Infrastructure.Models.Designs;
using CottonPrompt.Infrastructure.Models.OutputSizes;
using CottonPrompt.Infrastructure.Models.PrintColors;

namespace CottonPrompt.Infrastructure.Models.Orders
{
    public record GetOrderModel(
        int Id,
        string OrderNumber,
        bool Priority,
        string Concept,
        PrintColor PrintColor,
        DesignBracket DesignBracket,
        OutputSize OutputSize,
        int UserGroupId,
        string CustomerEmail,
        IEnumerable<ImageReferenceModel> ImageReferences,
        DesignModel? Design,
        IEnumerable<DesignModel> PreviousDesigns,
        string ArtistStatus,
        string CheckerStatus,
        string CustomerStatus,
        Guid? ArtistId,
        Guid? CheckerId,
        Guid? AuthorId,
        string? AuthorName,
        string UserGroup,
        bool IsChangeRequest,
        DateTime? CheckerRemovedOn
    );
}
