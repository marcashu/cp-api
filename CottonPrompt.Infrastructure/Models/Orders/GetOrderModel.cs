﻿using CottonPrompt.Infrastructure.Models.DesignBrackets;
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
        string CustomerEmail,
        IEnumerable<string> ImageReferences,
        DesignModel? Design,
        IEnumerable<DesignModel> PreviousDesigns,
        string ArtistStatus,
        string CheckerStatus,
        Guid? ArtistId,
        Guid? CheckerId
    );
}
