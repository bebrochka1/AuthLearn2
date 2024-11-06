using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    Role admin = new Role { Id = 1, Name = "Admin" };
    Role user = new Role { Id = 2, Name = "User" };
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(u => u.HasData(new User[]
        {
            new User { Id = 1, Name = "Admin", Email = "admin@email.com", Password = "secretAdminPassword", RoleId = 1 },
            new User { Id = 2, Name = "Stas", Email = "tim4chenko.stas@gmail.com", Password = "12345", RoleId = 2 }
        }
        ));

        modelBuilder.Entity<User>().HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId);

        modelBuilder.Entity<User>().ToTable("Users");

        modelBuilder.Entity<Role>(u => u.HasData(new Role[]
        {
            admin, user
        }));

        modelBuilder.Entity<Role>().HasKey(r => r.Id);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Users)
            .WithOne(u => u.Role);

        modelBuilder.Entity<Role>().ToTable("Roles");
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
}
