using KanbanBoardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace KanbanBoardAPI.Data
{
    public static class SeedData
    {
        public static async Task Initialize(AppDbContext context)
        {
            // Проверяем, есть ли уже роли
            if (!await context.Roles.AnyAsync())
            {
                await context.Roles.AddRangeAsync(
                    new Role { RoleName = "User" },
                    new Role { RoleName = "Manager" },
                    new Role { RoleName = "Admin" }
                );
                await context.SaveChangesAsync();
            }

            // Проверяем, есть ли тестовый администратор
            if (!await context.Users.AnyAsync(u => u.Username == "admin"))
            {
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                if (adminRole != null)
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@example.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        RoleId = adminRole.RoleId
                    };
                    await context.Users.AddAsync(adminUser);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}