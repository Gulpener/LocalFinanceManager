using System.Text.RegularExpressions;
using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;

namespace LocalFinanceManager.Application.Services;

/// <summary>
/// Rule engine for automatic transaction categorization based on user-defined rules.
/// </summary>
public class RuleEngine : IRuleEngine
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ITransactionRepository _transactionRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleEngine"/> class.
    /// </summary>
    /// <param name="ruleRepository">The rule repository.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    public RuleEngine(
        IRuleRepository ruleRepository,
        ITransactionRepository transactionRepository)
    {
        _ruleRepository = ruleRepository;
        _transactionRepository = transactionRepository;
    }

    /// <inheritdoc />
    public async Task<RuleMatchResult> ApplyRulesAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var rules = await _ruleRepository.GetAllOrderedByPriorityAsync(cancellationToken);

        var matchedRule = rules.FirstOrDefault(rule => RuleMatches(rule, transaction));
        if (matchedRule != null)
        {
            return new RuleMatchResult
            {
                HasMatch = true,
                MatchedRule = matchedRule,
                SuggestedCategoryId = matchedRule.TargetCategoryId,
                SuggestedEnvelopeId = matchedRule.TargetEnvelopeId,
                Labels = matchedRule.AddLabels.ToList()
            };
        }

        return new RuleMatchResult { HasMatch = false };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> PreviewRuleAsync(Rule rule, int? accountId = null, CancellationToken cancellationToken = default)
    {
        var transactions = accountId.HasValue
            ? await _transactionRepository.GetByAccountIdAsync(accountId.Value, cancellationToken)
            : await _transactionRepository.GetByDateRangeAsync(DateTime.MinValue, DateTime.MaxValue, cancellationToken);

        return transactions.Where(t => RuleMatches(rule, t)).ToList();
    }

    private static bool RuleMatches(Rule rule, Transaction transaction)
    {
        return rule.MatchType.ToLowerInvariant() switch
        {
            "contains" => MatchesContains(rule.Pattern, transaction),
            "regex" => MatchesRegex(rule.Pattern, transaction),
            "iban" => MatchesIban(rule.Pattern, transaction),
            "exact" => MatchesExact(rule.Pattern, transaction),
            _ => false
        };
    }

    private static bool MatchesContains(string pattern, Transaction transaction)
    {
        var lowerPattern = pattern.ToLowerInvariant();
        var description = transaction.Description?.ToLowerInvariant() ?? string.Empty;

        return description.Contains(lowerPattern, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesRegex(string pattern, Transaction transaction)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
            return regex.IsMatch(transaction.Description ?? string.Empty);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool MatchesIban(string pattern, Transaction transaction)
    {
        var normalizedPattern = pattern.ToUpperInvariant().Replace(" ", "");
        var normalizedAccount = transaction.CounterAccount?.ToUpperInvariant().Replace(" ", "") ?? string.Empty;

        return normalizedAccount == normalizedPattern;
    }

    private static bool MatchesExact(string pattern, Transaction transaction)
    {
        return string.Equals(transaction.Description, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
