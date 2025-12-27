using LocalFinanceManager.Api.DTOs;
using LocalFinanceManager.Core;
using LocalFinanceManager.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LocalFinanceManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AccountsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeArchived = false)
        {
            var q = _db.Accounts.AsQueryable();
            if (!includeArchived) q = q.Where(a => !a.IsArchived);
            var list = await q.OrderBy(a => a.Label).ToListAsync();
            var dtos = list.Select(a => ToDto(a));
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var a = await _db.Accounts.FindAsync(id);
            if (a == null) return NotFound();
            return Ok(ToDto(a));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateAccountDto dto)
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Label = dto.Label,
                Type = dto.Type,
                Currency = dto.Currency.ToUpperInvariant(),
                IBAN = dto.IBAN.Replace(" ", string.Empty).ToUpperInvariant(),
                StartingBalance = dto.StartingBalance,
                IsArchived = false
            };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = account.Id }, ToDto(account));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateAccountDto dto)
        {
            var account = await _db.Accounts.FindAsync(id);
            if (account == null) return NotFound();
            // concurrency check
            if (!account.RowVersion.SequenceEqual(dto.RowVersion)) return Conflict();

            account.Label = dto.Label;
            account.Type = dto.Type;
            account.Currency = dto.Currency.ToUpperInvariant();
            account.IBAN = dto.IBAN.Replace(" ", string.Empty).ToUpperInvariant();
            account.StartingBalance = dto.StartingBalance;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict();
            }

            return Ok(ToDto(account));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Archive(Guid id)
        {
            var account = await _db.Accounts.FindAsync(id);
            if (account == null) return NotFound();
            account.IsArchived = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static AccountDto ToDto(Account a) => new AccountDto
        {
            Id = a.Id,
            Label = a.Label,
            Type = a.Type,
            Currency = a.Currency,
            IBAN = a.IBAN,
            StartingBalance = a.StartingBalance,
            IsArchived = a.IsArchived,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            RowVersion = a.RowVersion ?? Array.Empty<byte>()
        };
    }
}
