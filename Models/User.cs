using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanBoardAPI.Models;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Username { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    [ForeignKey("Role")]
    public int RoleId { get; set; }

    // Навигационные свойства
    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<KanbanTask> AssignedKanbanTasks { get; set; } = new List<KanbanTask>();
    public virtual ICollection<KanbanTask> CreatedKanbanTasks { get; set; } = new List<KanbanTask>();
}