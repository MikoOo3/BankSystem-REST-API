using BankDataAccessLayer;
using BankDataAccessLayer.DTOs;
using BankSystem.API.Services;
using BankSystem.Business.BusinessObjects;
using BankSystem.DataAccess.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ActiveUser")]
    [EnableRateLimiting("GeneralApiLimiter")]
    public class AccountsController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public AccountsController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        [Authorize(Policy = "BankStaff")]
        public async Task<ActionResult<IEnumerable<AccountDTO>>> GetAllAccounts()
        {
            var accounts = await AccountBusiness.GetAllAccountsAsync(_context);
            return Ok(accounts);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<AccountDTO>> GetAccountById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid Account ID format.");
            }

            var accountBusiness = await AccountBusiness.FindAsync(_context, id);

            if (accountBusiness == null)
            {
                return NotFound($"Account with ID {id} not found.");
            }

            if (!User.IsBankStaff() && accountBusiness.CustomerId != User.GetUserId())
                return Forbid();


            return Ok(accountBusiness.ToDTO);
        }

        [HttpGet("{accountNumber}")]
        public async Task<ActionResult<AccountDTO>> GetAccountByNumber(string accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return BadRequest("Account number cannot be empty.");
            }

            var accountBusiness = await AccountBusiness.FindByAccountNumberAsync(_context, accountNumber);

            if (accountBusiness == null)
            {
                return NotFound($"Account number {accountNumber} not found.");
            }

            if (!User.IsBankStaff() && accountBusiness.CustomerId != User.GetUserId())
                return Forbid();

            return Ok(accountBusiness.ToDTO);
        }

        [HttpGet("customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<AccountDTO>>> GetCustomerAccounts(Guid customerId)
        {
            if (customerId == Guid.Empty)
            {
                return BadRequest("Invalid Customer ID format.");
            }

            if (!User.IsBankStaff() && User.GetUserId() != customerId)
                return Forbid();


            var accounts = await AccountBusiness.GetCustomerAccountsAsync(_context, customerId);
            return Ok(accounts);
        }

        [HttpPost("create")]
        [Authorize(Policy = "BankStaff")]
        public async Task<ActionResult<AccountDTO>> CreateAccount([FromBody] AccountDTO accountDto)
        {
            if (accountDto == null) return BadRequest("Invalid account data.");

            var userExist = await UserBusiness.FindAsync(_context, accountDto.CustomerId);
            if (userExist == null)
            {
                return BadRequest("Cannot create account. Customer does not exist.");
            }

            var accountBusiness = new AccountBusiness(_context, accountDto, AccountBusiness.enMode.AddNew);
            bool isSaved = await accountBusiness.SaveAsync();

            if (!isSaved)
            {
                return StatusCode(500, "A problem occurred while creating the account.");
            }

            await AuditLogBusiness.LogAsync(
                _context,
                User.GetUserId(),
                "Account Created",
                null,
                $"AccountId: {accountBusiness.Id}, AccountNumber: {accountBusiness.AccountNumber}, CustomerId: {accountBusiness.CustomerId}, Type: {accountBusiness.AccountType}"
            );

            return CreatedAtAction(nameof(GetAccountById), new { id = accountBusiness.Id }, accountBusiness.ToDTO);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "BankStaff")]
        public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] AccountUpdateDTO accountUpdateDto)
        {
            if (accountUpdateDto == null) return BadRequest("Invalid account data.");
            if (id != accountUpdateDto.Id) return BadRequest("Account ID mismatch.");

            var accountBusiness = await AccountBusiness.FindAsync(_context, id);
            if (accountBusiness == null) return NotFound($"Account with ID {id} not found.");

            string oldValues = $"AccountId: {id}, Type: {accountBusiness.AccountType}, DailyLimit: {accountBusiness.DailyWithdrawalLimit}";

            accountBusiness.AccountType = accountUpdateDto.AccountType;
            accountBusiness.DailyWithdrawalLimit = accountUpdateDto.DailyWithdrawalLimit;
            accountBusiness.Mode = AccountBusiness.enMode.Update;

            if (!await accountBusiness.SaveAsync())
                return StatusCode(500, "A problem occurred while updating the account.");

            string newValues = $"AccountId: {id}, Type: {accountBusiness.AccountType}, DailyLimit: {accountBusiness.DailyWithdrawalLimit}";
            await AuditLogBusiness.LogAsync(_context, User.GetUserId(), "Account Updated", oldValues, newValues);

            return NoContent();
        }

        [HttpPut("{id:guid}/activate")]
        [Authorize(Policy = "BankManager")]
        public async Task<IActionResult> ActivateAccount(Guid id)
        {
            var accountBusiness = await AccountBusiness.FindAsync(_context, id);
            if (accountBusiness == null) return NotFound($"Account with ID {id} not found.");

            if (accountBusiness.IsActive) return BadRequest("Account is already active.");

            accountBusiness.IsActive = true;
            accountBusiness.Mode = AccountBusiness.enMode.Update;

            if (!await accountBusiness.SaveAsync())
                return StatusCode(500, "Failed to activate account.");

            await AuditLogBusiness.LogAsync(_context, User.GetUserId(), "Account Activated", $"AccountId: {id}, IsActive: False", $"AccountId: {id}, IsActive: True");

            return Ok(new { message = "Account activated successfully." });
        }

        [HttpPut("close/{id:guid}")]
        [Authorize(Policy = "BankManager")]
        public async Task<IActionResult> CloseAccount(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid Account ID format.");
            }

            var accountBusiness = await AccountBusiness.FindAsync(_context, id);
            if (accountBusiness == null)
            {
                return NotFound($"Account with ID {id} not found.");
            }

            bool isClosed = await accountBusiness.CloseAccountAsync();

            if (!isClosed)
            {
                return StatusCode(500, "A problem occurred while closing the account.");
            }

            await AuditLogBusiness.LogAsync(_context, User.GetUserId(), "Account Closed", $"AccountId: {id}, IsActive: True", $"AccountId: {id}, IsActive: False");

            return Ok($"Account with ID {id} has been successfully closed.");
        }

    }
}