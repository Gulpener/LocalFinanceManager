using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Interfaces;

/// <summary>
/// Result of applying rules to a transaction.
/// </summary>
public class RuleMatchResult
{
    /// <summary>
    /// Whether a rule matched.
    /// </summary>
    public bool HasMatch { get; set; }

    /// <summary>
    /// The matching rule, if any.
    /// </summary>
    public Rule? MatchedRule { get; set; }

    /// <summary>
    /// Suggested category ID from the rule.
    /// </summary>
    public int? SuggestedCategoryId { get; set; }

    /// <summary>
    /// Suggested envelope ID from the rule.
    /// </summary>
    public int? SuggestedEnvelopeId { get; set; }

    /// <summary>
    /// Labels to add from the rule.
    /// </summary>
    public List<string> Labels { get; set; } = new();
}

/// <summary>
/// Repository interface for Rule entity operations.
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// Gets all rules ordered by priority.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of rules ordered by priority descending.</returns>
    Task<IReadOnlyList<Rule>> GetAllOrderedByPriorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a rule by ID.
    /// </summary>
    /// <param name="id">The rule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rule if found; otherwise null.</returns>
    Task<Rule?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new rule.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added rule with generated ID.</returns>
    Task<Rule> AddAsync(Rule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing rule.
    /// </summary>
    /// <param name="rule">The rule to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a rule by ID.
    /// </summary>
    /// <param name="id">The rule ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for the rule engine.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Applies rules to a transaction and returns the result.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rule match result.</returns>
    Task<RuleMatchResult> ApplyRulesAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions that would be affected by a rule.
    /// </summary>
    /// <param name="rule">The rule to preview.</param>
    /// <param name="accountId">Optional account ID to filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of transactions that match the rule.</returns>
    Task<IReadOnlyList<Transaction>> PreviewRuleAsync(Rule rule, int? accountId = null, CancellationToken cancellationToken = default);
}
