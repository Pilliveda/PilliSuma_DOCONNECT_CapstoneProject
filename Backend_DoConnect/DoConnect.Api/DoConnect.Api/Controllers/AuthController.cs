using DoConnect.Api.Data;
using DoConnect.Api.Dtos;
using DoConnect.Api.Models;
using DoConnect.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoConnect.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtTokenService _jwt;

        public AuthController(AppDbContext db, JwtTokenService jwt)
        {
            _db = db; _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
                return Conflict(new { message = "Username or email already exists" });

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = RoleType.User
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Created("", new { user.Id, user.Username, user.Email });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Invalid credentials" });

            var (token, expires) = _jwt.Create(user);
            return Ok(new { token, expires, user = new { user.Id, user.Username, role = user.Role.ToString() }});
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                // id = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
             id = User.FindFirst("sub")?.Value 
             ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,


                // username = User.Identity?.Name ?? User.FindFirst("unique_name")?.Value,
                username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? User.FindFirst("unique_name")?.Value,

                email = User.FindFirst("email")?.Value,
                role = User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            });
        }
    }
}
