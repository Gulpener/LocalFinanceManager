using LocalFinanceManager.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using System.Text.Json;

namespace LocalFinanceManager.Tests.Services;

[TestFixture]
public class FilterStateServiceTests
{
    private Mock<IJSRuntime> _mockJsRuntime;
    private Mock<ILogger<FilterStateService>> _mockLogger;
    private FilterStateService _service;

    [SetUp]
    public void Setup()
    {
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockLogger = new Mock<ILogger<FilterStateService>>();
        _service = new FilterStateService(_mockJsRuntime.Object, _mockLogger.Object);
    }

    [Test]
    public async Task SaveFiltersAsync_StoresFiltersInLocalStorage()
    {
        // Arrange
        var filters = new FilterState
        {
            AssignmentStatus = "assigned",
            SuggestionStatus = "suggested",
            DateRange = "last30days",
            CustomStartDate = new DateTime(2026, 1, 1),
            CustomEndDate = new DateTime(2026, 1, 31),
            SelectedCategories = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            MinAmount = 10.50m,
            MaxAmount = 500.00m,
            SelectedAccountId = Guid.NewGuid()
        };

        string? capturedJson = null;
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.setItem",
                It.IsAny<object[]>()))
            .Callback<string, object[]>((_, args) => capturedJson = args[1] as string)
            .Returns(ValueTask.FromResult<object>(null!));

        // Act
        await _service.SaveFiltersAsync(filters);

        // Assert
        _mockJsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.setItem",
            It.Is<object[]>(args => 
                args.Length == 2 && 
                (string)args[0] == "transactionFilters")), 
            Times.Once);

        Assert.That(capturedJson, Is.Not.Null);
        var deserializedFilters = JsonSerializer.Deserialize<FilterState>(capturedJson!);
        Assert.Multiple(() =>
        {
            Assert.That(deserializedFilters!.AssignmentStatus, Is.EqualTo(filters.AssignmentStatus));
            Assert.That(deserializedFilters.SuggestionStatus, Is.EqualTo(filters.SuggestionStatus));
            Assert.That(deserializedFilters.DateRange, Is.EqualTo(filters.DateRange));
            Assert.That(deserializedFilters.SelectedCategories, Has.Count.EqualTo(2));
            Assert.That(deserializedFilters.MinAmount, Is.EqualTo(filters.MinAmount));
            Assert.That(deserializedFilters.MaxAmount, Is.EqualTo(filters.MaxAmount));
        });
    }

    [Test]
    public async Task LoadFiltersAsync_RetrievesFiltersFromLocalStorage()
    {
        // Arrange
        var filters = new FilterState
        {
            AssignmentStatus = "unassigned",
            DateRange = "last7days",
            SelectedCategories = new List<Guid> { Guid.NewGuid() },
            MinAmount = 20.00m
        };

        var json = JsonSerializer.Serialize(filters);
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(json);

        // Act
        var result = await _service.LoadFiltersAsync();

        // Assert
        _mockJsRuntime.Verify(js => js.InvokeAsync<string>(
            "localStorage.getItem",
            It.Is<object[]>(args => 
                args.Length == 1 && 
                (string)args[0] == "transactionFilters")), 
            Times.Once);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.AssignmentStatus, Is.EqualTo(filters.AssignmentStatus));
            Assert.That(result.DateRange, Is.EqualTo(filters.DateRange));
            Assert.That(result.SelectedCategories, Has.Count.EqualTo(1));
            Assert.That(result.MinAmount, Is.EqualTo(filters.MinAmount));
        });
    }

    [Test]
    public async Task LoadFiltersAsync_ReturnsNull_WhenNoFiltersSaved()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync((string)null!);

        // Act
        var result = await _service.LoadFiltersAsync();

        // Assert
        Assert.That(result, Is.Null);
        _mockJsRuntime.Verify(js => js.InvokeAsync<string>(
            "localStorage.getItem",
            It.Is<object[]>(args => (string)args[0] == "transactionFilters")), 
            Times.Once);
    }

    [Test]
    public async Task LoadFiltersAsync_ReturnsNull_WhenEmptyStringReturned()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _service.LoadFiltersAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ClearFiltersAsync_RemovesFiltersFromLocalStorage()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.removeItem",
                It.IsAny<object[]>()))
            .Returns(ValueTask.FromResult<object>(null!));

        // Act
        await _service.ClearFiltersAsync();

        // Assert
        _mockJsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.removeItem",
            It.Is<object[]>(args => 
                args.Length == 1 && 
                (string)args[0] == "transactionFilters")), 
            Times.Once);
    }

    [Test]
    public async Task SaveFiltersAsync_HandlesJSDisconnectedException_WithoutThrowing()
    {
        // Arrange
        var filters = new FilterState();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.setItem",
                It.IsAny<object[]>()))
            .Throws(new JSDisconnectedException("Client disconnected"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.SaveFiltersAsync(filters));
        
        // Verify warning logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected while saving filter state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SaveFiltersAsync_HandlesInvalidOperationException_WithoutThrowing()
    {
        // Arrange
        var filters = new FilterState();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.setItem",
                It.IsAny<object[]>()))
            .Throws(new InvalidOperationException("JSInterop call invalid"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.SaveFiltersAsync(filters));
        
        // Verify warning logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSInterop call invalid")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SaveFiltersAsync_HandlesGenericException_WithoutThrowing()
    {
        // Arrange
        var filters = new FilterState();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.setItem",
                It.IsAny<object[]>()))
            .Throws(new Exception("Unexpected error"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.SaveFiltersAsync(filters));
        
        // Verify error logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to save filter state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task LoadFiltersAsync_HandlesJSDisconnectedException_ReturnsNull()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .Throws(new JSDisconnectedException("Client disconnected"));

        // Act
        var result = await _service.LoadFiltersAsync();

        // Assert
        Assert.That(result, Is.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected while loading filter state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task LoadFiltersAsync_HandlesInvalidJson_ReturnsNull()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.IsAny<object[]>()))
            .ReturnsAsync("{ invalid json }");

        // Act
        var result = await _service.LoadFiltersAsync();

        // Assert
        Assert.That(result, Is.Null);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load filter state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ClearFiltersAsync_HandlesJSDisconnectedException_WithoutThrowing()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.removeItem",
                It.IsAny<object[]>()))
            .Throws(new JSDisconnectedException("Client disconnected"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.ClearFiltersAsync());
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected while clearing filter state")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SaveFiltersAsync_WithComplexData_SerializesCorrectly()
    {
        // Arrange - Test with max data to ensure JSON serialization handles all edge cases
        var filters = new FilterState
        {
            AssignmentStatus = "all",
            SuggestionStatus = "needsReview",
            DateRange = "custom",
            CustomStartDate = DateTime.MinValue,
            CustomEndDate = DateTime.MaxValue,
            SelectedCategories = Enumerable.Range(1, 50).Select(_ => Guid.NewGuid()).ToList(),
            MinAmount = decimal.MinValue,
            MaxAmount = decimal.MaxValue,
            SelectedAccountId = Guid.Empty
        };

        string? capturedJson = null;
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.setItem",
                It.IsAny<object[]>()))
            .Callback<string, object[]>((_, args) => capturedJson = args[1] as string)
            .Returns(ValueTask.FromResult<object>(null!));

        // Act
        await _service.SaveFiltersAsync(filters);

        // Assert
        Assert.That(capturedJson, Is.Not.Null);
        var deserializedFilters = JsonSerializer.Deserialize<FilterState>(capturedJson!);
        Assert.Multiple(() =>
        {
            Assert.That(deserializedFilters, Is.Not.Null);
            Assert.That(deserializedFilters!.SelectedCategories, Has.Count.EqualTo(50));
            Assert.That(deserializedFilters.CustomStartDate, Is.EqualTo(DateTime.MinValue));
            Assert.That(deserializedFilters.CustomEndDate, Is.EqualTo(DateTime.MaxValue));
            Assert.That(deserializedFilters.MinAmount, Is.EqualTo(decimal.MinValue));
            Assert.That(deserializedFilters.MaxAmount, Is.EqualTo(decimal.MaxValue));
        });
    }
}
