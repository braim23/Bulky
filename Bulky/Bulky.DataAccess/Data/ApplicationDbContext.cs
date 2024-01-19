using Bulky.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Company> Compaiens { get; set; }
    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<ApplicationUser> ApplicationUsers { get; set; }
    public DbSet<OrderHeader> OrderHeaders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Action", DisplayOrder = 1 },
            new Category { Id = 2, Name = "SciFi", DisplayOrder = 2 },
            new Category { Id = 3, Name = "History", DisplayOrder = 3 }
            );

        modelBuilder.Entity<Company>().HasData(
            new Company
            {
                Id = 1,
                Name = "Big Tech",
                StreetAddress = "123 Tech St.",
                City = "Tech City",
                PostalCode = "12345",
                State = "IL",
                PhoneNumber = "1234567891"
            },
            new Company
            {
                Id = 2,
                Name = "Big Books",
                StreetAddress = "123 Book St.",
                City = "Book City",
                PostalCode = "12345",
                State = "IL",
                PhoneNumber = "1234567891"
            },
            new Company
            {
                Id = 3,
                Name = "Big Cook",
                StreetAddress = "123 Cook St.",
                City = "Cook City",
                PostalCode = "12345",
                State = "IL",
                PhoneNumber = "1234567891"
            }

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
                Price = 90,
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
                Price = 90,
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
                Price = 90,
                Price50 = 85,
                Price100 = 80,
                CategoryId = 3,
                ImageUrl = ""
            });
    }
}
