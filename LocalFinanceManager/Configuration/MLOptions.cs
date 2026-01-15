namespace LocalFinanceManager.Configuration;

/// <summary>
/// Configuration options for ML.NET model training and inference.
/// </summary>
public class MLOptions
{
    /// <summary>
    /// Minimum number of labeled examples required per category before auto-assignment is considered.
    /// Default: 10 examples per category.
    /// </summary>
    public int MinLabeledExamplesPerCategory { get; set; } = 10;

    /// <summary>
    /// Rolling window in days for training data inclusion.
    /// Default: 90 days.
    /// </summary>
    public int TrainingWindowDays { get; set; } = 90;

    /// <summary>
    /// Number of top features to include in explanation output.
    /// Default: 3 features.
    /// </summary>
    public int TopFeaturesCount { get; set; } = 3;

    /// <summary>
    /// Minimum F1 score threshold for model approval and activation.
    /// Models below this threshold are rejected and logged.
    /// Default: 0.85 (85% F1 score).
    /// </summary>
    public decimal MinF1ScoreForApproval { get; set; } = 0.85m;

    /// <summary>
    /// Number of trees to use in FastTreeBinaryClassificationTrainer.
    /// Default: 100 trees.
    /// </summary>
    public int NumberOfTrees { get; set; } = 100;

    /// <summary>
    /// Number of leaves per tree in FastTreeBinaryClassificationTrainer.
    /// Default: 20 leaves.
    /// </summary>
    public int NumberOfLeaves { get; set; } = 20;

    /// <summary>
    /// Minimum number of examples required per tree leaf.
    /// Default: 10 examples.
    /// </summary>
    public int MinimumExampleCountPerLeaf { get; set; } = 10;

    /// <summary>
    /// Learning rate for gradient boosting.
    /// Default: 0.2
    /// </summary>
    public double LearningRate { get; set; } = 0.2;
}
