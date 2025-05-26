using Microsoft.AspNetCore.Mvc;
using Movie_BE.Models;
using Movie_BE.Services;
using System.Threading.Tasks;

namespace MovieChatbot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly MovieChatbotSearchService _movieChatbotSearchService;

        public ChatbotController(MovieChatbotSearchService movieChatbotSearchService)
        {
            _movieChatbotSearchService = movieChatbotSearchService;
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchMovies([FromBody] MovieDescriptionRequest request)
        {
            if (string.IsNullOrEmpty(request.Description))
            {
                return BadRequest("Description is required.");
            }

            var result = await _movieChatbotSearchService.GetMoviesByDescriptionAsync(request.Description);
            return Ok(result);
        }
    }
}