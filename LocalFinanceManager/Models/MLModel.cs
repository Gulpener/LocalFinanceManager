using LocalFinanceManager.Models;

namespace LocalFinanceManager.Models;

/// <summary>
/// Entity for storing trained ML.NET models in the database.
/// Enables versioning and persistence without filesystem dependencies.
/// </summary>
public class MLModel : BaseEntity
{
    /// <summary>
    /// Serialized model bytes (.bin format from ML.NET).
    /// </summary>
    public byte[] ModelBytes { get; set; } = null!;

    /// <summary>
    /// Model version number for tracking iterations.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Timestamp when the model was trained.
    /// </summary>
    public DateTime TrainedAt { get; set; }

    /// <summary>
    /// JSON-serialized metrics from model training (accuracy, precision, recall, etc.).
    /// </summary>
    public string Metrics { get; set; } = string.Empty;

    /// <summary>
    /// Model type identifier (e.g., "CategoryClassifier").
    /// </summary>
    public string ModelType { get; set; } = string.Empty;
}
