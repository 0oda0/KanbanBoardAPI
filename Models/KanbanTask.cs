namespace KanbanBoardAPI.Models;

public class KanbanTask
{
    public int TaskId { get; set; }
    public required string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "ToDo";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }

    // Навигационные свойства с явной инициализацией
    public int? AssignedToUserId { get; set; }
    public virtual User? AssignedToUser { get; set; }

    public required int AssignedByUserId { get; set; }
    public virtual User? AssignedByUser { get; set; }
}