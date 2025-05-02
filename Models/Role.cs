namespace KanbanBoardAPI.Models;

public class Role
{
    public int RoleId { get; set; }
    public required string RoleName { get; set; } = string.Empty;

    // Инициализация коллекции
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}