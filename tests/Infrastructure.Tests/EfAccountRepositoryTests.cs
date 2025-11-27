using LocalFinanceManager.Domain.Entities;
using LocalFinanceManager.Infrastructure;
using LocalFinanceManager.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests;

public class EfAccountRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EfAccountRepository _repository;

    public EfAccountRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new EfAccountRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddAccount()
    {
        // Arrange
        var account = new Account
        {
            Name = "Test Account",
            AccountType = "bank",
            InitialBalance = 1000.00m,
            IsActive = true
        };

        // Act
        var result = await _repository.AddAsync(account);

        // Assert
        Assert.NotEqual(0, result.Id);
        Assert.Equal("Test Account", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAccount_WhenExists()
    {
        // Arrange
        var account = new Account
        {
            Name = "Test Account",
            AccountType = "bank",
            InitialBalance = 1000.00m,
            IsActive = true
        };
        await _repository.AddAsync(account);

        // Act
        var result = await _repository.GetByIdAsync(account.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Account", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllAccounts()
    {
        // Arrange
        await _repository.AddAsync(new Account { Name = "Account 1", AccountType = "bank", InitialBalance = 100m, IsActive = true });
        await _repository.AddAsync(new Account { Name = "Account 2", AccountType = "savings", InitialBalance = 200m, IsActive = true });

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetActiveAsync_ShouldReturnOnlyActiveAccounts()
    {
        // Arrange
        await _repository.AddAsync(new Account { Name = "Active Account", AccountType = "bank", InitialBalance = 100m, IsActive = true });
        await _repository.AddAsync(new Account { Name = "Inactive Account", AccountType = "bank", InitialBalance = 200m, IsActive = false });

        // Act
        var result = await _repository.GetActiveAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active Account", result[0].Name);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAccount()
    {
        // Arrange
        var account = new Account
        {
            Name = "Original Name",
            AccountType = "bank",
            InitialBalance = 1000.00m,
            IsActive = true
        };
        await _repository.AddAsync(account);

        // Act
        account.Name = "Updated Name";
        await _repository.UpdateAsync(account);

        // Assert
        var result = await _repository.GetByIdAsync(account.Id);
        Assert.Equal("Updated Name", result!.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteAccount_WhenExists()
    {
        // Arrange
        var account = new Account
        {
            Name = "To Delete",
            AccountType = "bank",
            InitialBalance = 1000.00m,
            IsActive = true
        };
        await _repository.AddAsync(account);

        // Act
        var result = await _repository.DeleteAsync(account.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await _repository.GetByIdAsync(account.Id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }
}
