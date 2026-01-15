namespace LocalFinanceManager.ML;

/// <summary>
/// Simple DTO for transaction data needed for feature extraction.
/// Decouples ML project from main project's Transaction model.
/// </summary>
public class TransactionData
{
    public string Description { get; set; } = string.Empty;
    public string? Counterparty { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Guid AccountId { get; set; }
}

/// <summary>
/// Service for extracting ML features from transactions.
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>
    /// Extracts features from transaction data for ML model training or inference.
    /// </summary>
    TransactionFeatures ExtractFeatures(TransactionData transactionData);

    /// <summary>
    /// Converts extracted features to ML.NET input format.
    /// </summary>
    CategoryPredictionInput ToMLInput(TransactionFeatures features, Guid? categoryId = null);
}

/// <summary>
/// Implements feature extraction from transactions for ML model training and inference.
/// </summary>
public class FeatureExtractor : IFeatureExtractor
{
    /// <summary>
    /// Common stop words to exclude from tokenization (optional, can expand).
    /// </summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by"
    };

    /// <summary>
    /// Extracts features from transaction data.
    /// </summary>
    public TransactionFeatures ExtractFeatures(TransactionData transactionData)
    {
        var features = new TransactionFeatures
        {
            DescriptionTokens = TokenizeDescription(transactionData.Description),
            Counterparty = NormalizeText(transactionData.Counterparty),
            AmountBin = BinAmount(Math.Abs(transactionData.Amount)),
            DayOfWeek = (int)transactionData.Date.DayOfWeek,
            Month = transactionData.Date.Month,
            Quarter = (transactionData.Date.Month - 1) / 3 + 1,
            AccountId = transactionData.AccountId,
            AbsoluteAmount = Math.Abs(transactionData.Amount),
            IsIncome = transactionData.Amount > 0
        };

        return features;
    }

    /// <summary>
    /// Converts extracted features to ML.NET input format.
    /// </summary>
    public CategoryPredictionInput ToMLInput(TransactionFeatures features, Guid? categoryId = null)
    {
        return new CategoryPredictionInput
        {
            DescriptionText = string.Join(" ", features.DescriptionTokens),
            Counterparty = features.Counterparty ?? string.Empty,
            AmountBin = (float)features.AmountBin,
            DayOfWeek = features.DayOfWeek,
            Month = features.Month,
            Quarter = features.Quarter,
            AbsoluteAmount = (float)features.AbsoluteAmount,
            IsIncome = features.IsIncome ? 1.0f : 0.0f,
            CategoryId = categoryId?.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Tokenizes transaction description into normalized words.
    /// Removes punctuation, converts to lowercase, filters stop words.
    /// </summary>
    private string[] TokenizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Array.Empty<string>();

        // Remove punctuation and split on whitespace
        var words = description
            .ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', ';', ':', '-', '_', '/', '\\', '(', ')', '[', ']', '{', '}' }, 
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 1 && !StopWords.Contains(w))
            .Distinct()
            .ToArray();

        return words;
    }

    /// <summary>
    /// Normalizes text (lowercase, trim).
    /// </summary>
    private string? NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return text.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Bins amount into predefined categories.
    /// </summary>
    private AmountBin BinAmount(decimal absoluteAmount)
    {
        return absoluteAmount switch
        {
            < 10m => AmountBin.Micro,
            < 100m => AmountBin.Small,
            < 1000m => AmountBin.Medium,
            < 10000m => AmountBin.Large,
            _ => AmountBin.XLarge
        };
    }
}
