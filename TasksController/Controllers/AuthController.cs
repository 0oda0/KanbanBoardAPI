using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KanbanBoardAPI.Data;
using KanbanBoardAPI.Models;
using KanbanBoardAPI.TasksController.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace KanbanBoardAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterModel model)
        {
            try
            {
                // Валидация входных данных
                if (string.IsNullOrWhiteSpace(model.Username))
                    return BadRequest(new { Error = "Username is required" });

                if (string.IsNullOrWhiteSpace(model.Email))
                    return BadRequest(new { Error = "Email is required" });

                if (string.IsNullOrWhiteSpace(model.Password))
                    return BadRequest(new { Error = "Password is required" });

                if (model.Password.Length < 6)
                    return BadRequest(new { Error = "Password must be at least 6 characters" });

                // Проверка уникальности
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    return Conflict(new { Error = "Username already exists" });

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    return Conflict(new { Error = "Email already in use" });

                // Создание нового пользователя
                var user = new User
                {
                    Username = model.Username.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    RoleId = 1 // Роль по умолчанию
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Генерация токена
                var token = GenerateJwtToken(user);

                return Ok(new AuthResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Role = "User",
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Login attempt for {loginDto.Username}");

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning($"Invalid login attempt for {loginDto.Username}");
                    return Unauthorized(new
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    });
                }

                var token = GenerateJwtToken(user);

                _logger.LogInformation($"User {user.Username} logged in successfully");

                return Ok(new
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Role = user.Role?.RoleName ?? "User",
                        Token = token
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse<object>(false, "User not authenticated"));
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));

                if (user == null)
                {
                    return NotFound(new ApiResponse<object>(false, "User not found"));
                }

                return Ok(new ApiResponse<UserDto>(
                    true,
                    "User data retrieved",
                    new UserDto(
                        user.UserId,
                        user.Username,
                        user.Email,
                        user.RoleId
                    )
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new ApiResponse<object>(false, "Internal server error"));
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Модели запросов и ответов
    public class RegisterModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }

    public class UserResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
    }
}