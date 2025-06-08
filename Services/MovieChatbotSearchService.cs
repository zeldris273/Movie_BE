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
                var openAiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(openAiKey))
                {
                    _logger.LogError("OpenAI API key is missing in configuration.");
                    throw new InvalidOperationException("OpenAI API key is not configured.");
                }
                var openAi = new OpenAIAPI(openAiKey);

                var prompt = $"Based on this description: '{description}', suggest 1-3 specific titles that can be tv series or movie matching the described theme or story. Include key search criteria (e.g., genre, year, actors, themes, keywords). Return the result in a VALID JSON format with double quotes around property names, like {{\"MovieTitles\": [], \"Genre\": \"\", \"Year\": \"\", \"Actors\": \"\", \"Themes\": \"\", \"Keywords\": \"\"}}. Ensure titles are specific, relevant, and diverse. If the question is not related to movies, return {{\"Error\": \"Invalid question\"}}.";
                var chatRequest = new ChatRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new[]
                    {
                        new ChatMessage(ChatMessageRole.System, "You are a helpful assistant that suggests 1-3 specific tv series or movies based on user descriptions, returning them in VALID JSON format with double quotes around property names. If the question is not related to movies, return {\"Error\": \"Invalid question\"}."),
                        new ChatMessage(ChatMessageRole.User, prompt)
                    },
                    MaxTokens = 300,
                    Temperature = 0.5
                };

                _logger.LogInformation("Sending request to Open AI with prompt: {Prompt}", prompt);
                var result = await openAi.Chat.CreateChatCompletionAsync(chatRequest);
                var searchCriteriaJson = result.Choices[0].Message.Content.Trim();

                if (string.IsNullOrWhiteSpace(searchCriteriaJson))
                {
                    _logger.LogError("Open AI returned empty response for description: {Description}", description);
                    throw new Exception("Open AI returned empty response.");
                }

                _logger.LogInformation("Received Open AI response: {Json}", searchCriteriaJson);

                JsonElement jsonElement;
                try
                {
                    jsonElement = JsonSerializer.Deserialize<JsonElement>(searchCriteriaJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Open AI response as JSON: {Json}", searchCriteriaJson);
                    return JsonSerializer.Serialize(new { Error = "Invalid response format from Open AI" });
                }

                if (jsonElement.TryGetProperty("Error", out var errorElement))
                {
                    return JsonSerializer.Serialize(new { Error = errorElement.GetString() });
                }

                MovieSearchCriteria searchCriteria;
                try
                {
                    searchCriteria = JsonSerializer.Deserialize<MovieSearchCriteria>(searchCriteriaJson);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Open AI response as MovieSearchCriteria: {Json}", searchCriteriaJson);
                    throw new Exception("Failed to parse Open AI response as MovieSearchCriteria.", ex);
                }

                if (searchCriteria == null)
                {
                    _logger.LogError("Parsed search criteria is null for response: {Json}", searchCriteriaJson);
                    throw new Exception("Parsed search criteria is null.");
                }

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