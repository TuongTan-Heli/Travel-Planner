//intent extract
//get best locations
//get Final plan

using System.Text.Json;
using TravelPlanner.Features.Chat.Models;

public class PromptBuilder
{
    public static string Build(TravelStage stage, TravelPromptContext context, TravelResponse response, string input = "")
    {
        return stage switch
        {
            TravelStage.IntentExtraction => PromptTemplate.BuildIntentExtractionPrompt(input, JsonSerializer.Serialize(context)),
            // TravelStage.RequirementCollection => PromptTemplate.BuildRequirementPrompt(context),
            // TravelStage.LocationSelection => PromptTemplate.BuildLocationSelectionPrompt(context),
            // TravelStage.ItineraryGeneration => PromptTemplate.BuildItineraryGenerationPrompt(context),
            TravelStage.FinalPresentation => PromptTemplate.BuildFinalPresentationPrompt(context, response),
            _ => throw new NotImplementedException($"Prompt template for stage {stage} is not implemented.")
        };
    }
}