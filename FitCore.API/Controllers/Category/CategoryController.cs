using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Category;
using FitCore.Shared.DTOs.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{
    // GET: api/Category
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var result = await categoryService.GetAllCategoriesAsync();
        return Ok(result);
    }

    // GET: api/Category/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var category = await categoryService.GetCategoryByIdAsync(id);
        return category == null ? NotFound(new { Message = "The category not found." }) : Ok(category);
    }

    // POST: api/Category
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await categoryService.CreateCategoryAsync(dto);
            return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    // PUT: api/Category/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await categoryService.UpdateCategoryAsync(id, dto);
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

    // DELETE: api/Category/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            await categoryService.DeleteCategoryAsync(id);
            return NoContent();
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
}
