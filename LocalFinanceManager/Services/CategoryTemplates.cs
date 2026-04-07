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
            ("Salaris", CategoryType.Income),
            ("Wonen", CategoryType.Expense),
            ("Vervoer", CategoryType.Expense),
            ("Eten & Drinken", CategoryType.Expense),
            ("Vrije Tijd", CategoryType.Expense),
            ("Sparen", CategoryType.Expense)
        },
        ["Business"] = new()
        {
            ("Omzet", CategoryType.Income),
            ("Kostprijs Verkopen", CategoryType.Expense),
            ("Bedrijfskosten", CategoryType.Expense),
            ("Marketing", CategoryType.Expense),
            ("Salarissen", CategoryType.Expense)
        },
        ["Household"] = new()
        {
            ("Inkomsten", CategoryType.Income),
            ("Huur/Hypotheek", CategoryType.Expense),
            ("Nutsvoorzieningen", CategoryType.Expense),
            ("Boodschappen", CategoryType.Expense),
            ("Kinderopvang", CategoryType.Expense),
            ("Zorgkosten", CategoryType.Expense)
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
