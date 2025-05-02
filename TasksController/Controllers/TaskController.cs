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

        public TasksController(
            AppDbContext context,
            ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KanbanTask>>> GetAllTasks()
        {
            try
            {
                var tasks = await _context.KanbanTasks
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tasks");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<KanbanTask>> GetTaskById(int id)
        {
            try
            {
                var task = await _context.KanbanTasks
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.AssignedByUser)
                    .FirstOrDefaultAsync(t => t.TaskId == id);

                if (task == null)
                {
                    return NotFound();
                }

                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting task with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<ActionResult<KanbanTask>> CreateTask(KanbanTask task)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                task.AssignedByUserId = currentUserId;

                _context.KanbanTasks.Add(task);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTaskById), new { id = task.TaskId }, task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> UpdateTask(int id, KanbanTask task)
        {
            try
            {
                if (id != task.TaskId)
                {
                    return BadRequest();
                }

                _context.Entry(task).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                _logger.LogError(ex, $"Concurrency error updating task {id}");
                return StatusCode(500, "Internal server error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating task {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] string status)
        {
            try
            {
                var task = await _context.KanbanTasks.FindAsync(id);
                if (task == null)
                {
                    return NotFound();
                }

                task.Status = status;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating status for task {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var task = await _context.KanbanTasks.FindAsync(id);
                if (task == null)
                {
                    return NotFound();
                }

                _context.KanbanTasks.Remove(task);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting task {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        private bool TaskExists(int id)
        {
            return _context.KanbanTasks.Any(e => e.TaskId == id);
        }
    }
}