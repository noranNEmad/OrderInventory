
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderInventory.Api.Data;
using OrderInventory.Api.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace OrderInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponseDto<ProductResponseDto>>> GetAll(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] int? categoryId = null)
    {
        // 1. Validate pagination
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(
                "Page must be at least 1 and pageSize must be between 1 and 100.");
        }

        // 2. Build query
        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        // 3. Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // 4. Count products before pagination
        var totalCount = await query.CountAsync();

        // 5. Calculate total pages
        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        // 6. Apply pagination
        var products = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 7. Map to DTOs
        var responseItems = products.Select(product =>
            new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name
            }).ToList();

        // 8. Build response
        var response = new PaginationResponseDto<ProductResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = responseItems
        };

        return Ok(response);
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _context.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                StockQuantity = p.StockQuantity
                ,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [Authorize(Policy = "CanManageProducts")]
    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create(
            CreateProductDto newProduct)
    {
        // 1. Check if the category exists
        if (newProduct.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == newProduct.CategoryId.Value);

            if (!categoryExists)
            {
                return BadRequest("Category does not exist.");
            }
        }

        // 2. Create the product
        var product = new Models.Product
        {
            Name = newProduct.Name,
            Price = newProduct.Price,
            StockQuantity = newProduct.StockQuantity,
            CategoryId = newProduct.CategoryId
        };

        // 3. Save to database
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // 4. Create response
        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = product.CategoryId.HasValue
    ? await _context.Categories
        .Where(c => c.Id == product.CategoryId.Value)
        .Select(c => c.Name)
        .FirstOrDefaultAsync()
    : null
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            response);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> Update(
            int id,
            UpdateProductDto updatedProduct)
    {
        // 1. Find the product
        var product = await _context.Products.FindAsync(id);

        if (product is null)
        {
            return NotFound("Product not found.");
        }

        // 2. Check if the category exists
        if (updatedProduct.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == updatedProduct.CategoryId.Value);

            if (!categoryExists)
            {
                return BadRequest("Category does not exist.");
            }
        }

        // 3. Update product properties
        product.Name = updatedProduct.Name;
        product.Price = updatedProduct.Price;
        product.StockQuantity = updatedProduct.StockQuantity;
        product.CategoryId = updatedProduct.CategoryId;

        // 4. Save changes
        await _context.SaveChangesAsync();

        // 5. Get category name
        var categoryName = product.CategoryId.HasValue
            ? await _context.Categories
                .Where(c => c.Id == product.CategoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync()
            : null;

        // 6. Create response
        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = categoryName
        };

        return Ok(response);
    }

    [Authorize]
    [HttpPost("{id:int}/purchase")]
    public async Task<ActionResult<ProductResponseDto>> Purchase(
            int id,
            StockUpdateDto request)
    {
        // 1. Find the product
        var product = await _context.Products.FindAsync(id);

        if (product is null)
        {
            return NotFound("Product not found.");
        }

        // 2. Check available stock
        if (request.Quantity > product.StockQuantity)
        {
            return BadRequest("Insufficient stock.");
        }

        // 3. Reduce stock
        product.StockQuantity -= request.Quantity;

        // 4. Save changes
        await _context.SaveChangesAsync();

        // 5. Get category name
        var categoryName = product.CategoryId.HasValue
            ? await _context.Categories
                .Where(c => c.Id == product.CategoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync()
            : null;

        // 6. Return updated product
        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = categoryName
        };

        return Ok(response);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{id:int}/restock")]
    public async Task<ActionResult<ProductResponseDto>> Restock(
            int id,
            StockUpdateDto request)
    {
        // 1. Find the product
        var product = await _context.Products.FindAsync(id);

        if (product is null)
        {
            return NotFound("Product not found.");
        }

        // 2. Increase stock
        product.StockQuantity += request.Quantity;

        // 3. Save changes
        await _context.SaveChangesAsync();

        // 4. Get category name
        var categoryName = product.CategoryId.HasValue
            ? await _context.Categories
                .Where(c => c.Id == product.CategoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync()
            : null;

        // 5. Create response
        var response = new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = categoryName
        };

        return Ok(response);
    }

    // [HttpGet("test-error")]
    // public IActionResult TestError()
    // {
    //     throw new Exception("This is a test exception.");
    // }


}
