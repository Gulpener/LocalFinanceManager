using LocalFinanceManager.Core;
using LocalFinanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using Xunit;

namespace LocalFinanceManager.Tests
{
    public class AccountDbTests
    {
        [Fact]
        public async System.Threading.Tasks.Task CanCreateAndReadAccount_InMemory()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;

            using (var context = new AppDbContext(options))
            {
                var acc = new Account
                {
                    Id = Guid.NewGuid(),
                    Label = "Test",
                    IBAN = "NL00TEST0000000000",
                    Currency = "EUR",
                    Type = AccountType.Checking,
                    StartingBalance = 100m
                };
                context.Accounts.Add(acc);
                context.SaveChanges();
            }

            using (var context = new AppDbContext(options))
            {
                var a = await context.Accounts.FirstOrDefaultAsync();
                Assert.NotNull(a);
                Assert.Equal("Test", a.Label);
            }
        }
    }
}
