using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.Models;

namespace OrderInventory.Api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedCategoriesAsync(AppDbContext context)
    {
        string[] categoryNames =
        {
            "Electronics",
            "Clothing",
            "Books",
            "Home & Kitchen"
        };

        foreach (var name in categoryNames)
        {
            bool exists = await context.Categories
                .AnyAsync(c => c.Name == name);

            if (!exists)
            {
                context.Categories.Add(new Category
                {
                    Name = name
                });
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedProductsAsync(AppDbContext context)
    {
        var products = new[]
        {
            new
            {
                Name = "Wireless Mouse",
                Price = 450.50m,
                StockQuantity = 20,
                CategoryName = "Electronics"
            },
            new
            {
                Name = "Mechanical Keyboard",
                Price = 1200m,
                StockQuantity = 15,
                CategoryName = "Electronics"
            },
            new
            {
                Name = "C# Programming Book",
                Price = 350m,
                StockQuantity = 30,
                CategoryName = "Books"
            },
            new
            {
                Name = "T-Shirt",
                Price = 250m,
                StockQuantity = 40,
                CategoryName = "Clothing"
            }
        };

        foreach (var item in products)
        {
            bool exists = await context.Products
                .AnyAsync(p => p.Name == item.Name);

            if (exists)
                continue;

            var category = await context.Categories
                .FirstAsync(c => c.Name == item.CategoryName);

            context.Products.Add(new Product
            {
                Name = item.Name,
                Price = item.Price,
                StockQuantity = item.StockQuantity,
                CategoryId = category.Id
            });
        }

        await context.SaveChangesAsync();
    }
}