using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LocalFinanceManager.Blazor.Models;

namespace LocalFinanceManager.Blazor.Services
{
    public class AccountService
    {
        private readonly HttpClient _http;
        public AccountService(HttpClient http) => _http = http;

        public async Task<AccountDto[]> GetAllAsync(bool includeArchived = false)
            => await _http.GetFromJsonAsync<AccountDto[]>($"api/accounts?includeArchived={includeArchived.ToString().ToLower()}") ?? Array.Empty<AccountDto>();

        public async Task<AccountDto?> GetAsync(Guid id)
            => await _http.GetFromJsonAsync<AccountDto>($"api/accounts/{id}");

        public async Task<bool> CreateAsync(CreateAccountDto dto)
        {
            var res = await _http.PostAsJsonAsync("api/accounts", dto);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateAccountDto dto)
        {
            var res = await _http.PutAsJsonAsync($"api/accounts/{id}", dto);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> ArchiveAsync(Guid id)
        {
            var res = await _http.DeleteAsync($"api/accounts/{id}");
            return res.IsSuccessStatusCode;
        }
    }
}
