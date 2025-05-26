using OpenAI_API;
using OpenAI_API.Chat;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Movie_BE.Models;

namespace Movie_BE.Services
{
    public class MovieChatbotSearchService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MovieChatbotSearchService> _logger;

        public MovieChatbotSearchService(IConfiguration configuration, ILogger<MovieChatbotSearchService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetMoviesByDescriptionAsync(string description)
        {
            try
            {
                // Initialize Open AI
                var openAiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(openAiKey))
                {
                    _logger.LogError("OpenAI API key is missing in configuration.");
                    throw new InvalidOperationException("OpenAI API key is not configured.");
                }
                var openAi = new OpenAIAPI(openAiKey);

                // Create prompt for Open AI to extract movie titles and criteria
                var prompt = $"Based on this description: '{description}', suggest 1-3 specific titles that can be tv series or movie matching the described theme or story. If the description involves a weak protagonist (e.g., a hunter) rising to become a powerful figure (e.g., an emperor or ruler) in a modern or fantasy setting with a ranking system, prioritize titles like 'Solo Leveling'. For other descriptions, suggest relevant titles across anime, live-action, or theatrical films. Include key search criteria (e.g., genre, year, actors, themes, keywords). Return the result in a structured JSON format like {{\"MovieTitles\": [], \"Genre\": \"\", \"Year\": \"\", \"Actors\": \"\", \"Themes\": \"\", \"Keywords\": \"\"}}. Ensure titles are specific, relevant, and diverse.";
                var chatRequest = new ChatRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new[]
                    {
                        new ChatMessage(ChatMessageRole.System, "You are a helpful assistant that suggests 1-3 specific tv series, or movie based on user descriptions, returning them in JSON format. Prioritize 'Solo Leveling' for descriptions about a weak hunter becoming a powerful ruler. Ensure suggestions are relevant and diverse across genres and formats."),
                        new ChatMessage(ChatMessageRole.User, prompt)
                    },
                    MaxTokens = 300,
                    Temperature = 0.5
                };

                _logger.LogInformation("Sending request to Open AI with prompt: {Prompt}", prompt);
                var result = await openAi.Chat.CreateChatCompletionAsync(chatRequest);
                var searchCriteriaJson = result.Choices[0].Message.Content;

                // Validate JSON
                if (string.IsNullOrWhiteSpace(searchCriteriaJson))
                {
                    _logger.LogError("Open AI returned empty response for description: {Description}", description);
                    throw new Exception("Open AI returned empty response.");
                }

                _logger.LogInformation("Received Open AI response: {Json}", searchCriteriaJson);

                // Parse Open AI response to ensure it's valid JSON
                MovieSearchCriteria searchCriteria;
                try
                {
                    searchCriteria = JsonSerializer.Deserialize<MovieSearchCriteria>(searchCriteriaJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Open AI response as JSON: {Json}", searchCriteriaJson);
                    throw new Exception("Failed to parse Open AI response as JSON.", ex);
                }

                if (searchCriteria == null)
                {
                    _logger.LogError("Parsed search criteria is null for response: {Json}", searchCriteriaJson);
                    throw new Exception("Parsed search criteria is null.");
                }

                // Return Open AI response directly
                return searchCriteriaJson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing movie search for description: {Description}", description);
                throw new Exception($"Error processing movie search: {ex.Message}", ex);
            }
        }
    }
}