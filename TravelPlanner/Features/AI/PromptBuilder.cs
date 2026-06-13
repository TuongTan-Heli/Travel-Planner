//intent extract
//get best locations
//get Final plan

using TravelPlanner.Features.Chat.Models;

public class PromptBuilder
{
    public static string Build(TravelStage stage, TravelPromptContext context, string input = "")
    {
        return stage switch
        {
            TravelStage.IntentExtraction => PromptTemplate.BuildIntentExtractionPrompt(input),
            TravelStage.RequirementCollection => PromptTemplate.BuildRequirementPrompt(context),
            TravelStage.LocationSelection => PromptTemplate.BuildLocationSelectionPrompt(context),
            TravelStage.ItineraryGeneration => PromptTemplate.BuildItineraryGenerationPrompt(context),
            TravelStage.FinalPresentation => PromptTemplate.BuildFinalPresentationPrompt(context),
            _ => throw new NotImplementedException($"Prompt template for stage {stage} is not implemented.")
        };
    }
}