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
    public class TransactionsController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public TransactionsController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetAllTransactions()
        {
            var transactions = await TransactionBusiness.GetAllTransactionsAsync(_context);
            return Ok(transactions);
        }

        [HttpGet("account-history/{accountId:guid}")]
        public async Task<ActionResult<IEnumerable<TransactionDTO>>> GetAccountHistory(Guid accountId)
        {
            if (accountId == Guid.Empty)
            {
                return BadRequest("Invalid Account ID format.");
            }

            var accountExist = await AccountBusiness.FindAsync(_context, accountId);
            if (accountExist == null)
            {
                return NotFound($"Account with ID {accountId} not found.");
            }

            var history = await TransactionBusiness.GetAccountHistoryAsync(_context, accountId);
            return Ok(history);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TransactionDTO>> GetTransactionById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Invalid Transaction ID format.");
            }

            var transactionBusiness = await TransactionBusiness.FindAsync(_context, id);

            if (transactionBusiness == null)
            {
                return NotFound($"Transaction with ID {id} not found.");
            }

            return Ok(transactionBusiness.ToDTO);
        }

        [HttpPost("deposit")]
        public async Task<ActionResult<TransactionDTO>> Deposit([FromBody] TransactionDTO transactionDto)
        {
            if (transactionDto == null) return BadRequest("Invalid transaction data.");

            if (transactionDto.DestinationAccountId == null || transactionDto.DestinationAccountId == Guid.Empty)
            {
                return BadRequest("Destination account ID is required for a deposit.");
            }

            var accountBusiness = await AccountBusiness.FindAsync(_context, transactionDto.DestinationAccountId.Value);

            if (accountBusiness == null || !accountBusiness.IsActive)
            {
                return BadRequest("Destination account is invalid or inactive.");
            }

            var completedTx = await accountBusiness.Deposit(transactionDto);

            if (completedTx != null)
            {
                return CreatedAtAction(nameof(GetTransactionById), new { id = completedTx.Id }, completedTx.ToDTO);
            }
            else
            {
                return StatusCode(500, "A critical error occurred. The deposit was safely rolled back.");
            }
        }

        [HttpPost("withdraw")]
        public async Task<ActionResult<TransactionDTO>> Withdraw([FromBody] TransactionDTO transactionDto)
        {
            if (transactionDto == null) return BadRequest("Invalid transaction data.");

            if (transactionDto.SourceAccountId == null || transactionDto.SourceAccountId == Guid.Empty)
            {
                return BadRequest("Source account ID is required for a withdrawal.");
            }

            var accountBusiness = await AccountBusiness.FindAsync(_context, transactionDto.SourceAccountId.Value);
            if (accountBusiness == null || !accountBusiness.IsActive)
            {
                return BadRequest("Source account is invalid or inactive.");
            }

            if (accountBusiness.Balance < transactionDto.Amount) return BadRequest("Insufficient funds.");

            if (transactionDto.Amount > accountBusiness.DailyWithdrawalLimit) return BadRequest("Amount exceeds daily withdrawal limit.");

            var completedTx = await accountBusiness.Withdraw(transactionDto);

            if (completedTx != null)
            {
                return CreatedAtAction(nameof(GetTransactionById), new { id = completedTx.Id }, completedTx.ToDTO);
            }
            else
            {
                return StatusCode(500, "A critical error occurred. The withdrawal was safely rolled back.");
            }
        }

        [HttpPost("transfer")]
        public async Task<ActionResult<TransactionDTO>> Transfer([FromBody] TransactionDTO transactionDto)
        {
            if (transactionDto == null) return BadRequest("Invalid transaction data.");

            if (transactionDto.SourceAccountId == null || transactionDto.DestinationAccountId == null)
            {
                return BadRequest("Both Source and Destination account IDs are required.");
            }

            if (transactionDto.SourceAccountId == transactionDto.DestinationAccountId)
            {
                return BadRequest("Cannot transfer money to the same account.");
            }

            var sourceAccount = await AccountBusiness.FindAsync(_context, transactionDto.SourceAccountId.Value);
            var destAccount = await AccountBusiness.FindAsync(_context, transactionDto.DestinationAccountId.Value);

            if (sourceAccount == null || !sourceAccount.IsActive) return BadRequest("Source account is invalid or inactive.");
            if (destAccount == null || !destAccount.IsActive) return BadRequest("Destination account is invalid or inactive.");

            if (sourceAccount.Balance < transactionDto.Amount) return BadRequest("Insufficient funds for this transfer.");
            if (transactionDto.Amount > sourceAccount.DailyWithdrawalLimit) return BadRequest("Amount exceeds daily withdrawal limit.");

            var completedTx = await AccountBusiness.TransferFundsAsync(_context, sourceAccount, destAccount, transactionDto);

            if (completedTx != null)
            {
                return CreatedAtAction(nameof(GetTransactionById), new { id = completedTx.Id }, completedTx.ToDTO);
            }
            else
            {
                return StatusCode(500, "A critical error occurred. The transfer was safely rolled back.");
            }
        }

    }
}