namespace KanbanBoardAPI.Models;

public class KanbanTask
{
    public int TaskId { get; set; }
    public required string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    private string _status = "todo";
    public string Status
    {
        get => _status;
        set => _status = value?.ToLower() ?? "todo";
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }

    // Navigation properties
    public int? AssignedToUserId { get; set; }
    public virtual User? AssignedToUser { get; set; }

    public required int AssignedByUserId { get; set; }
    public virtual User? AssignedByUser { get; set; }
}