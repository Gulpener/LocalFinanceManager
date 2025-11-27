using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Repository interface for Envelope entity operations.
/// </summary>
public interface IEnvelopeRepository
{
    /// <summary>
    /// Gets an envelope by its ID.
    /// </summary>
    /// <param name="id">The envelope ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The envelope if found; otherwise null.</returns>
    Task<Envelope?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all envelopes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all envelopes.</returns>
    Task<IReadOnlyList<Envelope>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new envelope.
    /// </summary>
    /// <param name="envelope">The envelope to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added envelope with generated ID.</returns>
    Task<Envelope> AddAsync(Envelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing envelope.
    /// </summary>
    /// <param name="envelope">The envelope to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Envelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an envelope by ID.
    /// </summary>
    /// <param name="id">The envelope ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
