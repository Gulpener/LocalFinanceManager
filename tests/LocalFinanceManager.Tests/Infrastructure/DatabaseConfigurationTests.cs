using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LocalFinanceManager.Tests.Infrastructure;

[TestFixture]
public class DatabaseConfigurationTests
{
    [Test]
    public void DevelopmentEnvironment_LoadsDevDatabase()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true);
        
        var config = configBuilder.Build();

        // Act
        var connectionString = config.GetConnectionString("Default");

        // Assert
        Assert.That(connectionString, Does.Contain("localfinancemanager.dev.db"),
            "Development environment should use localfinancemanager.dev.db");
    }

    [Test]
    public void ProductionEnvironment_LoadsProdDatabase()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false);
        
        var config = configBuilder.Build();

        // Act
        var connectionString = config.GetConnectionString("Default");

        // Assert
        Assert.That(connectionString, Does.Contain("localfinancemanager.db"),
            "Production environment should use localfinancemanager.db");
        Assert.That(connectionString, Does.Not.Contain(".dev."),
            "Production environment should not use .dev. database");
    }

    [Test]
    public void DatabasePath_CanBeParsedFromConnectionString()
    {
        // Arrange
        var connectionString = "Data Source=localfinancemanager.dev.db";

        // Act
        var match = Regex.Match(
            connectionString,
            @"Data Source=([^;]+)",
            RegexOptions.IgnoreCase);

        // Assert
        Assert.That(match.Success, Is.True, "Should be able to parse database path from connection string");
        Assert.That(match.Groups[1].Value, Is.EqualTo("localfinancemanager.dev.db"));
    }

    [Test]
    public void DatabasePath_WithAbsolutePath_ParsesCorrectly()
    {
        // Arrange
        var connectionString = "Data Source=/var/lib/myapp/localfinancemanager.db;Mode=ReadWrite";

        // Act
        var match = Regex.Match(
            connectionString,
            @"Data Source=([^;]+)",
            RegexOptions.IgnoreCase);

        // Assert
        Assert.That(match.Success, Is.True);
        Assert.That(match.Groups[1].Value, Is.EqualTo("/var/lib/myapp/localfinancemanager.db"));
    }

    [Test]
    public void DevelopmentConfiguration_HasRecreateDatabase_DefaultFalse()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true);
        
        var config = configBuilder.Build();

        // Act
        var recreateDb = config.GetValue<bool>("RecreateDatabase");

        // Assert
        Assert.That(recreateDb, Is.False,
            "RecreateDatabase should default to false to prevent accidental data loss");
    }

    [Test]
    public void EnvironmentVariableOverride_CanOverrideConnectionString()
    {
        // Arrange
        var customConnectionString = "Data Source=/custom/path/mydb.db";
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = customConnectionString
            });
        
        var config = configBuilder.Build();

        // Act
        var connectionString = config.GetConnectionString("Default");

        // Assert
        Assert.That(connectionString, Is.EqualTo(customConnectionString),
            "Environment variable should override appsettings.json connection string");
    }
}
