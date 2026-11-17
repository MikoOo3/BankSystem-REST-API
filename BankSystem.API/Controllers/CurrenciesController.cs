using BankBusinessLayer;
using BankDataAccessLayer;
using BankSystem.API.Services;
using BankSystem.DataAccess.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ActiveUser")]
    [EnableRateLimiting("GeneralApiLimiter")]
    public class CurrenciesController : ControllerBase
    {
        private readonly BankSystemDbContext _context;

        public CurrenciesController(BankSystemDbContext context)
        {
            _context = context;
        }

        [HttpGet("All")]
        public async Task<ActionResult<List<CurrencyDTO>>> GetAllCurrencies()
        {
            var currencies = await CurrencyBusiness.GetAllCurrenciesAsync(_context);
            return Ok(currencies);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CurrencyDTO>> GetById(int id)
        {
            var currency = await CurrencyBusiness.FindByIdAsync(_context, id);
            if (currency == null)
                return NotFound($"Currency with ID {id} not found.");

            return Ok(currency);
        }

        [HttpGet("code/{code}")]
        public async Task<ActionResult<CurrencyDTO>> GetByCode(string code)
        {
            var currency = await CurrencyBusiness.FindByCodeAsync(_context, code);
            if (currency == null)
                return NotFound($"Currency with code '{code}' not found.");

            return Ok(currency);
        }

        [HttpGet("country/{country}")]
        public async Task<ActionResult<CurrencyDTO>> GetByCountry(string country)
        {
            var currency = await CurrencyBusiness.FindByCountryAsync(_context, country);
            if (currency == null)
                return NotFound($"Currency for country '{country}' not found.");

            return Ok(currency);
        }

        [HttpGet("CurrencyName/{name}")]
        public async Task<ActionResult<CurrencyDTO>> GetByCurrencyName(string name)
        {
            var currency = await CurrencyBusiness.FindByNameAsync(_context, name);
            if (currency == null)
                return NotFound($"Currency with Currency name '{name}' not found.");

            return Ok(currency);
        }

        [HttpGet("calculate")]
        public async Task<ActionResult<CurrencyConversionResultDTO>> CalculateExchange([FromQuery] string fromCurrency,
            [FromQuery] string toCurrency,[FromQuery] decimal amount)
        {
            var (result, errorMessage) = await CurrencyBusiness.CalculateExchangeAsync(_context, fromCurrency, toCurrency, amount);

            if (errorMessage != null)
                return BadRequest(errorMessage);

            return Ok(result);
        }

        [HttpPut("{code}/rate")]
        [Authorize(Policy = "BankManager")]
        public async Task<IActionResult> UpdateRate(string code, [FromBody] UpdateCurrencyRateDTO updateDto)
        {
            var (success, errorMessage) = await CurrencyBusiness.UpdateRateAsync(_context, code, updateDto.NewRate, User.GetUserId());

            if (!success)
                return BadRequest(errorMessage);

            return Ok(new { message = $"Exchange rate for '{code.ToUpper()}' updated successfully to {updateDto.NewRate}." });
        }
    }
}