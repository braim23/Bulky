using Bulky.Models;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Action", DisplayOrder = 1 },
            new Category { Id = 2, Name = "SciFi", DisplayOrder = 2 },
            new Category { Id = 3, Name = "History", DisplayOrder = 3 }
            );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Title = "Sunnah",
                Description = "A biography about our beloved prophet",
                ISBN = "SWD999901",
                Author="Hamza",
                ListPrice = 99,
                Price50 = 85,
                Price100 = 80,
                CategoryId = 1,
                ImageUrl = ""
            },
            new Product
            {
                Id = 2,
                Title = "Omar",
                Description = "A biography about our beloved companion Omar bin Alkhattab",
                ISBN = "SWD999902",
                Author = "Hamza",
                ListPrice = 99,
                Price50 = 85,
                Price100 = 80,
                CategoryId = 2,
                ImageUrl = ""
            },
            new Product
            {
                Id = 3,
                Title = "Abu Bakr",
                Description = "A biography about our beloved companion Abu Bakr",
                ISBN = "SWD999903",
                Author = "Hamza",
                ListPrice = 99,
                Price50 = 85,
                Price100 = 80,
                CategoryId = 3,
                ImageUrl = ""
            });
    }
}
