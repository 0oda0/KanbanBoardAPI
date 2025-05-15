using KanbanBoardAPI.Data;
using KanbanBoardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KanbanBoardAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TasksController> _logger;

        public TasksController(AppDbContext context, ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllTasks()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var tasks = await _context.KanbanTasks
                    .Where(t => t.AssignedToUserId == userId || t.AssignedByUserId == userId)
                    .ToListAsync();

                return Ok(new { success = true, data = tasks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tasks");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetTaskById(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var task = await _context.KanbanTasks
                    .FirstOrDefaultAsync(t => t.TaskId == id &&
                        (t.AssignedToUserId == userId || t.AssignedByUserId == userId));

                if (task == null)
                    return NotFound(new { success = false, message = "Task not found" });

                return Ok(new { success = true, data = task });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting task {id}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateTask([FromBody] TaskCreateRequest request)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var task = new KanbanTask
                {
                    Title = request.Title,
                    Description = request.Description,
                    Status = request.Status?.ToLower() ?? "todo",
                    Deadline = request.Deadline,
                    AssignedByUserId = currentUserId,
                    AssignedToUserId = currentUserId
                };

                _context.KanbanTasks.Add(task);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = task });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTask(int id, [FromBody] TaskUpdateRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var task = await _context.KanbanTasks
                    .FirstOrDefaultAsync(t => t.TaskId == id &&
                        (t.AssignedToUserId == userId || t.AssignedByUserId == userId));

                if (task == null)
                    return NotFound(new { success = false, message = "Task not found" });

                task.Title = request.Title;
                task.Description = request.Description;
                task.Status = request.Status?.ToLower() ?? task.Status;
                task.Deadline = request.Deadline;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = task });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating task {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateTaskStatus(int id, [FromBody] StatusUpdateRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var task = await _context.KanbanTasks
                    .FirstOrDefaultAsync(t => t.TaskId == id &&
                        (t.AssignedToUserId == userId || t.AssignedByUserId == userId));

                if (task == null)
                    return NotFound(new { success = false, message = "Task not found" });

                task.Status = request.Status?.ToLower() ?? task.Status;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = task });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating task {id} status");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var task = await _context.KanbanTasks
                    .FirstOrDefaultAsync(t => t.TaskId == id && t.AssignedByUserId == userId);

                if (task == null)
                    return NotFound(new { success = false, message = "Task not found" });

                _context.KanbanTasks.Remove(task);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Task deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting task {id}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }

    public class TaskCreateRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "todo";
        public DateTime? Deadline { get; set; }
    }

    public class TaskUpdateRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? Deadline { get; set; }
    }

    public class StatusUpdateRequest
    {
        public string Status { get; set; }
    }
}