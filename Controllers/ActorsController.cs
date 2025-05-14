using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movie_BE.Models;

[Route("api/[controller]")]
[ApiController]
public class ActorsController : ControllerBase
{
    private readonly MovieDbContext _context;

    public ActorsController(MovieDbContext context)
    {
        _context = context;
    }

    // POST: api/actors/find-or-create
    [HttpPost("find-or-create")]
    public async Task<IActionResult> FindOrCreateActor([FromBody] ActorNameRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Tên diễn viên không được để trống");

        var existingActor = await _context.Actors
            .FirstOrDefaultAsync(a => a.Name.ToLower() == request.Name.ToLower());

        if (existingActor != null)
        {
            return Ok(existingActor);
        }

        var newActor = new Actor
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        _context.Actors.Add(newActor);
        await _context.SaveChangesAsync();

        return Ok(newActor);
    }
}

public class ActorNameRequest
{
    public string Name { get; set; }
}
