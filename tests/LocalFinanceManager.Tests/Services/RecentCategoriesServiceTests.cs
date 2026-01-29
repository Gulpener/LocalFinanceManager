using LocalFinanceManager.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using System.Text.Json;

namespace LocalFinanceManager.Tests.Services;

[TestFixture]
public class RecentCategoriesServiceTests
{
    private Mock<IJSRuntime> _mockJsRuntime;
    private Mock<ILogger<RecentCategoriesService>> _mockLogger;
    private RecentCategoriesService _service;

    [SetUp]
    public void Setup()
    {
        _mockJsRuntime = new Mock<IJSRuntime>();
        _mockLogger = new Mock<ILogger<RecentCategoriesService>>();
        _service = new RecentCategoriesService(_mockJsRuntime.Object, _mockLogger.Object);
    }

    [Test]
    public async Task TrackCategoryUsageAsync_IncrementsUsageCount()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var initialUsageData = new Dictionary<Guid, int>();

        SetupGetUsageData(initialUsageData);
        Dictionary<Guid, int>? capturedUsageData = null;
        SetupSaveUsageData(data => capturedUsageData = data);

        // Act - Track the same category twice
        await _service.TrackCategoryUsageAsync(categoryId);

        // Update mock to return the saved data for second call
        SetupGetUsageData(capturedUsageData ?? new Dictionary<Guid, int>());
        await _service.TrackCategoryUsageAsync(categoryId);

        // Assert
        Assert.That(capturedUsageData, Is.Not.Null);
        Assert.That(capturedUsageData!.ContainsKey(categoryId), Is.True);
        Assert.That(capturedUsageData[categoryId], Is.EqualTo(2));
    }

    [Test]
    public async Task TrackCategoryUsageAsync_AddsNewCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        SetupGetUsageData(new Dictionary<Guid, int>());

        Dictionary<Guid, int>? capturedUsageData = null;
        SetupSaveUsageData(data => capturedUsageData = data);

        // Act
        await _service.TrackCategoryUsageAsync(categoryId);

        // Assert
        Assert.That(capturedUsageData, Is.Not.Null);
        Assert.That(capturedUsageData!.ContainsKey(categoryId), Is.True);
        Assert.That(capturedUsageData[categoryId], Is.EqualTo(1));
    }

    [Test]
    public async Task TrackCategoryUsageAsync_TrimsToTop20Categories()
    {
        // Arrange - Create 20 categories with varying usage counts (1-20)
        // The service adds a new category which would make 21 total
        var usageData = new Dictionary<Guid, int>();
        for (int i = 1; i <= 20; i++)
        {
            usageData[Guid.NewGuid()] = i; // Usage counts 1-20
        }

        var newCategoryId = Guid.NewGuid();
        SetupGetUsageData(usageData);

        Dictionary<Guid, int>? capturedUsageData = null;
        SetupSaveUsageData(data => capturedUsageData = data);

        // Act - Track a new category (will get usage count 1)
        await _service.TrackCategoryUsageAsync(newCategoryId);

        // Assert
        Assert.That(capturedUsageData, Is.Not.Null);
        Assert.That(capturedUsageData!.Count, Is.EqualTo(20), "Should trim to exactly 20 categories");

        // The lowest usage count should be 1 (could be from multiple categories)
        Assert.That(capturedUsageData.Values.Min(), Is.EqualTo(1));

        // Verify the top 20 categories by usage are kept
        Assert.That(capturedUsageData.Values.Max(), Is.EqualTo(20), "Highest usage category should be kept");
    }

    [Test]
    public async Task GetRecentCategoriesAsync_ReturnsTop5MostUsed()
    {
        // Arrange - Create 10 categories with known usage counts
        var category1 = Guid.NewGuid();
        var category2 = Guid.NewGuid();
        var category3 = Guid.NewGuid();
        var category4 = Guid.NewGuid();
        var category5 = Guid.NewGuid();
        var category6 = Guid.NewGuid();
        var category7 = Guid.NewGuid();
        var category8 = Guid.NewGuid();
        var category9 = Guid.NewGuid();
        var category10 = Guid.NewGuid();

        var usageData = new Dictionary<Guid, int>
        {
            { category1, 1 },
            { category2, 2 },
            { category3, 3 },
            { category4, 4 },
            { category5, 5 },
            { category6, 6 },
            { category7, 7 },
            { category8, 8 },
            { category9, 9 },
            { category10, 10 }
        };

        SetupGetUsageData(usageData);

        // Act
        var result = await _service.GetRecentCategoriesAsync(5);

        // Assert
        Assert.That(result, Has.Count.EqualTo(5));
        Assert.That(result[0], Is.EqualTo(category10), "First should be highest usage");
        Assert.That(result[1], Is.EqualTo(category9));
        Assert.That(result[2], Is.EqualTo(category8));
        Assert.That(result[3], Is.EqualTo(category7));
        Assert.That(result[4], Is.EqualTo(category6));
    }

    [Test]
    public async Task GetRecentCategoriesAsync_ReturnsEmptyList_WhenNoUsageData()
    {
        // Arrange
        SetupGetUsageData(new Dictionary<Guid, int>());

        // Act
        var result = await _service.GetRecentCategoriesAsync(5);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetRecentCategoriesAsync_ReturnsAllCategories_WhenFewerThanRequested()
    {
        // Arrange
        var category1 = Guid.NewGuid();
        var category2 = Guid.NewGuid();
        var usageData = new Dictionary<Guid, int>
        {
            { category1, 5 },
            { category2, 3 }
        };

        SetupGetUsageData(usageData);

        // Act
        var result = await _service.GetRecentCategoriesAsync(5);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(category1));
        Assert.That(result[1], Is.EqualTo(category2));
    }

    [Test]
    public async Task ToggleFavoriteAsync_AddsFavorite()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        SetupGetFavorites(new List<Guid>());

        List<Guid>? capturedFavorites = null;
        SetupSaveFavorites(favorites => capturedFavorites = favorites);

        // Act
        await _service.ToggleFavoriteAsync(categoryId);

        // Assert
        Assert.That(capturedFavorites, Is.Not.Null);
        Assert.That(capturedFavorites!, Contains.Item(categoryId));
        Assert.That(capturedFavorites.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ToggleFavoriteAsync_RemovesFavorite()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        SetupGetFavorites(new List<Guid> { categoryId, otherCategoryId });

        List<Guid>? capturedFavorites = null;
        SetupSaveFavorites(favorites => capturedFavorites = favorites);

        // Act
        await _service.ToggleFavoriteAsync(categoryId);

        // Assert
        Assert.That(capturedFavorites, Is.Not.Null);
        Assert.That(capturedFavorites!, Does.Not.Contain(categoryId));
        Assert.That(capturedFavorites, Contains.Item(otherCategoryId));
        Assert.That(capturedFavorites.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetFavoriteCategoriesAsync_ReturnsFavoritesList()
    {
        // Arrange
        var category1 = Guid.NewGuid();
        var category2 = Guid.NewGuid();
        var category3 = Guid.NewGuid();
        var favorites = new List<Guid> { category1, category2, category3 };

        SetupGetFavorites(favorites);

        // Act
        var result = await _service.GetFavoriteCategoriesAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Contains.Item(category1));
        Assert.That(result, Contains.Item(category2));
        Assert.That(result, Contains.Item(category3));
    }

    [Test]
    public async Task GetFavoriteCategoriesAsync_ReturnsEmptyList_WhenNoFavorites()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "favoriteCategories")))
            .ReturnsAsync((string)null!);

        // Act
        var result = await _service.GetFavoriteCategoriesAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetFavoriteCategoriesAsync_ReturnsEmptyList_WhenEmptyString()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "favoriteCategories")))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _service.GetFavoriteCategoriesAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task TrackCategoryUsageAsync_HandlesJSDisconnectedException_WithoutThrowing()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "recentCategories")))
            .Throws(new JSDisconnectedException("Client disconnected"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.TrackCategoryUsageAsync(categoryId));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected while getting usage data from localStorage")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task TrackCategoryUsageAsync_HandlesInvalidOperationException_WithoutThrowing()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "recentCategories")))
            .Throws(new InvalidOperationException("JSInterop call invalid"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.TrackCategoryUsageAsync(categoryId));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("JSInterop call invalid while getting usage data from localStorage")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task TrackCategoryUsageAsync_HandlesGenericException_WithoutThrowing()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "recentCategories")))
            .Throws(new Exception("Unexpected error"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.TrackCategoryUsageAsync(categoryId));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get usage data from localStorage")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetRecentCategoriesAsync_HandlesJSDisconnectedException_ReturnsEmptyList()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "recentCategories")))
            .Throws(new JSDisconnectedException("Client disconnected"));

        // Act
        var result = await _service.GetRecentCategoriesAsync(5);

        // Assert
        Assert.That(result, Is.Empty);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected while getting usage data from localStorage")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetRecentCategoriesAsync_HandlesInvalidJson_ReturnsEmptyList()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "recentCategories")))
            .ReturnsAsync("{ invalid json }");

        // Act
        var result = await _service.GetRecentCategoriesAsync(5);

        // Assert
        Assert.That(result, Is.Empty);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get usage data from localStorage")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ToggleFavoriteAsync_HandlesJSDisconnectedException_WithoutThrowing()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "favoriteCategories")))
            .Throws(new JSDisconnectedException("Client disconnected"));

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _service.ToggleFavoriteAsync(categoryId));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected while getting favorite categories")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetFavoriteCategoriesAsync_HandlesJSDisconnectedException_ReturnsEmptyList()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "favoriteCategories")))
            .Throws(new JSDisconnectedException("Client disconnected"));

        // Act
        var result = await _service.GetFavoriteCategoriesAsync();

        // Assert
        Assert.That(result, Is.Empty);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected while getting favorite categories")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetFavoriteCategoriesAsync_HandlesInvalidJson_ReturnsEmptyList()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "favoriteCategories")))
            .ReturnsAsync("not valid json");

        // Act
        var result = await _service.GetFavoriteCategoriesAsync();

        // Assert
        Assert.That(result, Is.Empty);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to get favorite categories")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task TrackCategoryUsageAsync_WithMultipleCategories_MaintainsCorrectCounts()
    {
        // Arrange
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var categoryC = Guid.NewGuid();

        var usageData = new Dictionary<Guid, int>();
        SetupGetUsageData(usageData);

        Dictionary<Guid, int>? capturedUsageData = null;
        SetupSaveUsageData(data =>
        {
            capturedUsageData = data;
            // Update the mock to return the new data for subsequent calls
            SetupGetUsageData(new Dictionary<Guid, int>(data));
        });

        // Act - Track different categories with different frequencies
        await _service.TrackCategoryUsageAsync(categoryA);
        await _service.TrackCategoryUsageAsync(categoryB);
        await _service.TrackCategoryUsageAsync(categoryA);
        await _service.TrackCategoryUsageAsync(categoryC);
        await _service.TrackCategoryUsageAsync(categoryA);

        // Assert
        Assert.That(capturedUsageData, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedUsageData![categoryA], Is.EqualTo(3));
            Assert.That(capturedUsageData[categoryB], Is.EqualTo(1));
            Assert.That(capturedUsageData[categoryC], Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ToggleFavoriteAsync_MultipleToggles_WorksCorrectly()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var favorites = new List<Guid>();

        SetupGetFavorites(favorites);
        List<Guid>? capturedFavorites = null;
        SetupSaveFavorites(f =>
        {
            capturedFavorites = f;
            SetupGetFavorites(new List<Guid>(f));
        });

        // Act & Assert - Toggle on
        await _service.ToggleFavoriteAsync(categoryId);
        Assert.That(capturedFavorites!, Contains.Item(categoryId));

        // Toggle off
        await _service.ToggleFavoriteAsync(categoryId);
        Assert.That(capturedFavorites!, Does.Not.Contain(categoryId));

        // Toggle on again
        await _service.ToggleFavoriteAsync(categoryId);
        Assert.That(capturedFavorites!, Contains.Item(categoryId));
    }

    // Helper methods to setup mocks
    private void SetupGetUsageData(Dictionary<Guid, int> usageData)
    {
        var json = JsonSerializer.Serialize(usageData);
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "recentCategories")))
            .ReturnsAsync(json);
    }

    private void SetupSaveUsageData(Action<Dictionary<Guid, int>> callback)
    {
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.setItem",
                It.Is<object[]>(args => (string)args[0] == "recentCategories")))
            .Callback<string, object[]>((_, args) =>
            {
                var json = args[1] as string;
                var data = JsonSerializer.Deserialize<Dictionary<Guid, int>>(json!);
                callback(data!);
            })
            .Returns(ValueTask.FromResult<object>(null!));
    }

    private void SetupGetFavorites(List<Guid> favorites)
    {
        var json = JsonSerializer.Serialize(favorites);
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => (string)args[0] == "favoriteCategories")))
            .ReturnsAsync(json);
    }

    private void SetupSaveFavorites(Action<List<Guid>> callback)
    {
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<object>(
                "localStorage.setItem",
                It.Is<object[]>(args => (string)args[0] == "favoriteCategories")))
            .Callback<string, object[]>((_, args) =>
            {
                var json = args[1] as string;
                var data = JsonSerializer.Deserialize<List<Guid>>(json!);
                callback(data!);
            })
            .Returns(ValueTask.FromResult<object>(null!));
    }
}
