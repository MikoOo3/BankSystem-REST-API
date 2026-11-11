using BankDataAccessLayer;
using BankSystem.Business.BusinessObjects;
using BankSystem.DataAccess.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public AccountsController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
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

            return Ok(accountBusiness.ToDTO);
        }

        [HttpGet("customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<AccountDTO>>> GetCustomerAccounts(Guid customerId)
        {
            if (customerId == Guid.Empty)
            {
                return BadRequest("Invalid Customer ID format.");
            }

            var accounts = await AccountBusiness.GetCustomerAccountsAsync(_context, customerId);
            return Ok(accounts);
        }

        [HttpPost("create")]
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

            return CreatedAtAction(nameof(GetAccountById), new { id = accountBusiness.Id }, accountBusiness.ToDTO);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] AccountDTO accountDto)
        {
            if (accountDto == null) return BadRequest("Invalid account data.");

            if (id != accountDto.Id) return BadRequest("Account ID mismatch.");

            var accountBusiness = await AccountBusiness.FindAsync(_context, id);
            if (accountBusiness == null)
            {
                return NotFound($"Account with ID {id} not found.");
            }

            accountBusiness.AccountType = accountDto.AccountType;
            accountBusiness.DailyWithdrawalLimit = accountDto.DailyWithdrawalLimit;
            accountBusiness.IsActive = accountDto.IsActive;
            accountBusiness.Mode = AccountBusiness.enMode.Update;

            bool isSaved = await accountBusiness.SaveAsync();

            if (!isSaved)
            {
                return StatusCode(500, "A problem occurred while updating the account.");
            }

            return NoContent();
        }


        [HttpPut("close/{id:guid}")]
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

            return Ok($"Account with ID {id} has been successfully closed.");
        }

    }
}