namespace LocalFinanceManager.ML;

/// <summary>
/// Amount binning categories for transaction amount features.
/// </summary>
public enum AmountBin
{
    /// <summary>
    /// Micro transactions: less than 10 currency units.
    /// </summary>
    Micro = 0,

    /// <summary>
    /// Small transactions: 10 to 100 currency units.
    /// </summary>
    Small = 1,

    /// <summary>
    /// Medium transactions: 100 to 1000 currency units.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Large transactions: 1000 to 10000 currency units.
    /// </summary>
    Large = 3,

    /// <summary>
    /// Extra-large transactions: over 10000 currency units.
    /// </summary>
    XLarge = 4
}

/// <summary>
/// Extracted features from a transaction for ML model training and inference.
/// </summary>
public class TransactionFeatures
{
    /// <summary>
    /// Tokenized description words (lowercase, normalized).
    /// </summary>
    public string[] DescriptionTokens { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Normalized counterparty name (lowercase, trimmed).
    /// </summary>
    public string? Counterparty { get; set; }

    /// <summary>
    /// Amount bin category (micro, small, medium, large, xlarge).
    /// </summary>
    public AmountBin AmountBin { get; set; }

    /// <summary>
    /// Day of the week (0=Sunday, 6=Saturday).
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// Month of the year (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Quarter of the year (1-4).
    /// </summary>
    public int Quarter { get; set; }

    /// <summary>
    /// Account ID for account-specific patterns.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Absolute amount value (for additional numeric features).
    /// </summary>
    public decimal AbsoluteAmount { get; set; }

    /// <summary>
    /// Is the amount positive (income) or negative (expense)?
    /// </summary>
    public bool IsIncome { get; set; }
}

/// <summary>
/// ML.NET input data for category prediction model.
/// </summary>
public class CategoryPredictionInput
{
    /// <summary>
    /// Combined text from description tokens (space-separated).
    /// </summary>
    public string DescriptionText { get; set; } = string.Empty;

    /// <summary>
    /// Counterparty name (normalized).
    /// </summary>
    public string Counterparty { get; set; } = string.Empty;

    /// <summary>
    /// Amount bin as float (0-4).
    /// </summary>
    public float AmountBin { get; set; }

    /// <summary>
    /// Day of week as float (0-6).
    /// </summary>
    public float DayOfWeek { get; set; }

    /// <summary>
    /// Month as float (1-12).
    /// </summary>
    public float Month { get; set; }

    /// <summary>
    /// Quarter as float (1-4).
    /// </summary>
    public float Quarter { get; set; }

    /// <summary>
    /// Absolute amount value.
    /// </summary>
    public float AbsoluteAmount { get; set; }

    /// <summary>
    /// Is income (1.0) or expense (0.0).
    /// </summary>
    public float IsIncome { get; set; }

    /// <summary>
    /// Target category ID for training (label).
    /// </summary>
    public string CategoryId { get; set; } = string.Empty;
}

/// <summary>
/// ML.NET output data from category prediction model.
/// </summary>
public class CategoryPredictionOutput
{
    /// <summary>
    /// Predicted category ID.
    /// </summary>
    public string PredictedLabel { get; set; } = string.Empty;

    /// <summary>
    /// Prediction score (confidence).
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Scores for all possible categories.
    /// </summary>
    public float[]? Scores { get; set; }
}
