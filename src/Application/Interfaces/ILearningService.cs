using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Service interface for updating category learning profiles.
/// </summary>
public interface ILearningService
{
    /// <summary>
    /// Updates the learning profile when a transaction is categorized.
    /// </summary>
    /// <param name="transaction">The transaction that was categorized.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LearnFromTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the learning profile when a transaction's category is changed.
    /// </summary>
    /// <param name="transaction">The transaction with the new category.</param>
    /// <param name="oldCategoryId">The previous category ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LearnFromCorrectionAsync(Transaction transaction, int oldCategoryId, CancellationToken cancellationToken = default);
}
