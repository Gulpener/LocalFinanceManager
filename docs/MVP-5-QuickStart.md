# MVP 5: ML-Based Category Suggestions - Quick Start Guide

## Overview

MVP 5 implements machine learning-based transaction categorization using ML.NET. The system learns from user assignments and provides category suggestions with confidence scores and explanations.

## Architecture

- **LocalFinanceManager.ML** - ML models, feature extraction, interfaces
- **LocalFinanceManager/Services/MLService** - Model training and inference implementation
- **LocalFinanceManager/Controllers** - API endpoints (SuggestionsController, MLController)
- **LabeledExample** - Entity for storing training data from user corrections

## Configuration

Add to `appsettings.json`:

```json
{
  "MLOptions": {
    "MinLabeledExamplesPerCategory": 10,
    "TrainingWindowDays": 90,
    "RetrainingScheduleCron": "0 2 * * 0",
    "TopFeaturesCount": 3,
    "MinF1ScoreForApproval": 0.85,
    "NumberOfTrees": 100,
    "NumberOfLeaves": 20,
    "MinimumExampleCountPerLeaf": 10,
    "LearningRate": 0.2
  }
}
```

## API Endpoints

### Get Category Suggestion

```http
GET /api/suggestions?transactionId={guid}

Response:
{
  "categoryId": "...",
  "categoryName": "Groceries",
  "confidence": 0.92,
  "modelVersion": 1,
  "explanation": [
    {
      "featureName": "description_tokens",
      "featureValue": "grocery, store, market",
      "importance": 0.9
    },
    {
      "featureName": "counterparty",
      "featureValue": "albert heijn",
      "importance": 0.8
    },
    {
      "featureName": "amount_bin",
      "featureValue": "Small",
      "importance": 0.6
    }
  ]
}
```

### Record User Feedback

```http
POST /api/suggestions/feedback

Body:
{
  "transactionId": "...",
  "accepted": true,
  "finalCategoryId": "...",
  "suggestedCategoryId": "...",
  "suggestionConfidence": 0.92,
  "modelVersion": 1
}
```

### Trigger Model Retraining

```http
POST /api/ml/retrain?windowDays=90

Response:
{
  "model": {
    "version": 2,
    "trainedAt": "2026-01-15T14:00:00Z",
    "precision": 0.89,
    "recall": 0.89,
    "f1Score": 0.89,
    "sampleSize": 150,
    "categoryCount": 8,
    "isActive": true
  },
  "message": "Model trained successfully and activated"
}
```

### Get Training Statistics

```http
GET /api/ml/stats

Response:
{
  "trainingWindowDays": 90,
  "totalLabeledExamples": 150,
  "categoriesWithData": 8,
  "categoriesBelowThreshold": 2,
  "minExamplesThreshold": 10,
  "acceptanceRate": 0.78,
  "acceptedSuggestions": 95,
  "totalSuggestions": 122,
  "countsPerCategory": {
    "...": 25,
    "...": 18
  }
}
```

## Feature Extraction

The system extracts the following features from transactions:

- **Description Tokens:** Tokenized, lowercased, stop words removed
- **Counterparty:** Normalized merchant/entity name
- **Amount Bin:** Micro (<10), Small (10-100), Medium (100-1k), Large (1k-10k), XLarge (>10k)
- **Temporal:** Day of week, month, quarter
- **Transaction Type:** Income vs Expense

## Workflow

1. **Manual Assignment Phase:**
   - User assigns categories to transactions manually
   - Each assignment creates a LabeledExample
   - System tracks: TransactionId, CategoryId, timestamp

2. **Training Phase:**
   - When sufficient data exists (10+ examples per category)
   - Call `POST /api/ml/retrain` to train a new model
   - Model uses FastTreeBinaryClassificationTrainer with one-vs-rest
   - Only activates if F1 score > 0.85 threshold

3. **Suggestion Phase:**
   - For new transactions, call `GET /api/suggestions?transactionId={id}`
   - System returns category prediction with confidence and explanation
   - User can accept or override the suggestion
   - Feedback is recorded via `POST /api/suggestions/feedback`

4. **Continuous Improvement:**
   - All user corrections become training data
   - Periodic retraining improves model accuracy
   - 90-day rolling window keeps model relevant

## Database Schema

### LabeledExample

```csharp
public class LabeledExample : BaseEntity
{
    public Guid TransactionId { get; set; }
    public Guid CategoryId { get; set; }
    public string? UserId { get; set; }
    public bool WasAutoApplied { get; set; }
    public bool? AcceptedSuggestion { get; set; }
    public float? SuggestionConfidence { get; set; }
    public int? ModelVersion { get; set; }
}
```

### MLModel

```csharp
public class MLModel : BaseEntity
{
    public byte[] ModelBytes { get; set; }
    public int Version { get; set; }
    public DateTime TrainedAt { get; set; }
    public string Metrics { get; set; } // JSON
    public string ModelType { get; set; }
}
```

## Testing

Run feature extraction tests:

```bash
dotnet test --filter "FullyQualifiedName~FeatureExtractorTests"
```

22 tests cover:
- Tokenization and stop word removal
- Amount binning for all ranges
- Income/expense identification
- Temporal pattern extraction
- Counterparty normalization
- ML input format conversion

## Limitations & Future Work

**Current Limitations:**
- No UI components for suggestion display
- Manual retraining trigger (no scheduled background job yet)
- Basic feature importance heuristics
- Single-user mode (no multi-user support)

**Future Enhancements (MVP 6):**
- Auto-apply suggestions at high confidence (>0.95)
- Background retraining service (IHostedService)
- More sophisticated feature importance calculation
- UI components for suggestion workflow
- Metrics dashboard page

## Performance

- **Training Time:** ~1-2 seconds for 100-500 examples
- **Inference Time:** <50ms per transaction
- **Storage:** ~10-50KB per model (serialized)
- **Memory:** Model cached in-memory after first load

## Troubleshooting

**No suggestions available:**
- Check if any model exists: `GET /api/suggestions/active-model`
- Verify labeled examples: `GET /api/ml/stats`
- Ensure minimum threshold met (10 examples per category)

**Low confidence scores:**
- More training data needed
- Check if transaction features match training data patterns
- Review feature importance explanations

**Training fails:**
- Check logs for detailed error messages
- Verify sufficient labeled examples
- Ensure database migrations applied

## Security Considerations

- API endpoints return RFC 7231 compliant error responses
- No sensitive data in model explanations
- Model bytes stored securely in database
- Audit trail for all user corrections

## Next Steps

To enable ML suggestions in your app:

1. Manually assign categories to 10+ transactions per category
2. Call `POST /api/ml/retrain` to train first model
3. Verify training success: `GET /api/ml/stats`
4. Get suggestions for new transactions: `GET /api/suggestions?transactionId={id}`
5. Collect user feedback for continuous improvement

For auto-apply functionality, see MVP 6 documentation (future work).
