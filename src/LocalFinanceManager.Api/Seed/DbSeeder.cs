using LocalFinanceManager.Core;
using LocalFinanceManager.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace LocalFinanceManager.Api.Seed
{
    public static class DbSeeder
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            if (db.Accounts.Any()) return;

            db.Accounts.AddRange(
                new Account { Id = Guid.NewGuid(), Label = "Betaalrekening", IBAN = "NL91ABNA0417164300", Type = AccountType.Checking, Currency = "EUR", StartingBalance = 1000m },
                new Account { Id = Guid.NewGuid(), Label = "Spaarrekening", IBAN = "NL20INGB0001234567", Type = AccountType.Savings, Currency = "EUR", StartingBalance = 2500m },
                new Account { Id = Guid.NewGuid(), Label = "Creditcard", IBAN = "", Type = AccountType.Credit, Currency = "EUR", StartingBalance = 0m }
            );
            db.SaveChanges();
        }
    }
}
