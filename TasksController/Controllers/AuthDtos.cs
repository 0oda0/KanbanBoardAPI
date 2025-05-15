using Microsoft.AspNetCore.Mvc;

namespace KanbanBoardAPI.TasksController.Controllers
{
    public record ApiResponse<T>(bool Success, string Message, T? Data = null) where T : class;
    public record RegisterDto(string Username, string Email, string Password);
    public record LoginDto(string Username, string Password);
    public record AuthResponse(int UserId, string Username, string Role, string Token);
    public record UserDto(int UserId, string Username, string Email, int RoleId);
}
