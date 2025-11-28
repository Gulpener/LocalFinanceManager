using System.Threading.Tasks;
using Xunit;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using LocalFinanceManager.Application.Interfaces;
using LocalFinanceManager.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.JSInterop;

namespace Infrastructure.Tests
{
    public class RulesUiTests : TestContext
    {
        [Fact]
        public void Index_ShowsCategoryAndEnvelopeNames_And_DeletesWhenConfirmed()
        {
            // Arrange
            var rule = new Rule { Id = 1, Priority = 10, MatchType = "contains", Pattern = "coffee", TargetCategoryId = 5, TargetEnvelopeId = 7 };

            var repo = new InMemoryRuleRepository(new[] { rule });
            Services.AddSingleton<IRuleRepository>(repo);

            var catRepo = new InMemoryCategoryRepository(new[] { new Category { Id = 5, Name = "Food" } });
            Services.AddSingleton<ICategoryRepository>(catRepo);

            var envRepo = new InMemoryEnvelopeRepository(new[] { new Envelope { Id = 7, Name = "Dining" } });
            Services.AddSingleton<IEnvelopeRepository>(envRepo);

            // Intercept JS confirm to return true
            var js = Services.AddMockJSRuntime();
            js.Setup<bool>("confirm").SetResult(true);

            // Act
            var comp = RenderComponent<LocalFinanceManager.Web.Components.Pages.Rules.Index>();

            // Assert: names are displayed
            comp.Markup.Contains("Food");
            comp.Markup.Contains("Dining");

            // Click delete button - find first delete button and click
            var deleteBtn = comp.FindAll("button").FirstOrDefault(b => b.TextContent.Trim() == "Delete");
            Assert.NotNull(deleteBtn);
            deleteBtn!.Click();

            // After deletion, repository should be empty
            Assert.Empty(repo.Items);
        }

        // Simple in-memory fake repositories
        private class InMemoryRuleRepository : IRuleRepository
        {
            public List<Rule> Items { get; }
            public InMemoryRuleRepository(IEnumerable<Rule> initial)
            {
                Items = initial.Select(r => { var copy = r; return copy; }).ToList();
            }
            public Task<Rule> AddAsync(Rule rule, System.Threading.CancellationToken cancellationToken = default)
            {
                rule.Id = Items.Any() ? Items.Max(x => x.Id) + 1 : 1;
                Items.Add(rule);
                return Task.FromResult(rule);
            }
            public Task<bool> DeleteAsync(int id, System.Threading.CancellationToken cancellationToken = default)
            {
                var ex = Items.FirstOrDefault(x => x.Id == id);
                if (ex == null) return Task.FromResult(false);
                Items.Remove(ex);
                return Task.FromResult(true);
            }
            public Task<IReadOnlyList<Rule>> GetAllOrderedByPriorityAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.FromResult((IReadOnlyList<Rule>)Items.OrderByDescending(x => x.Priority).ToList());
            }
            public Task<Rule?> GetByIdAsync(int id, System.Threading.CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
            }
            public Task UpdateAsync(Rule rule, System.Threading.CancellationToken cancellationToken = default)
            {
                var idx = Items.FindIndex(x => x.Id == rule.Id);
                if (idx >= 0) Items[idx] = rule;
                return Task.CompletedTask;
            }
        }

        private class InMemoryCategoryRepository : ICategoryRepository
        {
            private readonly List<Category> _items;
            public InMemoryCategoryRepository(IEnumerable<Category> items) => _items = items.ToList();
            public Task<Category> AddAsync(Category category, System.Threading.CancellationToken cancellationToken = default)
            {
                category.Id = _items.Any() ? _items.Max(x => x.Id) + 1 : 1;
                _items.Add(category);
                return Task.FromResult(category);
            }
            public Task<bool> DeleteAsync(int id, System.Threading.CancellationToken cancellationToken = default)
            {
                var ex = _items.FirstOrDefault(x => x.Id == id);
                if (ex == null) return Task.FromResult(false);
                _items.Remove(ex);
                return Task.FromResult(true);
            }
            public Task<IReadOnlyList<Category>> GetAllAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Category>)_items.ToList());
            public Task<IReadOnlyList<Category>> GetChildCategoriesAsync(int parentId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Category>)new List<Category>());
            public Task<IReadOnlyList<Category>> GetRootCategoriesAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Category>)new List<Category>());
            public Task<Category?> GetByIdAsync(int id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
            public Task UpdateAsync(Category category, System.Threading.CancellationToken cancellationToken = default) { var idx = _items.FindIndex(x => x.Id == category.Id); if (idx >= 0) _items[idx] = category; return Task.CompletedTask; }
        }

        private class InMemoryEnvelopeRepository : IEnvelopeRepository
        {
            private readonly List<Envelope> _items;
            public InMemoryEnvelopeRepository(IEnumerable<Envelope> items) => _items = items.ToList();
            public Task<Envelope> AddAsync(Envelope envelope, System.Threading.CancellationToken cancellationToken = default)
            {
                envelope.Id = _items.Any() ? _items.Max(x => x.Id) + 1 : 1;
                _items.Add(envelope);
                return Task.FromResult(envelope);
            }
            public Task<bool> DeleteAsync(int id, System.Threading.CancellationToken cancellationToken = default)
            {
                var ex = _items.FirstOrDefault(x => x.Id == id);
                if (ex == null) return Task.FromResult(false);
                _items.Remove(ex);
                return Task.FromResult(true);
            }
            public Task<IReadOnlyList<Envelope>> GetAllAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Envelope>)_items.ToList());
            public Task<Envelope?> GetByIdAsync(int id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
            public Task UpdateAsync(Envelope envelope, System.Threading.CancellationToken cancellationToken = default) { var idx = _items.FindIndex(x => x.Id == envelope.Id); if (idx >= 0) _items[idx] = envelope; return Task.CompletedTask; }
        }
    }
}
