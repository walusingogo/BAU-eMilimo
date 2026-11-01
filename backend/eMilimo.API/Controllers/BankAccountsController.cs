using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eMilimo.Core.Entities;
using eMilimo.Infrastructure.Data;

namespace eMilimo.API.Controllers
{
    [ApiController]
    [Route("api/contributions/bank-accounts")]  
    [Authorize]
    public class BankAccountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BankAccountsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var accounts = await _context.BankAccounts
                .OrderBy(b => b.AccountNumber)
                .Select(b => new
                {
                    b.Id,
                    b.AccountNumber,
                    b.AccountName,
                    b.BankName,
                    b.Branch,
                    b.IsActive
                })
                .ToListAsync();

            return Ok(accounts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var account = await _context.BankAccounts.FindAsync(id);
            if (account == null)
                return NotFound();

            return Ok(account);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BankAccount account)
        {
            // Ignore client-provided Id and set a new one
            account.Id = Guid.NewGuid();
            account.IsActive = true;  // New accounts are active by default

            _context.BankAccounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] BankAccount updated)
        {
            var account = await _context.BankAccounts.FindAsync(id);
            if (account == null)
                return NotFound();

            account.BankName = updated.BankName;
            account.AccountName = updated.AccountName;
            account.AccountNumber = updated.AccountNumber;
            account.Branch = updated.Branch;
            account.IsActive = updated.IsActive; // Respect provided status

            await _context.SaveChangesAsync();
            return Ok(account);
        }

        [HttpPut("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var account = await _context.BankAccounts.FindAsync(id);
            if (account == null)
                return NotFound();

            account.IsActive = !account.IsActive;
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Status toggled to {(account.IsActive ? "Active" : "Inactive")}" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var account = await _context.BankAccounts.FindAsync(id);
            if (account == null)
                return NotFound();

            _context.BankAccounts.Remove(account);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}