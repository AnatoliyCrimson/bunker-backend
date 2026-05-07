using BunkerGame.Data;
using BunkerGame.Models;
using BunkerGame.DTOs.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiPromptsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AiPromptsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/AiPrompts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AiPrompt>>> GetPrompts()
    {
        return await _context.AiPrompts.OrderBy(p => p.Code).ToListAsync();
    }

    // GET: api/AiPrompts/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<AiPrompt>> GetPrompt(Guid id)
    {
        var prompt = await _context.AiPrompts.FindAsync(id);

        if (prompt == null)
        {
            return NotFound();
        }

        return prompt;
    }

    // GET: api/AiPrompts/code/{code}
    [HttpGet("code/{code}")]
    public async Task<ActionResult<AiPrompt>> GetPromptByCode(string code)
    {
        var prompt = await _context.AiPrompts.FirstOrDefaultAsync(p => p.Code == code);

        if (prompt == null)
        {
            return NotFound();
        }

        return prompt;
    }

    // POST: api/AiPrompts
    [HttpPost]
    public async Task<ActionResult<AiPrompt>> CreatePrompt([FromBody] CreateAiPromptDto dto)
    {
        var prompt = new AiPrompt
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            SystemPrompt = dto.SystemPrompt,
            UserPromptTemplate = dto.UserPromptTemplate,
            Temperature = dto.Temperature,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.AiPrompts.Add(prompt);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await PromptExistsByCode(prompt.Code))
            {
                return Conflict("Prompt with this code already exists.");
            }
            throw;
        }

        return CreatedAtAction(nameof(GetPrompt), new { id = prompt.Id }, prompt);
    }

    // PUT: api/AiPrompts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePrompt(Guid id, [FromBody] EditAiPromptDto dto)
    {
        var prompt = await _context.AiPrompts.FindAsync(id);

        if (prompt == null)
        {
            return NotFound();
        }

        prompt.Code = dto.Code;
        prompt.SystemPrompt = dto.SystemPrompt;
        prompt.UserPromptTemplate = dto.UserPromptTemplate;
        prompt.Temperature = dto.Temperature;
        prompt.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await PromptExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/AiPrompts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrompt(Guid id)
    {
        var prompt = await _context.AiPrompts.FindAsync(id);
        if (prompt == null)
        {
            return NotFound();
        }

        _context.AiPrompts.Remove(prompt);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> PromptExists(Guid id)
    {
        return await _context.AiPrompts.AnyAsync(e => e.Id == id);
    }

    private async Task<bool> PromptExistsByCode(string code)
    {
        return await _context.AiPrompts.AnyAsync(e => e.Code == code);
    }
}
