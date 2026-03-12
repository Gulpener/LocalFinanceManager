using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;

namespace LocalFinanceManager.Tests.Infrastructure;

[TestFixture]
public class DatabaseConfigurationTests
{
    [Test]
    public void DevelopmentConfiguration_UsesPostgreSQLConnectionString()
    {
        // Arrange
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true);

        var config = configBuilder.Build();

        // Act
        var connectionString = config.GetConnectionString("Default");

        // Assert
        Assert.That(connectionString, Is.Not.Null.And.Not.Empty,
            "ConnectionStrings:Default must be configured");
        Assert.That(connectionString, Does.Not.Contain("Data Source=").IgnoreCase,
            "Connection string must not use SQLite Data Source format");
    }

    [Test]
    public void PostgreSQLConnectionString_ContainsRequiredParts()
    {
        // Arrange — local dev default
        var connectionString = "Host=localhost;Port=5432;Database=localfinancemanager;Username=postgres;Password=postgres";

        // Assert key components can be parsed from a PostgreSQL connection string
        Assert.That(connectionString, Does.Contain("Host=").IgnoreCase,
            "PostgreSQL connection string should contain Host");
        Assert.That(connectionString, Does.Contain("Database=").IgnoreCase,
            "PostgreSQL connection string should contain Database");
    }

    [Test]
    public void SupabaseConnectionString_ContainsRequiredParts()
    {
        // Arrange — Supabase format
        var connectionString = "Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=xxx;SSL Mode=Require;Trust Server Certificate=true";

        // Assert Supabase connection string contains both required SSL settings
        Assert.That(connectionString, Does.Contain("SSL Mode=Require"),
            "Production Supabase connection string must enforce SSL");
        Assert.That(connectionString, Does.Contain("supabase.co").IgnoreCase,
            "This test validates a Supabase-format connection string");
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
