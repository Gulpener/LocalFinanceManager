using LocalFinanceManager.Models;

namespace LocalFinanceManager.Services;

/// <summary>
/// Provides hard-coded category template definitions for budget plan initialization.
/// Templates are read-only definitions; once applied, categories become independent entities.
/// </summary>
public static class CategoryTemplates
{
    /// <summary>
    /// Dictionary of template names to their category definitions.
    /// </summary>
    public static readonly Dictionary<string, List<(string Name, CategoryType Type)>> Templates = new()
    {
        ["Personal"] = new()
        {
            ("Salary", CategoryType.Income),
            ("Housing", CategoryType.Expense),
            ("Transportation", CategoryType.Expense),
            ("Food", CategoryType.Expense),
            ("Entertainment", CategoryType.Expense),
            ("Savings", CategoryType.Expense)
        },
        ["Business"] = new()
        {
            ("Revenue", CategoryType.Income),
            ("COGS", CategoryType.Expense),
            ("Operating Expenses", CategoryType.Expense),
            ("Marketing", CategoryType.Expense),
            ("Payroll", CategoryType.Expense)
        },
        ["Household"] = new()
        {
            ("Income", CategoryType.Income),
            ("Rent/Mortgage", CategoryType.Expense),
            ("Utilities", CategoryType.Expense),
            ("Groceries", CategoryType.Expense),
            ("Childcare", CategoryType.Expense),
            ("Healthcare", CategoryType.Expense)
        },
        ["Empty"] = new()
    };

    /// <summary>
    /// Validates if a template name exists in the Templates dictionary.
    /// </summary>
    /// <param name="templateName">The template name to validate.</param>
    /// <returns>True if the template exists, otherwise false.</returns>
    public static bool IsValidTemplate(string templateName)
    {
        return Templates.ContainsKey(templateName);
    }
}
