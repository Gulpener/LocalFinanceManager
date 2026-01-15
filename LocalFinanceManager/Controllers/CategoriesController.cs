using Microsoft.AspNetCore.Mvc;
using LocalFinanceManager.Services;
using LocalFinanceManager.DTOs;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace LocalFinanceManager.Controllers;

/// <summary>
/// API controller for managing categories.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        CategoryService categoryService,
        IValidator<CreateCategoryDto> createValidator,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _createValidator = createValidator;
        _logger = logger;
    }

    /// <summary>
    /// Get all active categories.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var categories = await _categoryService.GetAllActiveAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Get a category by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
        {
            return NotFound(new { title = "Category not found", status = 404 });
        }
        return Ok(category);
    }

    /// <summary>
    /// Create a new category.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                title = "Validation failed",
                status = 400,
                errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage)
            });
        }

        var category = await _categoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>
    /// Archive a category (soft delete).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var success = await _categoryService.ArchiveAsync(id);
        if (!success)
        {
            return NotFound(new { title = "Category not found", status = 404 });
        }
        return NoContent();
    }
}
