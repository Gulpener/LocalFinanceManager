using LocalFinanceManager.Models;

namespace LocalFinanceManager.Tests.Fixtures;

/// <summary>
/// Factory for creating test data entities.
/// Provides shared seed data for consistent testing.
/// </summary>
public class TestDataBuilder
{
    /// <summary>
    /// Creates a sample BaseEntity-derived object for testing.
    /// Will be extended with specific entity types in MVP-1 and beyond.
    /// </summary>
    public static MLModel CreateMLModel(string modelType = "TestModel")
    {
        return new MLModel
        {
            Id = Guid.NewGuid(),
            ModelType = modelType,
            Version = 1,
            TrainedAt = DateTime.UtcNow,
            ModelBytes = new byte[] { 0x01, 0x02, 0x03 },
            Metrics = "{\"accuracy\": 0.95}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsArchived = false,
            RowVersion = null // Let EF Core handle RowVersion
        };
    }

    /// <summary>
    /// Creates multiple test ML models.
    /// </summary>
    public static List<MLModel> CreateMLModels(int count = 3)
    {
        var models = new List<MLModel>();
        for (int i = 0; i < count; i++)
        {
            models.Add(CreateMLModel($"TestModel{i + 1}"));
        }
        return models;
    }
}
