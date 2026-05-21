using BulkyWeb.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<OrderHeader> OrderHeaders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Action", DisplayOrder = 1 },
            new Category { Id = 2, Name = "SciFi", DisplayOrder = 2 },
            new Category { Id = 3, Name = "History", DisplayOrder = 3 });

        modelBuilder.Entity<Company>().HasData(
            new Company
            {
                Id = 1,
                Name = "Tech Solution",
                StreetAddress = "123 Tech St",
                City = "Tech City",
                PostalCode = "12121",
                State = "IL",
                PhoneNumber = "6669990000"
            },
            new Company
            {
                Id = 2,
                Name = "Vivid Books",
                StreetAddress = "999 Vid St",
                City = "Vid City",
                PostalCode = "66666",
                State = "IL",
                PhoneNumber = "7779990000"
            },
            new Company
            {
                Id = 3,
                Name = "Readers Club",
                StreetAddress = "999 Main St",
                City = "Lala land",
                PostalCode = "99999",
                State = "NY",
                PhoneNumber = "1113335555"
            });

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Title = "Fortune of Time",
                Author = "Nancy Spark",
                Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.",
                ISBN = "SWD9999001",
                ListPrice = 99,
                Price = 90,
                Price50 = 85,
                Price100 = 80,
                CategoryId = 1,
                ImageUrl = "/images/product/fortuneoftime.png"
            },
            new Product
            {
                Id = 2,
                Title = "Dark Skies",
                Author = "Julian Button",
                Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.",
                ISBN = "SWD9999002",
                ListPrice = 30,
                Price = 27,
                Price50 = 25,
                Price100 = 22,
                CategoryId = 1,
                ImageUrl = "/images/product/darkskies.png"
            },
            new Product
            {
                Id = 3,
                Title = "Vanish in the Sunset",
                Author = "Abby Muscles",
                Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.",
                ISBN = "SWD9999003",
                ListPrice = 25,
                Price = 23,
                Price50 = 22,
                Price100 = 20,
                CategoryId = 1,
                ImageUrl = "/images/product/vanishinthesunset.png"
            },
            new Product
            {
                Id = 4,
                Title = "Cotton Candy",
                Author = "Laura Phantom",
                Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.",
                ISBN = "SWD9999004",
                ListPrice = 70,
                Price = 65,
                Price50 = 60,
                Price100 = 55,
                CategoryId = 2,
                ImageUrl = "/images/product/cottoncandy.png"
            },
            new Product
            {
                Id = 5,
                Title = "Rock in the Ocean",
                Author = "Ron Parker",
                Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.",
                ISBN = "SWD9999005",
                ListPrice = 30,
                Price = 27,
                Price50 = 25,
                Price100 = 22,
                CategoryId = 2,
                ImageUrl = "/images/product/rockintheocean.png"
            },
            new Product
            {
                Id = 6,
                Title = "Leaves and Wonders",
                Author = "Laura Phantom",
                Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.",
                ISBN = "SWD9999006",
                ListPrice = 25,
                Price = 23,
                Price50 = 22,
                Price100 = 20,
                CategoryId = 3,
                ImageUrl = "/images/product/leavesandwonders.png"
            });
    }
}
