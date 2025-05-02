namespace KanbanBoardAPI.Models;

public class User
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public int RoleId { get; set; }

    // Навигационные свойства
    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<KanbanTask> AssignedKanbanTasks { get; set; } = new List<KanbanTask>();
    public virtual ICollection<KanbanTask> CreatedKanbanTasks { get; set; } = new List<KanbanTask>();
}