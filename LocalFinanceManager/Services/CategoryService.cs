using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.DTOs;
using LocalFinanceManager.Models;

namespace LocalFinanceManager.Services;

/// <summary>
/// Service for managing category operations.
/// </summary>
public class CategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active categories.
    /// </summary>
    public async Task<List<CategoryDto>> GetAllActiveAsync()
    {
        _logger.LogInformation("Retrieving all active categories");
        var categories = await _categoryRepository.GetActiveAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        }).ToList();
    }

    /// <summary>
    /// Get a category by ID.
    /// </summary>
    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving category with ID: {CategoryId}", id);
        var category = await _categoryRepository.GetByIdAsync(id);
        return category != null ? new CategoryDto { Id = category.Id, Name = category.Name } : null;
    }

    /// <summary>
    /// Create a new category.
    /// </summary>
    public async Task<CategoryDto> CreateAsync(CreateCategoryDto request)
    {
        _logger.LogInformation("Creating new category: {Name}", request.Name);

        var category = new Category
        {
            Name = request.Name,
            IsArchived = false
        };

        await _categoryRepository.AddAsync(category);

        _logger.LogInformation("Category created with ID: {CategoryId}", category.Id);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }

    /// <summary>
    /// Archive a category.
    /// </summary>
    public async Task<bool> ArchiveAsync(Guid id)
    {
        _logger.LogInformation("Archiving category with ID: {CategoryId}", id);

        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            _logger.LogWarning("Category not found with ID: {CategoryId}", id);
            return false;
        }

        await _categoryRepository.ArchiveAsync(id);
        _logger.LogInformation("Category archived successfully: {CategoryId}", id);
        return true;
    }
}
