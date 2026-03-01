namespace LocalFinanceManager.ML.Tests;

[TestFixture]
public class FixtureModelsTests
{
    [Test]
    public void FixturesDirectory_HasExpectedStructure()
    {
        var fixturesRoot = GetFixturesRootPath();
        var readmePath = Path.Combine(fixturesRoot, "README.md");
        var modelsPath = Path.Combine(fixturesRoot, "models");

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(fixturesRoot), Is.True, "Fixtures root directory should exist.");
            Assert.That(File.Exists(readmePath), Is.True, "Fixtures README should exist.");
            Assert.That(Directory.Exists(modelsPath), Is.True, "Fixture models directory should exist.");
        });
    }

    [Test]
    public void FixtureModelFiles_WhenPresent_AreNonEmptyAndSmall()
    {
        var modelsPath = Path.Combine(GetFixturesRootPath(), "models");

        if (!Directory.Exists(modelsPath))
        {
            Assert.Ignore("Fixture models directory does not exist yet.");
        }

        var modelFiles = Directory.GetFiles(modelsPath, "*.bin", SearchOption.TopDirectoryOnly);

        if (modelFiles.Length == 0)
        {
            Assert.Ignore("No .bin fixture models present yet. Populate during MVP-5.");
        }

        foreach (var modelFile in modelFiles)
        {
            var fileInfo = new FileInfo(modelFile);
            Assert.Multiple(() =>
            {
                Assert.That(fileInfo.Length, Is.GreaterThan(0), $"Fixture model '{fileInfo.Name}' must not be empty.");
                Assert.That(fileInfo.Length, Is.LessThan(1_000_000), $"Fixture model '{fileInfo.Name}' should stay below 1MB.");
            });
        }
    }

    private static string GetFixturesRootPath()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "fixtures"));
    }
}
