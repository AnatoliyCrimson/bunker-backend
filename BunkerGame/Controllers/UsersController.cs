// Controllers/UsersController.cs
using BunkerGame.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BunkerGame.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UsersController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = users.Select(u => new
        {
            u.Id,
            u.Name,
            u.Email,
            u.CreatedAt
        });
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var result = new
        {
            user.Id,
            user.Name,
            user.Email,
            user.CreatedAt
        };
        return Ok(result);
    }
}