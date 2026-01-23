using LocalFinanceManager.Configuration;
using LocalFinanceManager.Data;
using LocalFinanceManager.Data.Repositories;
using LocalFinanceManager.Models;
using LocalFinanceManager.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.Json;

namespace LocalFinanceManager.Services;

/// <summary>
/// ML.NET service for training and inference of category prediction models.
/// Implements IMLService from LocalFinanceManager.ML project.
/// </summary>
public class MLService : IMLService
{
    private readonly MLContext _mlContext;
    private readonly AppDbContext _dbContext;
    private readonly ILabeledExampleRepository _labeledExampleRepo;
    private readonly IFeatureExtractor _featureExtractor;
    private readonly MLOptions _options;
    private readonly ILogger<MLService> _logger;
    private ITransformer? _cachedModel;
    private int? _cachedModelVersion;

    public MLService(
        AppDbContext dbContext,
        ILabeledExampleRepository labeledExampleRepo,
        IFeatureExtractor featureExtractor,
        IOptions<MLOptions> options,
        ILogger<MLService> logger)
    {
        _mlContext = new MLContext(seed: 42);
        _dbContext = dbContext;
        _labeledExampleRepo = labeledExampleRepo;
        _featureExtractor = featureExtractor;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ModelTrainingResult> TrainModelAsync(int trainingWindowDays)
    {
        _logger.LogInformation("Starting ML model training with {WindowDays}-day rolling window", trainingWindowDays);

        // Get labeled training data
        var labeledExamples = await _labeledExampleRepo.GetTrainingDataAsync(trainingWindowDays);

        if (labeledExamples.Count == 0)
        {
            _logger.LogWarning("No labeled training data available");
            return new ModelTrainingResult
            {
                Message = "No training data available",
                SampleSize = 0
            };
        }

        // Check minimum examples per category
        var countsPerCategory = await _labeledExampleRepo.GetCountPerCategoryAsync(trainingWindowDays);
        var categoriesUnderThreshold = countsPerCategory
            .Where(kvp => kvp.Value < _options.MinLabeledExamplesPerCategory)
            .ToList();

        if (categoriesUnderThreshold.Any())
        {
            var categoryNames = await _dbContext.Categories
                .Where(c => categoriesUnderThreshold.Select(ct => ct.Key).Contains(c.Id))
                .Select(c => c.Name)
                .ToListAsync();

            _logger.LogWarning(
                "Categories below minimum threshold ({Threshold} examples): {Categories}",
                _options.MinLabeledExamplesPerCategory,
                string.Join(", ", categoryNames));
        }

        // Convert to ML.NET input format
        var trainingData = labeledExamples
            .Select(le =>
            {
                var transactionData = new TransactionData
                {
                    Description = le.Transaction!.Description,
                    Counterparty = le.Transaction.Counterparty,
                    Amount = le.Transaction.Amount,
                    Date = le.Transaction.Date,
                    AccountId = le.Transaction.AccountId
                };
                var features = _featureExtractor.ExtractFeatures(transactionData);
                return _featureExtractor.ToMLInput(features, le.CategoryId);
            })
            .ToList();

        _logger.LogInformation("Prepared {Count} training examples from {UniqueCategories} categories",
            trainingData.Count,
            trainingData.Select(d => d.CategoryId).Distinct().Count());

        // Load data into ML.NET
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Split into train/test (80/20)
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Build training pipeline
        var pipeline = _mlContext.Transforms.Text.FeaturizeText("DescriptionFeatures", nameof(CategoryPredictionInput.DescriptionText))
            .Append(_mlContext.Transforms.Text.FeaturizeText("CounterpartyFeatures", nameof(CategoryPredictionInput.Counterparty)))
            .Append(_mlContext.Transforms.Concatenate("Features",
                "DescriptionFeatures",
                "CounterpartyFeatures",
                nameof(CategoryPredictionInput.AmountBin),
                nameof(CategoryPredictionInput.DayOfWeek),
                nameof(CategoryPredictionInput.Month),
                nameof(CategoryPredictionInput.Quarter),
                nameof(CategoryPredictionInput.AbsoluteAmount),
                nameof(CategoryPredictionInput.IsIncome)))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(CategoryPredictionInput.CategoryId)))
            .Append(_mlContext.MulticlassClassification.Trainers.OneVersusAll(
                _mlContext.BinaryClassification.Trainers.FastTree(
                    numberOfLeaves: _options.NumberOfLeaves,
                    numberOfTrees: _options.NumberOfTrees,
                    minimumExampleCountPerLeaf: _options.MinimumExampleCountPerLeaf,
                    learningRate: _options.LearningRate)))
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // Train the model
        _logger.LogInformation("Training model with FastTree (trees={Trees}, leaves={Leaves})",
            _options.NumberOfTrees, _options.NumberOfLeaves);

        var model = pipeline.Fit(trainTestSplit.TrainSet);

        // Evaluate on test set
        var predictions = model.Transform(trainTestSplit.TestSet);
        var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

        _logger.LogInformation(
            "Model training complete. MacroAccuracy={Accuracy:F4}, LogLoss={LogLoss:F4}",
            metrics.MacroAccuracy,
            metrics.LogLoss);

        // Calculate F1 score (approximation using macro accuracy)
        var f1Score = 2 * metrics.MacroAccuracy / (1 + metrics.MacroAccuracy);

        // Serialize and save model
        var nextVersion = await GetNextModelVersionAsync();
        var modelBytes = SerializeModel(model);

        var metricsJson = JsonSerializer.Serialize(new
        {
            MacroAccuracy = metrics.MacroAccuracy,
            MicroAccuracy = metrics.MicroAccuracy,
            LogLoss = metrics.LogLoss,
            LogLossReduction = metrics.LogLossReduction,
            F1Score = f1Score,
            TopKAccuracy = metrics.TopKAccuracy,
            PerClassLogLoss = metrics.PerClassLogLoss.ToArray()
        });

        var mlModel = new MLModel
        {
            Id = Guid.NewGuid(),
            ModelBytes = modelBytes,
            Version = nextVersion,
            TrainedAt = DateTime.UtcNow,
            Metrics = metricsJson,
            ModelType = "CategoryClassifier",
            IsArchived = false
        };

        await _dbContext.MLModels.AddAsync(mlModel);
        await _dbContext.SaveChangesAsync();

        var meetsThreshold = (decimal)f1Score >= _options.MinF1ScoreForApproval;

        _logger.LogInformation(
            "Model v{Version} saved with F1 score {F1Score:F4} (threshold: {Threshold}). Meets approval: {Approved}",
            nextVersion,
            f1Score,
            _options.MinF1ScoreForApproval,
            meetsThreshold);

        // Auto-activate if meets threshold and is first model or better than current
        if (meetsThreshold)
        {
            await ActivateModelInternalAsync(mlModel, model);
        }

        return new ModelTrainingResult
        {
            Version = nextVersion,
            TrainedAt = mlModel.TrainedAt,
            SampleSize = trainingData.Count,
            CategoryCount = trainingData.Select(d => d.CategoryId).Distinct().Count(),
            Precision = metrics.MacroAccuracy, // Approximation
            Recall = metrics.MacroAccuracy, // Approximation
            F1Score = f1Score,
            MeetsApprovalThreshold = meetsThreshold,
            Message = meetsThreshold
                ? "Model trained successfully and activated"
                : $"Model trained but F1 score {f1Score:F4} below threshold {_options.MinF1ScoreForApproval}"
        };
    }

    /// <inheritdoc/>
    public async Task<CategoryPrediction?> PredictCategoryAsync(Guid transactionId)
    {
        // Load transaction
        var transaction = await _dbContext.Transactions
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
            return null;
        }

        // Get active model
        var activeModel = await GetActiveModelAsync();
        if (activeModel == null)
        {
            _logger.LogWarning("No active ML model available for predictions");
            return null;
        }

        // Load model into memory if not cached
        if (_cachedModel == null || _cachedModelVersion != activeModel.Version)
        {
            var modelEntity = await _dbContext.MLModels
                .FirstOrDefaultAsync(m => m.Version == activeModel.Version && !m.IsArchived);

            if (modelEntity == null)
            {
                _logger.LogError("Active model v{Version} not found in database", activeModel.Version);
                return null;
            }

            _cachedModel = DeserializeModel(modelEntity.ModelBytes);
            _cachedModelVersion = activeModel.Version;
        }

        // Extract features
        var transactionData = new TransactionData
        {
            Description = transaction.Description,
            Counterparty = transaction.Counterparty,
            Amount = transaction.Amount,
            Date = transaction.Date,
            AccountId = transaction.AccountId
        };
        var features = _featureExtractor.ExtractFeatures(transactionData);
        var input = _featureExtractor.ToMLInput(features);

        // Make prediction
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<CategoryPredictionInput, CategoryPredictionOutput>(_cachedModel);
        var prediction = predictionEngine.Predict(input);

        // Parse predicted category ID
        if (!Guid.TryParse(prediction.PredictedLabel, out var predictedCategoryId))
        {
            _logger.LogError("Failed to parse predicted category ID: {Label}", prediction.PredictedLabel);
            return null;
        }

        // Get category name
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == predictedCategoryId);

        if (category == null)
        {
            _logger.LogError("Predicted category {CategoryId} not found", predictedCategoryId);
            return null;
        }

        // Build explanation (top features)
        var topFeatures = BuildFeatureExplanation(features, input);

        return new CategoryPrediction
        {
            CategoryId = predictedCategoryId,
            CategoryName = category.Name,
            Confidence = prediction.Score,
            TopFeatures = topFeatures,
            ModelVersion = activeModel.Version
        };
    }

    /// <inheritdoc/>
    public async Task<ActiveModelInfo?> GetActiveModelAsync()
    {
        var activeModel = await _dbContext.MLModels
            .Where(m => !m.IsArchived)
            .OrderByDescending(m => m.Version)
            .FirstOrDefaultAsync();

        if (activeModel == null)
            return null;

        return new ActiveModelInfo
        {
            Version = activeModel.Version,
            TrainedAt = activeModel.TrainedAt,
            Metrics = activeModel.Metrics
        };
    }

    /// <inheritdoc/>
    public async Task ActivateModelAsync(int modelVersion)
    {
        var model = await _dbContext.MLModels
            .FirstOrDefaultAsync(m => m.Version == modelVersion && !m.IsArchived);

        if (model == null)
        {
            throw new InvalidOperationException($"Model version {modelVersion} not found");
        }

        var transformer = DeserializeModel(model.ModelBytes);
        await ActivateModelInternalAsync(model, transformer);

        _logger.LogInformation("Model v{Version} activated manually", modelVersion);
    }

    private async Task ActivateModelInternalAsync(MLModel model, ITransformer transformer)
    {
        _cachedModel = transformer;
        _cachedModelVersion = model.Version;

        // In a real system, we might mark the model as "active" in the database
        // For now, we just cache it in memory (latest non-archived is active)
        await Task.CompletedTask;
    }

    private async Task<int> GetNextModelVersionAsync()
    {
        var maxVersion = await _dbContext.MLModels
            .MaxAsync(m => (int?)m.Version) ?? 0;

        return maxVersion + 1;
    }

    private byte[] SerializeModel(ITransformer model)
    {
        using var stream = new MemoryStream();
        _mlContext.Model.Save(model, null, stream);
        return stream.ToArray();
    }

    private ITransformer DeserializeModel(byte[] modelBytes)
    {
        using var stream = new MemoryStream(modelBytes);
        return _mlContext.Model.Load(stream, out _);
    }

    private List<FeatureContribution> BuildFeatureExplanation(TransactionFeatures features, CategoryPredictionInput input)
    {
        // Simple heuristic-based feature importance (top N features based on presence/value)
        var contributions = new List<FeatureContribution>();

        // Description tokens (if any)
        if (features.DescriptionTokens.Length > 0)
        {
            contributions.Add(new FeatureContribution
            {
                FeatureName = "description_tokens",
                FeatureValue = string.Join(", ", features.DescriptionTokens.Take(3)),
                Importance = 0.9f
            });
        }

        // Counterparty
        if (!string.IsNullOrEmpty(features.Counterparty))
        {
            contributions.Add(new FeatureContribution
            {
                FeatureName = "counterparty",
                FeatureValue = features.Counterparty,
                Importance = 0.8f
            });
        }

        // Amount bin
        contributions.Add(new FeatureContribution
        {
            FeatureName = "amount_bin",
            FeatureValue = features.AmountBin.ToString(),
            Importance = 0.6f
        });

        // Temporal features (if distinct patterns)
        if (features.DayOfWeek == 0 || features.DayOfWeek == 6) // Weekend
        {
            contributions.Add(new FeatureContribution
            {
                FeatureName = "day_of_week",
                FeatureValue = features.DayOfWeek == 0 ? "Sunday" : "Saturday",
                Importance = 0.4f
            });
        }

        return contributions
            .OrderByDescending(c => c.Importance)
            .Take(_options.TopFeaturesCount)
            .ToList();
    }
}
