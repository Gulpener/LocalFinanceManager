namespace LocalFinanceManager.DTOs.ML;

/// <summary>
/// DTO for ML category suggestion response.
/// </summary>
public class SuggestionDto
{
    /// <summary>
    /// Suggested category ID.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Category name for display.
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Top contributing features explaining the suggestion.
    /// </summary>
    public List<FeatureExplanationDto> Explanation { get; set; } = new();

    /// <summary>
    /// Model version that generated this suggestion.
    /// </summary>
    public int ModelVersion { get; set; }
}

/// <summary>
/// DTO for feature importance explanation.
/// </summary>
public class FeatureExplanationDto
{
    /// <summary>
    /// Feature name (e.g., "description_token", "counterparty", "amount_bin").
    /// </summary>
    public string FeatureName { get; set; } = string.Empty;

    /// <summary>
    /// Feature value that contributed to the prediction.
    /// </summary>
    public string FeatureValue { get; set; } = string.Empty;

    /// <summary>
    /// Importance score (relative contribution).
    /// </summary>
    public float Importance { get; set; }
}

/// <summary>
/// DTO for user feedback on ML suggestion.
/// </summary>
public class SuggestionFeedbackDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Whether the user accepted the suggestion (true) or overrode it (false).
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// Final category ID chosen by the user (may differ from suggested).
    /// </summary>
    public Guid FinalCategoryId { get; set; }

    /// <summary>
    /// Suggested category ID (for tracking rejection reasons).
    /// </summary>
    public Guid? SuggestedCategoryId { get; set; }

    /// <summary>
    /// Confidence score of the suggestion.
    /// </summary>
    public float? SuggestionConfidence { get; set; }

    /// <summary>
    /// Model version that generated the suggestion.
    /// </summary>
    public int? ModelVersion { get; set; }
}

/// <summary>
/// DTO for model metrics response.
/// </summary>
public class ModelMetricsDto
{
    /// <summary>
    /// Model version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Timestamp when the model was trained.
    /// </summary>
    public DateTime TrainedAt { get; set; }

    /// <summary>
    /// Overall precision (macro-average).
    /// </summary>
    public double Precision { get; set; }

    /// <summary>
    /// Overall recall (macro-average).
    /// </summary>
    public double Recall { get; set; }

    /// <summary>
    /// Overall F1 score (macro-average).
    /// </summary>
    public double F1Score { get; set; }

    /// <summary>
    /// Number of training samples used.
    /// </summary>
    public int SampleSize { get; set; }

    /// <summary>
    /// Number of categories in the model.
    /// </summary>
    public int CategoryCount { get; set; }

    /// <summary>
    /// Is this model currently active for predictions?
    /// </summary>
    public bool IsActive { get; set; }
}
