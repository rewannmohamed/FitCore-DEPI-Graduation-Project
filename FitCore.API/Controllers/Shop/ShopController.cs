using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.IShopService;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs.Cart;
using FitCore.Shared.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class ShopController(IShopService shopService) : ControllerBase
{
    private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts() => Ok(await shopService.GetAllProductsAsync());

    [HttpPost("cart")]
    public async Task<IActionResult> AddToCart([FromBody] AddCartItemDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetUserId();
        var result = await shopService.AddToCartAsync(userId, dto);
        return result ? Ok(1) : BadRequest("Failed Add to cart");
    }

    [HttpGet("cart")]
    public async Task<IActionResult> GetCart() => Ok(await shopService.GetUserCartAsync(GetUserId()));

    [HttpDelete("cart/{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        var result = await shopService.RemoveFromCartAsync(cartItemId, GetUserId());
        return result ? Ok(1) : NotFound("The item not found");
    }

    [HttpPatch("cart/{cartItemId}")]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, [FromBody] int quantity)
    {
        var result = await shopService.UpdateCartItemQuantityAsync(cartItemId, quantity, GetUserId());
        return result ? Ok() : BadRequest();
    }

    //[HttpPost("checkout")]
    //public async Task<IActionResult> Checkout([FromBody] CheckoutDTO dto)
    //{
    //    var invoiceId = await shopService.CheckoutAsync(GetUserId(), dto);
    //    return Ok(new { InvoiceId = invoiceId, Message = "Purchase completed successfully!" });
    //}


    [HttpGet("admin/products")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? categoryId = null)
    {
        var result = await shopService.GetAdminProductsAsync(page, pageSize, searchTerm, categoryId);
        return Ok(result);
    }

    [HttpGet("products/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var product = await shopService.GetProductByIdAsync(id);
        return product == null ? NotFound(new { Message = "Product not found" }) : Ok(product);
    }

    [HttpPost("products")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await shopService.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetProductById), new { id = result.ProductID }, result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("products/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await shopService.UpdateProductAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("products/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            await shopService.DeleteProductAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    // =========================================================
    // Admin: Lookups
    // =========================================================
    [HttpGet("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCategories() => Ok(await shopService.GetCategoriesAsync());

    [HttpGet("suppliers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSuppliers() => Ok(await shopService.GetSuppliersAsync());

    [HttpGet("inventory")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetInventory([FromQuery] int? productId = null)
    {
        var result = await shopService.GetInventoryAsync(productId);
        return Ok(result);
    }

    [HttpPost("inventory")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddInventory([FromBody] AddInventoryDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await shopService.AddInventoryAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

}